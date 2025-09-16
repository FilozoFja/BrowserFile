using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrowserFile.Migrations
{
    /// <inheritdoc />
    public partial class StoredFileChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsInTrash",
                table: "StoredFiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsInTrash",
                table: "StoredFiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
