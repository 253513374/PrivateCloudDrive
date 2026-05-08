namespace PrivateCloudDrive;

public static class PrivateCloudDriveDomainErrorCodes
{
    /* You can add your business exception error codes here, as constants */
    public const string FileCenterFolderCannotHaveSize = "PrivateCloudDrive:FileCenter:000001";
    public const string FileCenterNodeAlreadyExists = "PrivateCloudDrive:FileCenter:000002";
    public const string FileCenterNodeNotFound = "PrivateCloudDrive:FileCenter:000003";
    public const string FileCenterParentFolderNotFound = "PrivateCloudDrive:FileCenter:000004";
    public const string FileCenterCannotMoveToSelfOrDescendant = "PrivateCloudDrive:FileCenter:000005";
    public const string FileCenterOnlyFolderCanBeManaged = "PrivateCloudDrive:FileCenter:000006";
    public const string FileCenterInvalidFileName = "PrivateCloudDrive:FileCenter:000007";
    public const string FileCenterFileTooLarge = "PrivateCloudDrive:FileCenter:000008";
    public const string FileCenterStorageQuotaExceeded = "PrivateCloudDrive:FileCenter:000009";
    public const string FileCenterOnlyFileCanBeDownloaded = "PrivateCloudDrive:FileCenter:000010";
    public const string FileCenterBlobObjectNotFound = "PrivateCloudDrive:FileCenter:000011";
    public const string FileCenterUploadSessionNotFound = "PrivateCloudDrive:FileCenter:000012";
    public const string FileCenterInvalidUploadSession = "PrivateCloudDrive:FileCenter:000013";
    public const string FileCenterInvalidUploadSessionState = "PrivateCloudDrive:FileCenter:000014";
    public const string FileCenterInvalidUploadChunkIndex = "PrivateCloudDrive:FileCenter:000015";
    public const string FileCenterUploadChunkSizeMismatch = "PrivateCloudDrive:FileCenter:000016";
    public const string FileCenterUploadSessionIncomplete = "PrivateCloudDrive:FileCenter:000017";
    public const string FileCenterUploadSessionHashMismatch = "PrivateCloudDrive:FileCenter:000018";
    public const string FileCenterThumbnailNotFound = "PrivateCloudDrive:FileCenter:000019";
    public const string FileCenterShareNotFound = "PrivateCloudDrive:FileCenter:000020";
    public const string FileCenterShareExpired = "PrivateCloudDrive:FileCenter:000021";
    public const string FileCenterSharePasswordRequired = "PrivateCloudDrive:FileCenter:000022";
    public const string FileCenterSharePasswordInvalid = "PrivateCloudDrive:FileCenter:000023";
    public const string FileCenterShareDownloadDisabled = "PrivateCloudDrive:FileCenter:000024";
    public const string FileCenterTagAlreadyExists = "PrivateCloudDrive:FileCenter:000025";
    public const string FileCenterTagNotFound = "PrivateCloudDrive:FileCenter:000026";
    public const string WeChatDisabled = "PrivateCloudDrive:MobileAuth:000001";
    public const string WeChatCodeExchangeFailed = "PrivateCloudDrive:MobileAuth:000002";
    public const string WeChatBindingRequired = "PrivateCloudDrive:MobileAuth:000003";
    public const string WeChatAlreadyBound = "PrivateCloudDrive:MobileAuth:000004";
    public const string WeChatBindingTicketNotFound = "PrivateCloudDrive:MobileAuth:000005";
    public const string WeChatUnbindNotAllowed = "PrivateCloudDrive:MobileAuth:000006";
    public const string WeChatRateLimited = "PrivateCloudDrive:MobileAuth:000007";
}
