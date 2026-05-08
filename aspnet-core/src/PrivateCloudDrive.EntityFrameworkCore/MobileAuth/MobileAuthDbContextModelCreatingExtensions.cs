using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace PrivateCloudDrive.MobileAuth;

public static class MobileAuthDbContextModelCreatingExtensions
{
    public static void ConfigureMobileAuth(this ModelBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Entity<MobileAuthAuditLog>(b =>
        {
            b.ToTable(MobileAuthDbProperties.DbTablePrefix + "AuditLogs", MobileAuthDbProperties.DbSchema);
            b.ConfigureByConvention();

            b.Property(log => log.UserName).HasMaxLength(MobileAuthAuditLogConsts.MaxUserNameLength);
            b.Property(log => log.Provider).IsRequired().HasMaxLength(MobileAuthAuditLogConsts.MaxProviderLength);
            b.Property(log => log.Action).IsRequired().HasMaxLength(MobileAuthAuditLogConsts.MaxActionLength);
            b.Property(log => log.Result).IsRequired().HasMaxLength(MobileAuthAuditLogConsts.MaxResultLength);
            b.Property(log => log.FailureReason).HasMaxLength(MobileAuthAuditLogConsts.MaxFailureReasonLength);
            b.Property(log => log.ClientId).HasMaxLength(MobileAuthAuditLogConsts.MaxClientIdLength);
            b.Property(log => log.DeviceIdHash).HasMaxLength(MobileAuthAuditLogConsts.MaxDeviceIdHashLength);
            b.Property(log => log.UserAgent).HasMaxLength(MobileAuthAuditLogConsts.MaxUserAgentLength);

            b.HasIndex(log => new { log.TenantId, log.CreationTime });
            b.HasIndex(log => new { log.TenantId, log.UserId, log.CreationTime });
            b.HasIndex(log => new { log.TenantId, log.Action, log.Result });
        });

        builder.Entity<WechatUserBinding>(b =>
        {
            b.ToTable(MobileAuthDbProperties.DbTablePrefix + "WechatUserBindings", MobileAuthDbProperties.DbSchema);
            b.ConfigureByConvention();

            b.Property(binding => binding.AppId).IsRequired().HasMaxLength(WechatUserBindingConsts.MaxAppIdLength);
            b.Property(binding => binding.OpenId).IsRequired().HasMaxLength(WechatUserBindingConsts.MaxOpenIdLength);
            b.Property(binding => binding.UnionId).HasMaxLength(WechatUserBindingConsts.MaxUnionIdLength);
            b.Property(binding => binding.NickName).HasMaxLength(WechatUserBindingConsts.MaxNickNameLength);
            b.Property(binding => binding.AvatarUrl).HasMaxLength(WechatUserBindingConsts.MaxAvatarUrlLength);

            b.HasIndex(binding => new { binding.AppId, binding.OpenId })
                .IsUnique()
                .HasFilter("\"TenantId\" IS NULL")
                .HasDatabaseName("UX_WechatUserBindings_Host_AppId_OpenId");
            b.HasIndex(binding => new { binding.TenantId, binding.AppId, binding.OpenId })
                .IsUnique()
                .HasFilter("\"TenantId\" IS NOT NULL")
                .HasDatabaseName("UX_WechatUserBindings_Tenant_AppId_OpenId");
            b.HasIndex(binding => binding.UnionId)
                .IsUnique()
                .HasFilter("\"TenantId\" IS NULL AND \"UnionId\" IS NOT NULL")
                .HasDatabaseName("UX_WechatUserBindings_Host_UnionId");
            b.HasIndex(binding => new { binding.TenantId, binding.UnionId })
                .IsUnique()
                .HasFilter("\"TenantId\" IS NOT NULL AND \"UnionId\" IS NOT NULL")
                .HasDatabaseName("UX_WechatUserBindings_Tenant_UnionId");
            b.HasIndex(binding => binding.UserId)
                .HasDatabaseName("IX_WechatUserBindings_UserId");
        });
    }
}
