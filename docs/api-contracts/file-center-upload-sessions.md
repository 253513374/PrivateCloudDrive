# File Center Upload Sessions API Contract

本文记录文件中心分片上传会话响应契约，供后端、MAUI/客户端与 QA 对齐。

## UploadSessionDto 响应字段

现有字段保持向后兼容：

- Id
- TenantId
- OwnerId
- ParentId
- FileName
- TotalSize
- ChunkSize
- TotalChunks
- ContentType
- Sha256
- Status
- ExpirationTime
- FileNodeId
- UploadedChunks

新增响应字段：

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| UploadedChunkCount | int | 已上传分片数量，等于 UploadedChunks 去重后的数量。 |
| UploadedBytes | long | 已上传字节数，按每个已上传分片的期望大小累加，最后一个分片按剩余大小计算。 |
| ProgressPercent | decimal | 上传进度百分比，按 UploadedBytes / TotalSize * 100 计算，保留 2 位小数并最高限制为 100。 |
| IsRetryable | bool | 当前会话是否可继续上传；Pending 为 true，Completed/Cancelled 为 false。 |
| StatusReason | string | 稳定状态原因字符串。 |
| FailureReason | string? | 失败或终止原因；取消时为 Cancelled，其他状态为 null。 |
| NextAction | string | 客户端下一步动作提示。 |

## 稳定字符串取值

StatusReason：

- WaitingForChunks：会话 Pending，客户端应继续上传缺失分片。
- Completed：会话已完成，可打开生成的文件。
- Cancelled：会话已取消，客户端应重新创建上传会话。
- Unknown：兜底值，客户端必须兼容未知状态。

NextAction：

- UploadMissingChunks：继续上传缺失分片。
- OpenFile：打开已生成文件。
- StartNewUploadSession：重新开始上传会话。

FailureReason：

- Cancelled：会话被取消。
- null：非失败/取消状态。

## 取消错误码

取消后的会话不允许继续上传分片或完成合并。客户端如果继续操作取消会话，服务端返回：

- 常量：FileCenterUploadSessionCancelled
- 错误码：PrivateCloudDrive:FileCenter:000033
- 推荐客户端动作：StartNewUploadSession

## 向后兼容

新增字段均为响应追加字段。旧客户端可忽略未知字段；新客户端读取时必须对缺失字段、Unknown 状态原因和未知 NextAction 做兜底处理。
