using Microsoft.EntityFrameworkCore;
using PrivateCloudDrive.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace PrivateCloudDrive.FileCenter;

public static class FileCenterDbContextModelCreatingExtensions
{
    public static void ConfigureFileCenter(this ModelBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Entity<FileNode>(b =>
        {
            b.ToTable(FileCenterDbProperties.DbTablePrefix + "FileNodes", FileCenterDbProperties.DbSchema);
            b.ConfigureByConvention();

            b.Property(node => node.OwnerId).IsRequired();
            b.Property(node => node.NodeType).IsRequired();
            b.Property(node => node.Name).IsRequired().HasMaxLength(FileNodeConsts.MaxNameLength);
            b.Property(node => node.NormalizedName).IsRequired().HasMaxLength(FileNodeConsts.MaxNormalizedNameLength);
            b.Property(node => node.ContentType).HasMaxLength(FileNodeConsts.MaxContentTypeLength);
            b.Property(node => node.BlobName).HasMaxLength(FileNodeConsts.MaxBlobNameLength);

            b.HasIndex(node => new { node.TenantId, node.OwnerId, node.ParentId });
            b.HasIndex(node => new { node.OwnerId, node.ParentId, node.NormalizedName })
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false AND \"ParentId\" IS NOT NULL");
            b.HasIndex(node => new { node.OwnerId, node.NormalizedName })
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false AND \"ParentId\" IS NULL");
        });
    }
}
