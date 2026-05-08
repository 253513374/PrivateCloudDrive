using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrivateCloudDrive.EntityFrameworkCore;

#nullable disable

namespace PrivateCloudDrive.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(PrivateCloudDriveDbContext))]
    [Migration("20260508033000_FixedWechatUserBindingUniqueIndexes")]
    public partial class FixedWechatUserBindingUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_WechatUserBindings_AppId_OpenId",
                table: "AppMobileAuthWechatUserBindings");

            migrationBuilder.DropIndex(
                name: "UX_WechatUserBindings_UnionId",
                table: "AppMobileAuthWechatUserBindings");

            migrationBuilder.CreateIndex(
                name: "UX_WechatUserBindings_Host_AppId_OpenId",
                table: "AppMobileAuthWechatUserBindings",
                columns: new[] { "AppId", "OpenId" },
                unique: true,
                filter: "\"TenantId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_WechatUserBindings_Host_UnionId",
                table: "AppMobileAuthWechatUserBindings",
                column: "UnionId",
                unique: true,
                filter: "\"TenantId\" IS NULL AND \"UnionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_WechatUserBindings_Tenant_AppId_OpenId",
                table: "AppMobileAuthWechatUserBindings",
                columns: new[] { "TenantId", "AppId", "OpenId" },
                unique: true,
                filter: "\"TenantId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_WechatUserBindings_Tenant_UnionId",
                table: "AppMobileAuthWechatUserBindings",
                columns: new[] { "TenantId", "UnionId" },
                unique: true,
                filter: "\"TenantId\" IS NOT NULL AND \"UnionId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_WechatUserBindings_Host_AppId_OpenId",
                table: "AppMobileAuthWechatUserBindings");

            migrationBuilder.DropIndex(
                name: "UX_WechatUserBindings_Host_UnionId",
                table: "AppMobileAuthWechatUserBindings");

            migrationBuilder.DropIndex(
                name: "UX_WechatUserBindings_Tenant_AppId_OpenId",
                table: "AppMobileAuthWechatUserBindings");

            migrationBuilder.DropIndex(
                name: "UX_WechatUserBindings_Tenant_UnionId",
                table: "AppMobileAuthWechatUserBindings");

            migrationBuilder.CreateIndex(
                name: "UX_WechatUserBindings_AppId_OpenId",
                table: "AppMobileAuthWechatUserBindings",
                columns: new[] { "TenantId", "AppId", "OpenId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_WechatUserBindings_UnionId",
                table: "AppMobileAuthWechatUserBindings",
                columns: new[] { "TenantId", "UnionId" },
                unique: true);
        }
    }
}
