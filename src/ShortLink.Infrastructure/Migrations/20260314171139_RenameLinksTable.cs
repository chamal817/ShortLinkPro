using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShortLink.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameLinksTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Links",
                table: "Links");

            migrationBuilder.RenameTable(
                name: "Links",
                newName: "links");

            migrationBuilder.RenameIndex(
                name: "IX_Links_ShortCode",
                table: "links",
                newName: "IX_links_ShortCode");

            migrationBuilder.AddPrimaryKey(
                name: "PK_links",
                table: "links",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_links",
                table: "links");

            migrationBuilder.RenameTable(
                name: "links",
                newName: "Links");

            migrationBuilder.RenameIndex(
                name: "IX_links_ShortCode",
                table: "Links",
                newName: "IX_Links_ShortCode");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Links",
                table: "Links",
                column: "Id");
        }
    }
}
