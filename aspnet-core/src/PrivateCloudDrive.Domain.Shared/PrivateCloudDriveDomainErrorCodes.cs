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
}
