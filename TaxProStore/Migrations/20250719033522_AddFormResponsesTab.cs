using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaxProStore.Migrations
{
    /// <inheritdoc />
    public partial class AddFormResponsesTab : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FormDefinitionId",
                table: "FormResponses");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FormDefinitionId",
                table: "FormResponses",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }
    }
}
