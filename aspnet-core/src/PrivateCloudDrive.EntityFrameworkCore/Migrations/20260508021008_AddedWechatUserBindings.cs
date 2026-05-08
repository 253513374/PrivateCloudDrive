using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrivateCloudDrive.Migrations
{
    /// <inheritdoc />
    public partial class AddedWechatUserBindings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppMobileAuthWechatUserBindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OpenId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UnionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    NickName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
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
                    table.PrimaryKey("PK_AppMobileAuthWechatUserBindings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WechatUserBindings_UserId",
                table: "AppMobileAuthWechatUserBindings",
                column: "UserId");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppMobileAuthWechatUserBindings");
        }
    }
}
