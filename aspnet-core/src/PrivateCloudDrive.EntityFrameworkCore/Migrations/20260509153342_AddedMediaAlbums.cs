using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrivateCloudDrive.Migrations
{
    /// <inheritdoc />
    public partial class AddedMediaAlbums : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppFileCenterMediaAlbumItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    AlbumId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppFileCenterMediaAlbumItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppFileCenterMediaAlbums",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CoverFileNodeId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_AppFileCenterMediaAlbums", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppFileCenterMediaAssets_TenantId_OwnerId_ProcessStatus",
                table: "AppFileCenterMediaAssets",
                columns: new[] { "TenantId", "OwnerId", "ProcessStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_AppFileCenterMediaAssets_TenantId_OwnerId_TakenAt",
                table: "AppFileCenterMediaAssets",
                columns: new[] { "TenantId", "OwnerId", "TakenAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AppFileCenterMediaAlbumItems_AlbumId_FileNodeId",
                table: "AppFileCenterMediaAlbumItems",
                columns: new[] { "AlbumId", "FileNodeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppFileCenterMediaAlbumItems_TenantId_OwnerId_AlbumId",
                table: "AppFileCenterMediaAlbumItems",
                columns: new[] { "TenantId", "OwnerId", "AlbumId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppFileCenterMediaAlbumItems_TenantId_OwnerId_FileNodeId",
                table: "AppFileCenterMediaAlbumItems",
                columns: new[] { "TenantId", "OwnerId", "FileNodeId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppFileCenterMediaAlbums_TenantId_OwnerId_LastModificationT~",
                table: "AppFileCenterMediaAlbums",
                columns: new[] { "TenantId", "OwnerId", "LastModificationTime" });

            migrationBuilder.CreateIndex(
                name: "IX_AppFileCenterMediaAlbums_TenantId_OwnerId_NormalizedName",
                table: "AppFileCenterMediaAlbums",
                columns: new[] { "TenantId", "OwnerId", "NormalizedName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppFileCenterMediaAlbumItems");

            migrationBuilder.DropTable(
                name: "AppFileCenterMediaAlbums");

            migrationBuilder.DropIndex(
                name: "IX_AppFileCenterMediaAssets_TenantId_OwnerId_ProcessStatus",
                table: "AppFileCenterMediaAssets");

            migrationBuilder.DropIndex(
                name: "IX_AppFileCenterMediaAssets_TenantId_OwnerId_TakenAt",
                table: "AppFileCenterMediaAssets");
        }
    }
}
