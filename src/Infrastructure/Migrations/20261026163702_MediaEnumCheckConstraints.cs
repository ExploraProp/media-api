using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Media.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MediaEnumCheckConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_MediaVariant_VariantType",
                table: "MediaVariant",
                sql: "[VariantType] IN (0, 1, 2, 3)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MediaUpload_ContentType",
                table: "MediaUpload",
                sql: "[ContentType] IN (0, 1, 2, 4, 8, 16)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MediaUpload_OwnerType",
                table: "MediaUpload",
                sql: "[OwnerType] IN (0, 1, 2, 3)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MediaUpload_Status",
                table: "MediaUpload",
                sql: "[Status] IN (0, 1, 2, 3, 4)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MediaPolicy_AccessLevel",
                table: "MediaPolicy",
                sql: "[AccessLevel] IN (0, 1)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MediaPolicy_AllowedContentTypes",
                table: "MediaPolicy",
                sql: "[AllowedContentTypes] >= 0 AND [AllowedContentTypes] <= 31");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MediaPolicy_OwnerType",
                table: "MediaPolicy",
                sql: "[OwnerType] IN (0, 1, 2, 3)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MediaFile_ContentType",
                table: "MediaFile",
                sql: "[ContentType] IN (0, 1, 2, 4, 8, 16)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MediaFile_OwnerType",
                table: "MediaFile",
                sql: "[OwnerType] IN (0, 1, 2, 3)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_MediaVariant_VariantType",
                table: "MediaVariant");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MediaUpload_ContentType",
                table: "MediaUpload");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MediaUpload_OwnerType",
                table: "MediaUpload");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MediaUpload_Status",
                table: "MediaUpload");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MediaPolicy_AccessLevel",
                table: "MediaPolicy");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MediaPolicy_AllowedContentTypes",
                table: "MediaPolicy");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MediaPolicy_OwnerType",
                table: "MediaPolicy");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MediaFile_ContentType",
                table: "MediaFile");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MediaFile_OwnerType",
                table: "MediaFile");
        }
    }
}
