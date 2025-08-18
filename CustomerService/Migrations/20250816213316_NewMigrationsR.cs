using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomerService.Migrations
{
    /// <inheritdoc />
    public partial class NewMigrationsR : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContactInfos_PreferredContacts_PreferredContactId",
                table: "ContactInfos");

            migrationBuilder.RenameColumn(
                name: "TaxUserId",
                table: "Customers",
                newName: "CreatedByTaxUserId");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByTaxUserId",
                table: "TaxInformations",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "LastModifiedByTaxUserId",
                table: "TaxInformations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByTaxUserId",
                table: "Dependents",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "LastModifiedByTaxUserId",
                table: "Dependents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "Customers",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "LastModifiedByTaxUserId",
                table: "Customers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByTaxUserId",
                table: "ContactInfos",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "LastModifiedByTaxUserId",
                table: "ContactInfos",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByTaxUserId",
                table: "Addresses",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "LastModifiedByTaxUserId",
                table: "Addresses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxInformations_CreatedBy",
                table: "TaxInformations",
                column: "CreatedByTaxUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Dependents_CreatedBy",
                table: "Dependents",
                column: "CreatedByTaxUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CompanyId",
                table: "Customers",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CreatedBy",
                table: "Customers",
                column: "CreatedByTaxUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactInfos_CreatedBy",
                table: "ContactInfos",
                column: "CreatedByTaxUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_CreatedBy",
                table: "Addresses",
                column: "CreatedByTaxUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ContactInfos_PreferredContacts_PreferredContactId",
                table: "ContactInfos",
                column: "PreferredContactId",
                principalTable: "PreferredContacts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContactInfos_PreferredContacts_PreferredContactId",
                table: "ContactInfos");

            migrationBuilder.DropIndex(
                name: "IX_TaxInformations_CreatedBy",
                table: "TaxInformations");

            migrationBuilder.DropIndex(
                name: "IX_Dependents_CreatedBy",
                table: "Dependents");

            migrationBuilder.DropIndex(
                name: "IX_Customers_CompanyId",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_CreatedBy",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_ContactInfos_CreatedBy",
                table: "ContactInfos");

            migrationBuilder.DropIndex(
                name: "IX_Addresses_CreatedBy",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "CreatedByTaxUserId",
                table: "TaxInformations");

            migrationBuilder.DropColumn(
                name: "LastModifiedByTaxUserId",
                table: "TaxInformations");

            migrationBuilder.DropColumn(
                name: "CreatedByTaxUserId",
                table: "Dependents");

            migrationBuilder.DropColumn(
                name: "LastModifiedByTaxUserId",
                table: "Dependents");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "LastModifiedByTaxUserId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CreatedByTaxUserId",
                table: "ContactInfos");

            migrationBuilder.DropColumn(
                name: "LastModifiedByTaxUserId",
                table: "ContactInfos");

            migrationBuilder.DropColumn(
                name: "CreatedByTaxUserId",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "LastModifiedByTaxUserId",
                table: "Addresses");

            migrationBuilder.RenameColumn(
                name: "CreatedByTaxUserId",
                table: "Customers",
                newName: "TaxUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ContactInfos_PreferredContacts_PreferredContactId",
                table: "ContactInfos",
                column: "PreferredContactId",
                principalTable: "PreferredContacts",
                principalColumn: "Id");
        }
    }
}
