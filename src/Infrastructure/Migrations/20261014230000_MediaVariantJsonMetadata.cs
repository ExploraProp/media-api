using Media.Infrastructure.Schema;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Media.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MediaVariantJsonMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "MediaVariant",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE [MediaVariant]
                SET [Metadata] = N'{"width":' + CAST([Width] AS nvarchar(20)) + N',"height":' + CAST([Height] AS nvarchar(20)) + N'}'
                WHERE [Width] IS NOT NULL AND [Height] IS NOT NULL;
                """);

            migrationBuilder.DropColumn(
                name: "Width",
                table: "MediaVariant");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "MediaVariant");

            migrationBuilder.Sql("""
                ALTER TABLE [MediaVariant] ADD
                    [MetadataWidth] AS (TRY_CONVERT(int, JSON_VALUE([Metadata], N'$.width'))) PERSISTED,
                    [MetadataHeight] AS (TRY_CONVERT(int, JSON_VALUE([Metadata], N'$.height'))) PERSISTED;
                """);

            // SQL Server disallows filtered indexes whose predicate references a computed column.
            migrationBuilder.Sql($"""
                CREATE NONCLUSTERED INDEX [{MediaConstraints.MediaVariantMetadataWidthIndex}]
                ON [MediaVariant] ([MetadataWidth]);
                """);

            migrationBuilder.Sql($"""
                CREATE NONCLUSTERED INDEX [{MediaConstraints.MediaVariantMetadataHeightIndex}]
                ON [MediaVariant] ([MetadataHeight]);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                DROP INDEX IF EXISTS [{MediaConstraints.MediaVariantMetadataWidthIndex}] ON [MediaVariant];
                """);

            migrationBuilder.Sql($"""
                DROP INDEX IF EXISTS [{MediaConstraints.MediaVariantMetadataHeightIndex}] ON [MediaVariant];
                """);

            migrationBuilder.Sql("""
                ALTER TABLE [MediaVariant] DROP COLUMN [MetadataWidth];
                ALTER TABLE [MediaVariant] DROP COLUMN [MetadataHeight];
                """);

            migrationBuilder.AddColumn<int>(
                name: "Width",
                table: "MediaVariant",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Height",
                table: "MediaVariant",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE [MediaVariant]
                SET
                    [Width] = TRY_CONVERT(int, JSON_VALUE([Metadata], N'$.width')),
                    [Height] = TRY_CONVERT(int, JSON_VALUE([Metadata], N'$.height'))
                WHERE [Metadata] IS NOT NULL;
                """);

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "MediaVariant");
        }
    }
}
