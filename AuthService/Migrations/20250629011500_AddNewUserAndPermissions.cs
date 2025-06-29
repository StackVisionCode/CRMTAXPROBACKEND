using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AuthService.Migrations
{
    /// <inheritdoc />
    public partial class AddNewUserAndPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Companies",
                columns: new[] { "Id", "Address", "Brand", "CompanyName", "DeleteAt", "Description", "FullName", "Phone", "UpdatedAt", "UserLimit" },
                values: new object[] { new Guid("770e8400-e29b-41d4-a716-556655441000"), "Calle C, Brisa Oriental VIII", "https://images5.example.com/", "StackVsion Sofwatre S.R.L.", null, "Sofwatre Developers Assembly.", "Vision Software", "8298981594", null, 25 });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Code", "DeleteAt", "Description", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("550e8400-e29b-41d4-a716-446655440027"), "Customer.DisableLogin", null, null, "Disable customer login", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440028"), "Customer.EnableLogin", null, null, "Enable customer login", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440029"), "Sessions.Read", null, null, "Read sessions", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440030"), "Dependent.Create", null, null, "Create dependent", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440031"), "Dependent.Update", null, null, "Update dependent", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440032"), "Dependent.Delete", null, null, "Delete dependent", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440033"), "Dependent.Read", null, null, "Read dependent", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440034"), "Dependent.Viewer", null, null, "View dependent", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440035"), "TaxInformation.Create", null, null, "Create tax info", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440036"), "TaxInformation.Update", null, null, "Update tax info", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440037"), "TaxInformation.Delete", null, null, "Delete tax info", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440038"), "TaxInformation.Read", null, null, "Read tax info", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440039"), "TaxInformation.Viewer", null, null, "View tax info", null }
                });

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("660e8400-e29b-41d4-a716-446655450026"),
                column: "RoleId",
                value: new Guid("550e8400-e29b-41d4-a716-446655441001"));

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Id", "DeleteAt", "PermissionId", "RoleId", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("770e8400-e29b-41d4-a716-556655450026"), null, new Guid("550e8400-e29b-41d4-a716-446655440026"), new Guid("550e8400-e29b-41d4-a716-446655441004"), null },
                    { new Guid("770e8400-e29b-41d4-a716-556655460000"), null, new Guid("550e8400-e29b-41d4-a716-446655440011"), new Guid("550e8400-e29b-41d4-a716-446655441003"), null },
                    { new Guid("770e8400-e29b-41d4-a716-556655460001"), null, new Guid("550e8400-e29b-41d4-a716-446655440012"), new Guid("550e8400-e29b-41d4-a716-446655441003"), null },
                    { new Guid("770e8400-e29b-41d4-a716-556655460002"), null, new Guid("550e8400-e29b-41d4-a716-446655440013"), new Guid("550e8400-e29b-41d4-a716-446655441003"), null },
                    { new Guid("770e8400-e29b-41d4-a716-556655460003"), null, new Guid("550e8400-e29b-41d4-a716-446655440015"), new Guid("550e8400-e29b-41d4-a716-446655441003"), null },
                    { new Guid("770e8400-e29b-41d4-a716-556655460006"), null, new Guid("550e8400-e29b-41d4-a716-446655440001"), new Guid("550e8400-e29b-41d4-a716-446655441003"), null },
                    { new Guid("770e8400-e29b-41d4-a716-556655460007"), null, new Guid("550e8400-e29b-41d4-a716-446655440004"), new Guid("550e8400-e29b-41d4-a716-446655441003"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450027"), null, new Guid("550e8400-e29b-41d4-a716-446655440027"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450028"), null, new Guid("550e8400-e29b-41d4-a716-446655440028"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450029"), null, new Guid("550e8400-e29b-41d4-a716-446655440029"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450030"), null, new Guid("550e8400-e29b-41d4-a716-446655440030"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450031"), null, new Guid("550e8400-e29b-41d4-a716-446655440031"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450032"), null, new Guid("550e8400-e29b-41d4-a716-446655440032"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450033"), null, new Guid("550e8400-e29b-41d4-a716-446655440033"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450034"), null, new Guid("550e8400-e29b-41d4-a716-446655440034"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450035"), null, new Guid("550e8400-e29b-41d4-a716-446655440035"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450036"), null, new Guid("550e8400-e29b-41d4-a716-446655440036"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450037"), null, new Guid("550e8400-e29b-41d4-a716-446655440037"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450038"), null, new Guid("550e8400-e29b-41d4-a716-446655440038"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450039"), null, new Guid("550e8400-e29b-41d4-a716-446655440039"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("770e8400-e29b-41d4-a716-556655460004"), null, new Guid("550e8400-e29b-41d4-a716-446655440027"), new Guid("550e8400-e29b-41d4-a716-446655441003"), null },
                    { new Guid("770e8400-e29b-41d4-a716-556655460005"), null, new Guid("550e8400-e29b-41d4-a716-446655440028"), new Guid("550e8400-e29b-41d4-a716-446655441003"), null },
                    { new Guid("770e8400-e29b-41d4-a716-556655460008"), null, new Guid("550e8400-e29b-41d4-a716-446655440030"), new Guid("550e8400-e29b-41d4-a716-446655441003"), null },
                    { new Guid("770e8400-e29b-41d4-a716-556655460009"), null, new Guid("550e8400-e29b-41d4-a716-446655440031"), new Guid("550e8400-e29b-41d4-a716-446655441003"), null },
                    { new Guid("770e8400-e29b-41d4-a716-556655460010"), null, new Guid("550e8400-e29b-41d4-a716-446655440032"), new Guid("550e8400-e29b-41d4-a716-446655441003"), null },
                    { new Guid("770e8400-e29b-41d4-a716-556655460011"), null, new Guid("550e8400-e29b-41d4-a716-446655440033"), new Guid("550e8400-e29b-41d4-a716-446655441003"), null },
                    { new Guid("770e8400-e29b-41d4-a716-556655460012"), null, new Guid("550e8400-e29b-41d4-a716-446655440034"), new Guid("550e8400-e29b-41d4-a716-446655441003"), null },
                    { new Guid("770e8400-e29b-41d4-a716-556655460013"), null, new Guid("550e8400-e29b-41d4-a716-446655440035"), new Guid("550e8400-e29b-41d4-a716-446655441003"), null },
                    { new Guid("770e8400-e29b-41d4-a716-556655460014"), null, new Guid("550e8400-e29b-41d4-a716-446655440036"), new Guid("550e8400-e29b-41d4-a716-446655441003"), null },
                    { new Guid("770e8400-e29b-41d4-a716-556655460015"), null, new Guid("550e8400-e29b-41d4-a716-446655440037"), new Guid("550e8400-e29b-41d4-a716-446655441003"), null },
                    { new Guid("770e8400-e29b-41d4-a716-556655460016"), null, new Guid("550e8400-e29b-41d4-a716-446655440038"), new Guid("550e8400-e29b-41d4-a716-446655441003"), null },
                    { new Guid("770e8400-e29b-41d4-a716-556655460017"), null, new Guid("550e8400-e29b-41d4-a716-446655440039"), new Guid("550e8400-e29b-41d4-a716-446655441003"), null }
                });

            migrationBuilder.InsertData(
                table: "TaxUsers",
                columns: new[] { "Id", "CompanyId", "Confirm", "ConfirmToken", "DeleteAt", "Domain", "Email", "Factor2", "IsActive", "Otp", "OtpExpires", "OtpVerified", "Password", "ResetPasswordExpires", "ResetPasswordToken", "UpdatedAt" },
                values: new object[] { new Guid("880e8400-e29b-41d4-a716-556655441000"), new Guid("770e8400-e29b-41d4-a716-556655441000"), true, null, null, "stackvision", "stackvisionsoftware@gmail.com", null, true, null, null, false, "zBLVJHyDUQKSp3ZYdgIeOEDnoeD61Zg566QoP2165AQAPHxzvJlAWjt1dV+Qinc7", null, null, null });

            migrationBuilder.InsertData(
                table: "UserRoles",
                columns: new[] { "Id", "DeleteAt", "RoleId", "TaxUserId", "UpdatedAt" },
                values: new object[] { new Guid("880e8400-e29b-41d4-a716-556655442000"), null, new Guid("550e8400-e29b-41d4-a716-446655441001"), new Guid("880e8400-e29b-41d4-a716-556655441000"), null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("660e8400-e29b-41d4-a716-446655450027"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("660e8400-e29b-41d4-a716-446655450028"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("660e8400-e29b-41d4-a716-446655450029"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("660e8400-e29b-41d4-a716-446655450030"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("660e8400-e29b-41d4-a716-446655450031"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("660e8400-e29b-41d4-a716-446655450032"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("660e8400-e29b-41d4-a716-446655450033"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("660e8400-e29b-41d4-a716-446655450034"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("660e8400-e29b-41d4-a716-446655450035"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("660e8400-e29b-41d4-a716-446655450036"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("660e8400-e29b-41d4-a716-446655450037"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("660e8400-e29b-41d4-a716-446655450038"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("660e8400-e29b-41d4-a716-446655450039"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("770e8400-e29b-41d4-a716-556655450026"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("770e8400-e29b-41d4-a716-556655460000"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("770e8400-e29b-41d4-a716-556655460001"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("770e8400-e29b-41d4-a716-556655460002"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("770e8400-e29b-41d4-a716-556655460003"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("770e8400-e29b-41d4-a716-556655460004"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("770e8400-e29b-41d4-a716-556655460005"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("770e8400-e29b-41d4-a716-556655460006"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("770e8400-e29b-41d4-a716-556655460007"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("770e8400-e29b-41d4-a716-556655460008"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("770e8400-e29b-41d4-a716-556655460009"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("770e8400-e29b-41d4-a716-556655460010"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("770e8400-e29b-41d4-a716-556655460011"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("770e8400-e29b-41d4-a716-556655460012"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("770e8400-e29b-41d4-a716-556655460013"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("770e8400-e29b-41d4-a716-556655460014"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("770e8400-e29b-41d4-a716-556655460015"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("770e8400-e29b-41d4-a716-556655460016"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("770e8400-e29b-41d4-a716-556655460017"));

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: new Guid("880e8400-e29b-41d4-a716-556655442000"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440027"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440028"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440029"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440030"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440031"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440032"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440033"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440034"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440035"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440036"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440037"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440038"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440039"));

            migrationBuilder.DeleteData(
                table: "TaxUsers",
                keyColumn: "Id",
                keyValue: new Guid("880e8400-e29b-41d4-a716-556655441000"));

            migrationBuilder.DeleteData(
                table: "Companies",
                keyColumn: "Id",
                keyValue: new Guid("770e8400-e29b-41d4-a716-556655441000"));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("660e8400-e29b-41d4-a716-446655450026"),
                column: "RoleId",
                value: new Guid("550e8400-e29b-41d4-a716-446655441004"));
        }
    }
}
