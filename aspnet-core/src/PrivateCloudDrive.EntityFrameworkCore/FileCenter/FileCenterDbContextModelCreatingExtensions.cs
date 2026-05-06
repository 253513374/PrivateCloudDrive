using Microsoft.EntityFrameworkCore;
using Volo.Abp;

namespace PrivateCloudDrive.FileCenter;

public static class FileCenterDbContextModelCreatingExtensions
{
    public static void ConfigureFileCenter(this ModelBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        // FileCenter entity mappings will be added by later tasks.
    }
}
