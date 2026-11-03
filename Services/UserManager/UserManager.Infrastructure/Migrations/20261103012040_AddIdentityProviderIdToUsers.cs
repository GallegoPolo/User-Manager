using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityProviderIdToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM \"Users\";");

            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "IdentityProviderId",
                table: "Users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IdentityProviderId",
                table: "Users",
                column: "IdentityProviderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_IdentityProviderId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IdentityProviderId",
                table: "Users");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }
    }
}
