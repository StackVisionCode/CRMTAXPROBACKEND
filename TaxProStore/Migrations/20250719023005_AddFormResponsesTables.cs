using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaxProStore.Migrations
{
    /// <inheritdoc />
    public partial class AddFormResponsesTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                table: "Products",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "TotalRatings",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "FormInstances",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeleteAt",
                table: "FormInstances",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "FormInstances",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "TotalRatings",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "FormInstances");

            migrationBuilder.DropColumn(
                name: "DeleteAt",
                table: "FormInstances");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "FormInstances");
        }
    }
}
