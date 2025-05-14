using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SignDocuTax.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExternalSigners_RequirementSignatures_RequirementSignatureId1",
                table: "ExternalSigners");

            migrationBuilder.RenameColumn(
                name: "RequirementSignatureId1",
                table: "ExternalSigners",
                newName: "RequirementSignatureId");

            migrationBuilder.RenameIndex(
                name: "IX_ExternalSigners_RequirementSignatureId1",
                table: "ExternalSigners",
                newName: "IX_ExternalSigners_RequirementSignatureId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExternalSigners_RequirementSignatures_RequirementSignatureId",
                table: "ExternalSigners",
                column: "RequirementSignatureId",
                principalTable: "RequirementSignatures",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExternalSigners_RequirementSignatures_RequirementSignatureId",
                table: "ExternalSigners");

            migrationBuilder.RenameColumn(
                name: "RequirementSignatureId",
                table: "ExternalSigners",
                newName: "RequirementSignatureId1");

            migrationBuilder.RenameIndex(
                name: "IX_ExternalSigners_RequirementSignatureId",
                table: "ExternalSigners",
                newName: "IX_ExternalSigners_RequirementSignatureId1");

            migrationBuilder.AddForeignKey(
                name: "FK_ExternalSigners_RequirementSignatures_RequirementSignatureId1",
                table: "ExternalSigners",
                column: "RequirementSignatureId1",
                principalTable: "RequirementSignatures",
                principalColumn: "Id");
        }
    }
}
