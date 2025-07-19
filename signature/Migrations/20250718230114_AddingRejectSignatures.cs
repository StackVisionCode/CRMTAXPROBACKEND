using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace signature.Migrations
{
    /// <inheritdoc />
    public partial class AddingRejectSignatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RejectReason",
                table: "Signers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAtUtc",
                table: "Signers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectReason",
                table: "SignatureRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAtUtc",
                table: "SignatureRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RejectedBySignerId",
                table: "SignatureRequests",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RejectReason",
                table: "Signers");

            migrationBuilder.DropColumn(
                name: "RejectedAtUtc",
                table: "Signers");

            migrationBuilder.DropColumn(
                name: "RejectReason",
                table: "SignatureRequests");

            migrationBuilder.DropColumn(
                name: "RejectedAtUtc",
                table: "SignatureRequests");

            migrationBuilder.DropColumn(
                name: "RejectedBySignerId",
                table: "SignatureRequests");
        }
    }
}
