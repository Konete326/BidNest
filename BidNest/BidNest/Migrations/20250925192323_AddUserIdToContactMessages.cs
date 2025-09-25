using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BidNest.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToContactMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "ContactMessages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ContactMessages_UserId",
                table: "ContactMessages",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ContactMessages_Users_UserId",
                table: "ContactMessages",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContactMessages_Users_UserId",
                table: "ContactMessages");

            migrationBuilder.DropIndex(
                name: "IX_ContactMessages_UserId",
                table: "ContactMessages");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ContactMessages");
        }
    }
}
