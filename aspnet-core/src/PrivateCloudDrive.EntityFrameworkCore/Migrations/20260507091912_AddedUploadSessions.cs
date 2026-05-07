using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrivateCloudDrive.Migrations
{
    /// <inheritdoc />
    public partial class AddedUploadSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppFileCenterUploadSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    NormalizedFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    TotalSize = table.Column<long>(type: "bigint", nullable: false),
                    ChunkSize = table.Column<int>(type: "integer", nullable: false),
                    TotalChunks = table.Column<int>(type: "integer", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UploadedChunksJson = table.Column<string>(type: "character varying(16384)", maxLength: 16384, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ExpirationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    FileNodeId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_AppFileCenterUploadSessions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppFileCenterUploadSessions_ExpirationTime",
                table: "AppFileCenterUploadSessions",
                column: "ExpirationTime");

            migrationBuilder.CreateIndex(
                name: "IX_AppFileCenterUploadSessions_OwnerId_ParentId_NormalizedFile~",
                table: "AppFileCenterUploadSessions",
                columns: new[] { "OwnerId", "ParentId", "NormalizedFileName" });

            migrationBuilder.CreateIndex(
                name: "IX_AppFileCenterUploadSessions_TenantId_OwnerId_Status",
                table: "AppFileCenterUploadSessions",
                columns: new[] { "TenantId", "OwnerId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppFileCenterUploadSessions");
        }
    }
}
