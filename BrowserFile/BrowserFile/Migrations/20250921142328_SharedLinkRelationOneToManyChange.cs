using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrowserFile.Migrations
{
    /// <inheritdoc />
    public partial class SharedLinkRelationOneToManyChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SharedLinks_FileId",
                table: "SharedLinks");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "SharedLinks",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.CreateIndex(
                name: "IX_SharedLinks_FileId",
                table: "SharedLinks",
                column: "FileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SharedLinks_FileId",
                table: "SharedLinks");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "SharedLinks",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SharedLinks_FileId",
                table: "SharedLinks",
                column: "FileId",
                unique: true);
        }
    }
}
