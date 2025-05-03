using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AuthService.Migrations
{
    /// <inheritdoc />
    public partial class AddingDataCarlosComputer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "CreatedAt", "DeleteAt", "Description", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, null, null, null, "Write", null },
                    { 2, null, null, null, "Reader", null },
                    { 3, null, null, null, "View", null },
                    { 4, null, null, null, "Delete", null },
                    { 5, null, null, null, "Update", null }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedAt", "DeleteAt", "Description", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, null, null, "Has full access to all system features, settings, and user management. Responsible for maintaining and overseeing the platform.", "Administrator", null },
                    { 2, null, null, "Has limited access to the system, can view and interact with allowed features based on their permissions. Typically focuses on using the core functionality", "User", null }
                });

            migrationBuilder.InsertData(
                table: "TaxUserTypes",
                columns: new[] { "Id", "CreatedAt", "DeleteAt", "Description", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, null, null, "SuperUsuario", "Owner", null },
                    { 2, null, null, "Cliente", "Client", null },
                    { 3, null, null, "Empleado", "Staff", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "TaxUserTypes",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "TaxUserTypes",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "TaxUserTypes",
                keyColumn: "Id",
                keyValue: 3);
        }
    }
}
