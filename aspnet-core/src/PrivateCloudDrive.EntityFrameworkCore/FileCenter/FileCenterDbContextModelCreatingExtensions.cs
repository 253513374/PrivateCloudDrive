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

        builder.Entity<BlobObject>(b =>
        {
            b.ToTable(FileCenterDbProperties.DbTablePrefix + "BlobObjects", FileCenterDbProperties.DbSchema);
            b.ConfigureByConvention();

            b.Property(blob => blob.OwnerId).IsRequired();
            b.Property(blob => blob.BlobName).IsRequired().HasMaxLength(BlobObjectConsts.MaxBlobNameLength);
            b.Property(blob => blob.FileName).IsRequired().HasMaxLength(BlobObjectConsts.MaxFileNameLength);
            b.Property(blob => blob.ContentType).HasMaxLength(BlobObjectConsts.MaxContentTypeLength);
            b.Property(blob => blob.Hash).HasMaxLength(BlobObjectConsts.MaxHashLength);

            b.HasIndex(blob => blob.BlobName).IsUnique();
            b.HasIndex(blob => new { blob.TenantId, blob.OwnerId });
        });

        builder.Entity<UploadSession>(b =>
        {
            b.ToTable(FileCenterDbProperties.DbTablePrefix + "UploadSessions", FileCenterDbProperties.DbSchema);
            b.ConfigureByConvention();

            b.Property(session => session.OwnerId).IsRequired();
            b.Property(session => session.FileName).IsRequired().HasMaxLength(UploadSessionConsts.MaxFileNameLength);
            b.Property(session => session.NormalizedFileName).IsRequired().HasMaxLength(UploadSessionConsts.MaxNormalizedFileNameLength);
            b.Property(session => session.ContentType).HasMaxLength(UploadSessionConsts.MaxContentTypeLength);
            b.Property(session => session.Sha256).HasMaxLength(UploadSessionConsts.MaxSha256Length);
            b.Property(session => session.UploadedChunksJson)
                .IsRequired()
                .HasMaxLength(UploadSessionConsts.MaxUploadedChunksJsonLength);
            b.Property(session => session.Status).IsRequired();

            b.HasIndex(session => new { session.TenantId, session.OwnerId, session.Status });
            b.HasIndex(session => new { session.OwnerId, session.ParentId, session.NormalizedFileName });
            b.HasIndex(session => session.ExpirationTime);
        });

        builder.Entity<MediaAsset>(b =>
        {
            b.ToTable(FileCenterDbProperties.DbTablePrefix + "MediaAssets", FileCenterDbProperties.DbSchema);
            b.ConfigureByConvention();

            b.Property(asset => asset.OwnerId).IsRequired();
            b.Property(asset => asset.FileNodeId).IsRequired();
            b.Property(asset => asset.MediaType).IsRequired();
            b.Property(asset => asset.Codec).HasMaxLength(MediaAssetConsts.MaxCodecLength);
            b.Property(asset => asset.ProcessStatus).IsRequired();
            b.Property(asset => asset.ProcessError).HasMaxLength(MediaAssetConsts.MaxProcessErrorLength);

            b.HasIndex(asset => asset.FileNodeId).IsUnique();
            b.HasIndex(asset => new { asset.TenantId, asset.OwnerId, asset.MediaType });
            b.HasIndex(asset => asset.ProcessStatus);
        });
    }
}
