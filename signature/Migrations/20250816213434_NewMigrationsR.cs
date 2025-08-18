using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace signature.Migrations
{
    /// <inheritdoc />
    public partial class NewMigrationsR : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "SignatureRequests",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByTaxUserId",
                table: "SignatureRequests",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "LastModifiedByTaxUserId",
                table: "SignatureRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SignatureRequests_CompanyId",
                table: "SignatureRequests",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_SignatureRequests_CompanyId_Status",
                table: "SignatureRequests",
                columns: new[] { "CompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SignatureRequests_CreatedByTaxUserId",
                table: "SignatureRequests",
                column: "CreatedByTaxUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SignatureRequests_CompanyId",
                table: "SignatureRequests");

            migrationBuilder.DropIndex(
                name: "IX_SignatureRequests_CompanyId_Status",
                table: "SignatureRequests");

            migrationBuilder.DropIndex(
                name: "IX_SignatureRequests_CreatedByTaxUserId",
                table: "SignatureRequests");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "SignatureRequests");

            migrationBuilder.DropColumn(
                name: "CreatedByTaxUserId",
                table: "SignatureRequests");

            migrationBuilder.DropColumn(
                name: "LastModifiedByTaxUserId",
                table: "SignatureRequests");
        }
    }
}
