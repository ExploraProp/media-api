using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Media.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MediaFile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerType = table.Column<int>(type: "int", nullable: false),
                    MediaType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    QuarantinePath = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    PublicUrl = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    ContentType = table.Column<int>(type: "int", nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ScanAttempts = table.Column<int>(type: "int", nullable: false),
                    UploadedByClientUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaFile", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MediaPolicy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerType = table.Column<int>(type: "int", nullable: false),
                    MaxFileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    AllowedContentTypes = table.Column<int>(type: "int", nullable: false),
                    MaxMediaCount = table.Column<int>(type: "int", nullable: false),
                    AccessLevel = table.Column<int>(type: "int", nullable: false),
                    WatermarkEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaPolicy", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxEvent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AggregateType = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AggregateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    EventData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxEvent", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MediaVariant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MediaFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VariantType = table.Column<int>(type: "int", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    Width = table.Column<int>(type: "int", nullable: true),
                    Height = table.Column<int>(type: "int", nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaVariant", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaVariant_MediaFile_MediaFileId",
                        column: x => x.MediaFileId,
                        principalTable: "MediaFile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MediaFile_OwnerId",
                table: "MediaFile",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaFile_OwnerId_OwnerType",
                table: "MediaFile",
                columns: new[] { "OwnerId", "OwnerType" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaFile_Status",
                table: "MediaFile",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_MediaPolicy_OwnerType",
                table: "MediaPolicy",
                column: "OwnerType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaVariant_MediaFileId",
                table: "MediaVariant",
                column: "MediaFileId");

            migrationBuilder.CreateIndex(
                name: "UX_MediaVariant_MediaFileId_VariantType",
                table: "MediaVariant",
                columns: new[] { "MediaFileId", "VariantType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvent_AggregateId",
                table: "OutboxEvent",
                column: "AggregateId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvent_IsPublished",
                table: "OutboxEvent",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvent_Unpublished",
                table: "OutboxEvent",
                columns: new[] { "IsPublished", "RetryCount", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MediaPolicy");

            migrationBuilder.DropTable(
                name: "MediaVariant");

            migrationBuilder.DropTable(
                name: "OutboxEvent");

            migrationBuilder.DropTable(
                name: "MediaFile");
        }
    }
}
