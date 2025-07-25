using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaxProStore.Migrations
{
    /// <inheritdoc />
    public partial class basededatos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FormInstances_Templates_TemplateId",
                table: "FormInstances");

            migrationBuilder.AlterColumn<string>(
                name: "CustomTitle",
                table: "FormInstances",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<Guid>(
                name: "TemplateId1",
                table: "FormInstances",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormInstances_TemplateId1",
                table: "FormInstances",
                column: "TemplateId1");

            migrationBuilder.AddForeignKey(
                name: "FK_FormInstances_Templates_TemplateId",
                table: "FormInstances",
                column: "TemplateId",
                principalTable: "Templates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FormInstances_Templates_TemplateId1",
                table: "FormInstances",
                column: "TemplateId1",
                principalTable: "Templates",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FormInstances_Templates_TemplateId",
                table: "FormInstances");

            migrationBuilder.DropForeignKey(
                name: "FK_FormInstances_Templates_TemplateId1",
                table: "FormInstances");

            migrationBuilder.DropIndex(
                name: "IX_FormInstances_TemplateId1",
                table: "FormInstances");

            migrationBuilder.DropColumn(
                name: "TemplateId1",
                table: "FormInstances");

            migrationBuilder.AlterColumn<string>(
                name: "CustomTitle",
                table: "FormInstances",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddForeignKey(
                name: "FK_FormInstances_Templates_TemplateId",
                table: "FormInstances",
                column: "TemplateId",
                principalTable: "Templates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
