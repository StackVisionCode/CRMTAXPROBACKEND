using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommLinkServices.Migrations
{
    /// <inheritdoc />
    public partial class Add_OneActiveCallPerConversation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Calls_ConversationId",
                table: "Calls");

            migrationBuilder.CreateIndex(
                name: "UX_Calls_OneActivePerConversation",
                table: "Calls",
                column: "ConversationId",
                unique: true,
                filter: "[EndedAt] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Calls_OneActivePerConversation",
                table: "Calls");

            migrationBuilder.CreateIndex(
                name: "IX_Calls_ConversationId",
                table: "Calls",
                column: "ConversationId");
        }
    }
}
