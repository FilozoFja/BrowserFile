using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrowserFile.Migrations
{
    /// <inheritdoc />
    public partial class FixingFileSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoredFiles_UserId_Name_FolderId",
                table: "StoredFiles");

            migrationBuilder.CreateIndex(
                name: "IX_StoredFiles_UserId",
                table: "StoredFiles",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoredFiles_UserId",
                table: "StoredFiles");

            migrationBuilder.CreateIndex(
                name: "IX_StoredFiles_UserId_Name_FolderId",
                table: "StoredFiles",
                columns: new[] { "UserId", "Name", "FolderId" },
                unique: true);
        }
    }
}
