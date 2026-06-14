from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
APP = ROOT / "maui" / "PrivateCloudDrive.App"

cloud = (APP / "Services" / "CloudDriveApiClient.cs").read_text(encoding="utf-8")
model = (APP / "Models" / "UploadQueueItem.cs").read_text(encoding="utf-8")
xaml = (APP / "Views" / "UploadsPage.xaml").read_text(encoding="utf-8")
mock = (APP / "Services" / "MockCloudDriveApiClient.cs").read_text(encoding="utf-8")
service = (APP / "Services" / "BackupTransferService.cs").read_text(encoding="utf-8")
interface = (APP / "Services" / "ICloudDriveApiClient.cs").read_text(encoding="utf-8")

required_dto_fields = [
    "UploadedChunkCount",
    "UploadedBytes",
    "ProgressPercent",
    "IsRetryable",
    "StatusReason",
    "FailureReason",
    "NextAction",
]
for field in required_dto_fields:
    assert field in cloud, f"CloudDriveApiClient UploadSessionDto missing {field}"

assert "IProgress<UploadTransferProgress>?" in interface, "API client must expose structured upload progress"
assert "public sealed class UploadTransferProgress" in model, "UploadTransferProgress model missing"
assert "ApplyServerProgress" in model, "UploadQueueItem must consume server session progress"
assert "GetStatusReasonText" in model and "未知状态" in model, "statusReason must have unknown/default display"
assert "GetNextActionText" in model and "等待客户端兼容" in model, "nextAction must have unknown/default display"
assert "PrivateCloudDrive:FileCenter:000033" in model and "重新开始备份" in model, "cancelled code must map to restart backup"
assert "StartNewUploadSession" in model and "UploadMissingChunks" in model and "OpenFile" in model, "known nextAction values missing"
assert "ServerStateText" in xaml and "RecoveryActionText" in xaml, "queue UI must show server status/action"
assert "UploadedBytesText" in xaml and "UploadedChunksText" in xaml, "queue UI must show server progress details"
assert "MockUploadSession" in mock and "Cancelled" in mock and "PrivateCloudDrive:FileCenter:000033" in mock, "mock session scenarios missing"
assert "new Progress<UploadTransferProgress>" in service, "backup service must wire structured progress into queue item"

print("upload session mobile contract checks passed")
