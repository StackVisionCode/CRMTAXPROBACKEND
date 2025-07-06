using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace signature.Migrations
{
    /// <inheritdoc />
    public partial class nueva : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Consent_button_text",
                table: "Signers",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Consent_text",
                table: "Signers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Consent_button_text",
                table: "Signers");

            migrationBuilder.DropColumn(
                name: "Consent_text",
                table: "Signers");
        }
    }
}
