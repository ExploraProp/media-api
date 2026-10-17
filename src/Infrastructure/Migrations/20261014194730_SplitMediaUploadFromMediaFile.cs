using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Media.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SplitMediaUploadFromMediaFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MediaUpload",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerType = table.Column<int>(type: "int", nullable: false),
                    MediaType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    QuarantinePath = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    ContentType = table.Column<int>(type: "int", nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ScanAttempts = table.Column<int>(type: "int", nullable: false),
                    UploadedByClientUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaUpload", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MediaUpload_OwnerId",
                table: "MediaUpload",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaUpload_OwnerId_OwnerType",
                table: "MediaUpload",
                columns: new[] { "OwnerId", "OwnerType" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaUpload_Status",
                table: "MediaUpload",
                column: "Status");

            // Pending=0, Scanning=1, Rejected=3, Failed=4 (Approved=2 stays on MediaFile)
            migrationBuilder.Sql(
                """
                INSERT INTO MediaUpload (
                    Id, OwnerId, OwnerType, MediaType, Status, QuarantinePath, ContentType,
                    FileSizeBytes, UploadedAt, ScanAttempts, UploadedByClientUserId,
                    CreatedAt, UpdatedAt, DeletedAt, Version)
                SELECT
                    Id, OwnerId, OwnerType, MediaType, Status, QuarantinePath, ContentType,
                    FileSizeBytes, UploadedAt, ScanAttempts, UploadedByClientUserId,
                    CreatedAt, UpdatedAt, DeletedAt, Version
                FROM MediaFile
                WHERE Status IN (0, 1, 3, 4);

                DELETE FROM MediaVariant
                WHERE MediaFileId IN (SELECT Id FROM MediaFile WHERE Status IN (0, 1, 3, 4));

                DELETE FROM MediaFile
                WHERE Status IN (0, 1, 3, 4);

                -- Ready rows: CreatedAt becomes approval time (replaces ApprovedAt).
                UPDATE MediaFile
                SET CreatedAt = COALESCE(ApprovedAt, CreatedAt);
                """);

            migrationBuilder.DropIndex(
                name: "IX_MediaFile_Status",
                table: "MediaFile");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "MediaFile");

            migrationBuilder.DropColumn(
                name: "QuarantinePath",
                table: "MediaFile");

            migrationBuilder.DropColumn(
                name: "ScanAttempts",
                table: "MediaFile");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "MediaFile");

            migrationBuilder.DropColumn(
                name: "UploadedAt",
                table: "MediaFile");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "MediaFile",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuarantinePath",
                table: "MediaFile",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ScanAttempts",
                table: "MediaFile",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "MediaFile",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<DateTime>(
                name: "UploadedAt",
                table: "MediaFile",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.Sql(
                """
                UPDATE MediaFile
                SET Status = 2, UploadedAt = CreatedAt, ApprovedAt = CreatedAt;

                INSERT INTO MediaFile (
                    Id, OwnerId, OwnerType, MediaType, Status, QuarantinePath, ContentType,
                    FileSizeBytes, UploadedAt, ScanAttempts, UploadedByClientUserId,
                    AccessLevel, PublicUrl, ApprovedAt, CreatedAt, UpdatedAt, DeletedAt, Version)
                SELECT
                    Id, OwnerId, OwnerType, MediaType, Status, QuarantinePath, ContentType,
                    FileSizeBytes, UploadedAt, ScanAttempts, UploadedByClientUserId,
                    1, NULL, NULL, CreatedAt, UpdatedAt, DeletedAt, Version
                FROM MediaUpload;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_MediaFile_Status",
                table: "MediaFile",
                column: "Status");

            migrationBuilder.DropTable(
                name: "MediaUpload");
        }
    }
}
