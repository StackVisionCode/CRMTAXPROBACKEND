using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace signature.Migrations
{
    /// <inheritdoc />
    public partial class AddedAgentConsent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientIp",
                table: "Signers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConsentAgreedAtUtc",
                table: "Signers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SignedAtUtc",
                table: "Signers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "Signers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientIp",
                table: "Signers");

            migrationBuilder.DropColumn(
                name: "ConsentAgreedAtUtc",
                table: "Signers");

            migrationBuilder.DropColumn(
                name: "SignedAtUtc",
                table: "Signers");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "Signers");
        }
    }
}
