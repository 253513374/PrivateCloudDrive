using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrivateCloudDrive.Migrations
{
    /// <inheritdoc />
    public partial class AddedMediaAssets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppFileCenterMediaAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    MediaType = table.Column<int>(type: "integer", nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: true),
                    Height = table.Column<int>(type: "integer", nullable: true),
                    DurationMilliseconds = table.Column<long>(type: "bigint", nullable: true),
                    Codec = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    TakenAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ThumbnailBlobObjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    PreviewBlobObjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    ProcessStatus = table.Column<int>(type: "integer", nullable: false),
                    ProcessError = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
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
                    table.PrimaryKey("PK_AppFileCenterMediaAssets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppFileCenterMediaAssets_FileNodeId",
                table: "AppFileCenterMediaAssets",
                column: "FileNodeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppFileCenterMediaAssets_ProcessStatus",
                table: "AppFileCenterMediaAssets",
                column: "ProcessStatus");

            migrationBuilder.CreateIndex(
                name: "IX_AppFileCenterMediaAssets_TenantId_OwnerId_MediaType",
                table: "AppFileCenterMediaAssets",
                columns: new[] { "TenantId", "OwnerId", "MediaType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppFileCenterMediaAssets");
        }
    }
}
