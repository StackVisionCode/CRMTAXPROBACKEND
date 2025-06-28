using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AuthService.Migrations
{
    /// <inheritdoc />
    public partial class RolesChangedMigrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RolePermissions_Permissions_PermissionsId",
                table: "RolePermissions");

            migrationBuilder.DropIndex(
                name: "IX_RolePermissions_RoleId",
                table: "RolePermissions");

            migrationBuilder.RenameColumn(
                name: "PermissionsId",
                table: "RolePermissions",
                newName: "PermissionId");

            migrationBuilder.RenameIndex(
                name: "IX_RolePermissions_PermissionsId",
                table: "RolePermissions",
                newName: "IX_RolePermissions_PermissionId");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Roles",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Permissions",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440001"),
                columns: new[] { "Code", "Name" },
                values: new object[] { "Permission.Create", "Create Permissions" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440002"),
                columns: new[] { "Code", "Name" },
                values: new object[] { "Permission.Read", "Read Permissions" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440003"),
                columns: new[] { "Code", "Name" },
                values: new object[] { "Permission.View", "View Permissions" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440004"),
                columns: new[] { "Code", "Name" },
                values: new object[] { "Permission.Delete", "Delete Permissions" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440005"),
                columns: new[] { "Code", "Name" },
                values: new object[] { "Permission.Update", "Update Permissions" });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Code", "DeleteAt", "Description", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("550e8400-e29b-41d4-a716-446655440006"), "TaxUser.Create", null, null, "Create TaxUsers", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440007"), "TaxUser.Read", null, null, "Read TaxUsers", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440008"), "TaxUser.View", null, null, "View TaxUsers", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440009"), "TaxUser.Delete", null, null, "Delete TaxUsers", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440010"), "TaxUser.Update", null, null, "Update TaxUsers", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440011"), "Customer.Create", null, null, "Create Customers", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440012"), "Customer.Read", null, null, "Read Customers", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440013"), "Customer.View", null, null, "View Customers", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440014"), "Customer.Delete", null, null, "Delete Customers", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440015"), "Customer.Update", null, null, "Update Customers", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440016"), "Role.Create", null, null, "Create Roles", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440017"), "Role.Read", null, null, "Read Roles", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440018"), "Role.View", null, null, "View Roles", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440019"), "Role.Delete", null, null, "Delete Roles", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440020"), "Role.Update", null, null, "Update Roles", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440021"), "RolePermission.Create", null, null, "Create RolePermissions", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440022"), "RolePermission.Read", null, null, "Read RolePermissions", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440023"), "RolePermission.View", null, null, "View RolePermissions", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440024"), "RolePermission.Delete", null, null, "Delete RolePermissions", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440025"), "RolePermission.Update", null, null, "Update RolePermissions", null }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "DeleteAt", "Description", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("550e8400-e29b-41d4-a716-446655441003"), null, "Has limited access to the system, can view and interact with allowed features based on their permissions. Typically focuses on using the core functionality", "TaxPreparer", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655441004"), null, "Has limited access to the system, can view and interact with allowed features based on their permissions. Typically focuses on using the core functionality", "Customer", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId_PermissionId",
                table: "RolePermissions",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Code",
                table: "Permissions",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_RolePermissions_Permissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId",
                principalTable: "Permissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RolePermissions_Permissions_PermissionId",
                table: "RolePermissions");

            migrationBuilder.DropIndex(
                name: "IX_Roles_Name",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_RolePermissions_RoleId_PermissionId",
                table: "RolePermissions");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_Code",
                table: "Permissions");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440006"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440007"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440008"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440009"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440010"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440011"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440012"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440013"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440014"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440015"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440016"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440017"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440018"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440019"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440020"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440021"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440022"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440023"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440024"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440025"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655441003"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655441004"));

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Permissions");

            migrationBuilder.RenameColumn(
                name: "PermissionId",
                table: "RolePermissions",
                newName: "PermissionsId");

            migrationBuilder.RenameIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                newName: "IX_RolePermissions_PermissionsId");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Roles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440001"),
                column: "Name",
                value: "Write");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440002"),
                column: "Name",
                value: "Reader");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440003"),
                column: "Name",
                value: "View");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440004"),
                column: "Name",
                value: "Delete");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440005"),
                column: "Name",
                value: "Update");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId",
                table: "RolePermissions",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_RolePermissions_Permissions_PermissionsId",
                table: "RolePermissions",
                column: "PermissionsId",
                principalTable: "Permissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
