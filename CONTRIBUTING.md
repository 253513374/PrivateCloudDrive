# Contributing to PrivateCloudDrive

感谢你愿意参与 PrivateCloudDrive。这个项目当前采用“手机优先私有备份网盘”的产品方向，贡献应优先服务可信备份闭环，而不是扩散成泛 NAS 或企业协作平台。

## 优先贡献方向

1. 私有部署稳定性：Docker Compose、升级、回滚、健康检查、日志、备份恢复。
2. 移动端备份体验：后端地址配置、备份中心、上传进度、失败重试、恢复说明。
3. 数据与安全：权限边界、分享安全、密钥配置、隐私说明、测试覆盖。
4. 文档：部署教程、故障排查、产品边界、验收清单。
5. 自动化测试：后端单元/集成测试、MAUI 构建验证、端到端验收脚本。

## 暂不鼓励的方向

- NAS OS、RAID、SMB/NFS、下载器平台。
- 企业审批流、复杂组织架构、Office 在线协作。
- 在备份恢复和部署可信度完成前引入 AI 相册/AI 搜索。
- 只做视觉装饰、但不改善实际备份和恢复体验的 UI 改动。

## 开发流程

1. Fork 仓库并创建分支：`feature/<topic>`、`fix/<topic>` 或 `docs/<topic>`。
2. 修改前先阅读：
   - `README.md`
   - `docs/roadmap-public.md`
   - `docs/deployment.md`
   - `docs/testing.md`
3. 代码变更必须补充或更新对应测试/文档。
4. 提交前至少运行与影响范围匹配的验证命令。
5. 创建 Pull Request，说明用户场景、实现范围、风险、验证结果和截图/日志证据。

## 本地验证建议

后端：

```powershell
cd aspnet-core
dotnet build .\PrivateCloudDrive.slnx
dotnet test .\PrivateCloudDrive.slnx --no-build
```

Docker Compose：

```powershell
docker compose config
.\scriptserify-local-stack.ps1 -PreflightOnly
```

MAUI：

```powershell
.\scriptserify-maui-build.ps1 -SkipAndroid
.\scriptserify-maui-build.ps1 -SkipWindows
```

## 提交信息

推荐使用中文或英文的简洁提交信息：

```text
修复：备份失败重试状态不刷新
文档：补齐 Docker 部署密钥说明
feat: add storage health summary
```

## PR 必须说明

- 解决了什么用户问题？
- 影响哪些页面/API/数据表/部署项？
- 如何验证？
- 是否涉及密钥、权限、文件删除、数据迁移或备份恢复？
- 是否改变当前产品路线图？
