# GitHub Actions CI 启用说明

当前公开仓库需要启用基础 CI，但当前 Hermes 使用的 GitHub OAuth Token 只有 `repo` 等权限，没有 `workflow` scope。GitHub 会拒绝通过该 Token 创建或更新 `.github/workflows/*.yml`：

```text
refusing to allow an OAuth App to create or update workflow `.github/workflows/ci.yml` without `workflow` scope
```

因此，本文件先保留 CI 设计和待提交 workflow。补齐 `workflow` scope 后，将下方内容保存为 `.github/workflows/ci.yml`，再提交并推送。

## 验收目标

- Pull Request 和 main push 都运行 CI。
- CI 至少验证：
  - 公开 Markdown 不包含隐藏控制字符，例如 `\x0b`、`\x7f`。
  - `docker compose config --quiet`。
  - `dotnet restore aspnet-core/PrivateCloudDrive.slnx`。
  - `dotnet build aspnet-core/PrivateCloudDrive.slnx --no-restore --configuration Release`。
  - `dotnet test aspnet-core/PrivateCloudDrive.slnx --no-build --configuration Release`。
- 测试结果以 `.trx` artifact 上传，便于失败时排查。

## 待提交 workflow

```yaml
name: CI

on:
  push:
    branches:
      - main
  pull_request:
    branches:
      - main

permissions:
  contents: read

concurrency:
  group: ci-${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: true

jobs:
  quality-gate:
    name: Public repo quality gate
    runs-on: ubuntu-latest
    timeout-minutes: 30

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Scan public Markdown for hidden control characters
        shell: pwsh
        run: |
          $ErrorActionPreference = 'Stop'
          $badFiles = @()
          Get-ChildItem -Path . -Recurse -File -Include *.md |
            Where-Object { $_.FullName -notmatch '[\\/](\.git|artifacts|bin|obj)[\\/]' } |
            ForEach-Object {
              $content = Get-Content -Raw -LiteralPath $_.FullName
              if ($content.Contains([char]0x0b) -or $content.Contains([char]0x7f)) {
                $badFiles += $_.FullName
              }
            }
          if ($badFiles.Count -gt 0) {
            Write-Error ("Hidden control characters found in Markdown files:`n" + ($badFiles -join "`n"))
          }
          Write-Host 'Markdown control-character scan passed.'

      - name: Validate Docker Compose configuration
        run: docker compose config --quiet

      - name: Setup .NET SDK
        uses: actions/setup-dotnet@v4
        with:
          global-json-file: global.json
          cache: true
          cache-dependency-path: |
            aspnet-core/**/*.csproj
            maui/**/*.csproj

      - name: Restore backend
        run: dotnet restore aspnet-core/PrivateCloudDrive.slnx

      - name: Build backend
        run: dotnet build aspnet-core/PrivateCloudDrive.slnx --no-restore --configuration Release

      - name: Test backend
        run: dotnet test aspnet-core/PrivateCloudDrive.slnx --no-build --configuration Release --logger "trx;LogFilePrefix=ci-backend" --results-directory artifacts/test-results/ci-backend

      - name: Upload test results
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: backend-test-results
          path: artifacts/test-results/ci-backend/**/*.trx
          if-no-files-found: ignore
```

## 启用后同步修改

启用 workflow 后，还需要：

1. 在 `README.md` 增加 CI badge：

```markdown
[![CI](https://github.com/253513374/PrivateCloudDrive/actions/workflows/ci.yml/badge.svg)](https://github.com/253513374/PrivateCloudDrive/actions/workflows/ci.yml)
```

2. 在 Issue #3 评论实际 Actions 运行地址和结果。
3. CI 首次通过后，才能关闭 #3。
