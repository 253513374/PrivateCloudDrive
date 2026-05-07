using System;
using System.ComponentModel.DataAnnotations;

namespace PrivateCloudDrive.FileCenter;

public class CreateFileShareInput
{
    public Guid FileNodeId { get; set; }

    public DateTime? ExpirationTime { get; set; }

    public bool AllowDownload { get; set; } = true;

    [StringLength(128)]
    public string? Password { get; set; }
}
