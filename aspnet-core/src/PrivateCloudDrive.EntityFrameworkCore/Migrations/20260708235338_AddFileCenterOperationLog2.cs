using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrivateCloudDrive.Migrations
{
    /// <inheritdoc />
    public partial class AddFileCenterOperationLog2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppFileCenterOperationLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    FileNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    MediaAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    StatusBefore = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    StatusAfter = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OperatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppFileCenterOperationLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppFileCenterOperationLogs_TenantId_FileNodeId",
                table: "AppFileCenterOperationLogs",
                columns: new[] { "TenantId", "FileNodeId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppFileCenterOperationLogs_TenantId_OperatorUserId_Creation~",
                table: "AppFileCenterOperationLogs",
                columns: new[] { "TenantId", "OperatorUserId", "CreationTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppFileCenterOperationLogs");
        }
    }
}
