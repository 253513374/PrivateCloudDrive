using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrivateCloudDrive.Migrations
{
    /// <inheritdoc />
    public partial class AddedExternalUserBindings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppMobileAuthExternalUserBindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ProviderUserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    AvatarUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LastLoginTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppMobileAuthExternalUserBindings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalUserBindings_UserId_Provider",
                table: "AppMobileAuthExternalUserBindings",
                columns: new[] { "UserId", "Provider" });

            migrationBuilder.CreateIndex(
                name: "UX_ExternalUserBindings_Host_Provider_UserId",
                table: "AppMobileAuthExternalUserBindings",
                columns: new[] { "Provider", "ProviderUserId" },
                unique: true,
                filter: "\"TenantId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_ExternalUserBindings_Tenant_Provider_UserId",
                table: "AppMobileAuthExternalUserBindings",
                columns: new[] { "TenantId", "Provider", "ProviderUserId" },
                unique: true,
                filter: "\"TenantId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppMobileAuthExternalUserBindings");
        }
    }
}
