using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrivateCloudDrive.Migrations
{
    /// <inheritdoc />
    public partial class AddedSharesTagsFavorites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFavorite",
                table: "AppFileCenterFileNodes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "AppFileCenterFileNodeTags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    TagId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppFileCenterFileNodeTags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppFileCenterFileShares",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PasswordSalt = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    PasswordHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ExpirationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    AllowDownload = table.Column<bool>(type: "boolean", nullable: false),
                    VisitCount = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppFileCenterFileShares", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppFileCenterFileTags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Color = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppFileCenterFileTags", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppFileCenterFileNodes_TenantId_OwnerId_IsFavorite",
                table: "AppFileCenterFileNodes",
                columns: new[] { "TenantId", "OwnerId", "IsFavorite" });

            migrationBuilder.CreateIndex(
                name: "IX_AppFileCenterFileNodeTags_TenantId_OwnerId_FileNodeId_TagId",
                table: "AppFileCenterFileNodeTags",
                columns: new[] { "TenantId", "OwnerId", "FileNodeId", "TagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppFileCenterFileNodeTags_TenantId_OwnerId_TagId",
                table: "AppFileCenterFileNodeTags",
                columns: new[] { "TenantId", "OwnerId", "TagId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppFileCenterFileShares_ExpirationTime",
                table: "AppFileCenterFileShares",
                column: "ExpirationTime");

            migrationBuilder.CreateIndex(
                name: "IX_AppFileCenterFileShares_TenantId_OwnerId_FileNodeId",
                table: "AppFileCenterFileShares",
                columns: new[] { "TenantId", "OwnerId", "FileNodeId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppFileCenterFileShares_Token",
                table: "AppFileCenterFileShares",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppFileCenterFileTags_TenantId_OwnerId_NormalizedName",
                table: "AppFileCenterFileTags",
                columns: new[] { "TenantId", "OwnerId", "NormalizedName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppFileCenterFileNodeTags");

            migrationBuilder.DropTable(
                name: "AppFileCenterFileShares");

            migrationBuilder.DropTable(
                name: "AppFileCenterFileTags");

            migrationBuilder.DropIndex(
                name: "IX_AppFileCenterFileNodes_TenantId_OwnerId_IsFavorite",
                table: "AppFileCenterFileNodes");

            migrationBuilder.DropColumn(
                name: "IsFavorite",
                table: "AppFileCenterFileNodes");
        }
    }
}
