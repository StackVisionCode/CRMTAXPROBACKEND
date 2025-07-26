using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmailServices.Migrations
{
    /// <inheritdoc />
    public partial class FixingDuplicateEmails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_IncomingEmails_MessageId_ConfigId_Unique",
                table: "IncomingEmails",
                columns: new[] { "MessageId", "ConfigId" },
                unique: true,
                filter: "[MessageId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IncomingEmails_MessageId_ConfigId_Unique",
                table: "IncomingEmails");
        }
    }
}
