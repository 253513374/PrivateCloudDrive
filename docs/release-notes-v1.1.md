# PrivateCloudDrive V1.1 Release Notes

发布日期：2026-05-09

## 新增能力

- Files 页支持关键字搜索、当前目录/全盘搜索、排序、文件夹/文件筛选和图片/视频/其他媒体筛选。
- Files 页新增多选模式，可批量删除、收藏、取消收藏，并将选中项移动到根目录。
- Trash 页新增多选模式，可批量恢复或批量永久删除。
- Settings 页新增容量使用卡，展示已用容量、配额、剩余容量和进度。
- Settings 页新增“我的分享”入口，可查看当前用户分享、复制公开链接并禁用有效分享。

## 后端 API

- 新增 `/api/file-center/storage/usage` 返回当前用户容量摘要。
- 新增 `/api/file-center/nodes/batch/delete`、`batch/restore`、`batch/permanent-delete`、`batch/move`、`batch/favorite`。
- 文件列表查询支持 `SearchKeyword`、`SearchScope`、`NodeType`、`MediaType` 和 `Sorting`。
- 个人分享列表现在包含已禁用和已过期分享，并返回 `CreationTime`、`IsExpired` 状态。

## 验证

- `dotnet build .\aspnet-core\PrivateCloudDrive.slnx`：通过。
- `dotnet test .\aspnet-core\PrivateCloudDrive.slnx`：`PrivateCloudDrive.EntityFrameworkCore.Tests` 通过 79 个测试。
- `.\scripts\verify-maui-build.ps1 -SkipAndroid`：Windows MAUI 构建通过。
- `docker compose up -d --build` + `.\scripts\verify-docker-stack.ps1`：本地 Docker 后端栈启动并验证通过。

## 已知边界

- V1.1 搜索仍是文件名搜索，不包含全文、OCR 或 AI 搜索。
- 文件夹大小不递归汇总；容量卡按当前用户 Blob 存储使用量统计。
- 批量移动当前只在 MAUI 中提供“移到根目录”快捷操作，后端已支持传入任意目标目录。
- Android/iOS 交互验收仍需在具备对应设备和 workload 的环境中回填。
