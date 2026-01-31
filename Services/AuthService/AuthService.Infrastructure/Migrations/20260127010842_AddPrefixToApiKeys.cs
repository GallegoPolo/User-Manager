using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPrefixToApiKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "KeyHash",
                table: "ApiKeys",
                newName: "SecretHash");

            migrationBuilder.RenameIndex(
                name: "IX_ApiKeys_KeyHash",
                table: "ApiKeys",
                newName: "IX_ApiKeys_SecretHash");

            migrationBuilder.AddColumn<string>(
                name: "Prefix",
                table: "ApiKeys",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Prefix",
                table: "ApiKeys");

            migrationBuilder.RenameColumn(
                name: "SecretHash",
                table: "ApiKeys",
                newName: "KeyHash");

            migrationBuilder.RenameIndex(
                name: "IX_ApiKeys_SecretHash",
                table: "ApiKeys",
                newName: "IX_ApiKeys_KeyHash");
        }
    }
}
