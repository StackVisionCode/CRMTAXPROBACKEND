using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AuthService.Migrations
{
    /// <inheritdoc />
    public partial class RABCRolesPermMigrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaxUsers_Roles_RoleId",
                table: "TaxUsers");

            migrationBuilder.DropIndex(
                name: "IX_TaxUsers_RoleId",
                table: "TaxUsers");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "TaxUsers");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TaxUsers",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TaxUserProfiles",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Sessions",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Roles",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "RolePermissions",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Permissions",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "CustomerSessions",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Companies",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CustomerRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_TaxUsers_TaxUserId",
                        column: x => x.TaxUserId,
                        principalTable: "TaxUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Code", "DeleteAt", "Description", "Name", "UpdatedAt" },
                values: new object[] { new Guid("550e8400-e29b-41d4-a716-446655440026"), "Customer.SelfRead", null, null, "Read own profile", null });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Id", "DeleteAt", "PermissionId", "RoleId", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("660e8400-e29b-41d4-a716-446655450001"), null, new Guid("550e8400-e29b-41d4-a716-446655440001"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450002"), null, new Guid("550e8400-e29b-41d4-a716-446655440002"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450003"), null, new Guid("550e8400-e29b-41d4-a716-446655440003"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450004"), null, new Guid("550e8400-e29b-41d4-a716-446655440004"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450005"), null, new Guid("550e8400-e29b-41d4-a716-446655440005"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450006"), null, new Guid("550e8400-e29b-41d4-a716-446655440006"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450007"), null, new Guid("550e8400-e29b-41d4-a716-446655440007"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450008"), null, new Guid("550e8400-e29b-41d4-a716-446655440008"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450009"), null, new Guid("550e8400-e29b-41d4-a716-446655440009"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450010"), null, new Guid("550e8400-e29b-41d4-a716-446655440010"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450011"), null, new Guid("550e8400-e29b-41d4-a716-446655440011"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450012"), null, new Guid("550e8400-e29b-41d4-a716-446655440012"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450013"), null, new Guid("550e8400-e29b-41d4-a716-446655440013"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450014"), null, new Guid("550e8400-e29b-41d4-a716-446655440014"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450015"), null, new Guid("550e8400-e29b-41d4-a716-446655440015"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450016"), null, new Guid("550e8400-e29b-41d4-a716-446655440016"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450017"), null, new Guid("550e8400-e29b-41d4-a716-446655440017"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450018"), null, new Guid("550e8400-e29b-41d4-a716-446655440018"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450019"), null, new Guid("550e8400-e29b-41d4-a716-446655440019"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450020"), null, new Guid("550e8400-e29b-41d4-a716-446655440020"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450021"), null, new Guid("550e8400-e29b-41d4-a716-446655440021"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450022"), null, new Guid("550e8400-e29b-41d4-a716-446655440022"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450023"), null, new Guid("550e8400-e29b-41d4-a716-446655440023"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450024"), null, new Guid("550e8400-e29b-41d4-a716-446655440024"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450025"), null, new Guid("550e8400-e29b-41d4-a716-446655440025"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450026"), null, new Guid("550e8400-e29b-41d4-a716-446655440026"), new Guid("550e8400-e29b-41d4-a716-446655441004"), null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerRoles_CustomerId_RoleId",
                table: "CustomerRoles",
                columns: new[] { "CustomerId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerRoles_RoleId",
                table: "CustomerRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_TaxUserId_RoleId",
                table: "UserRoles",
                columns: new[] { "TaxUserId", "RoleId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerRoles");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("660e8400-e29b-41d4-a716-446655450001"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("660e8400-e29b-41d4-a716-446655450002"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("660e8400-e29b-41d4-a716-446655450003"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("660e8400-e29b-41d4-a716-446655450004"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("660e8400-e29b-41d4-a716-446655450005"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("660e8400-e29b-41d4-a716-446655450006"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("660e8400-e29b-41d4-a716-446655450007"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("660e8400-e29b-41d4-a716-446655450008"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("660e8400-e29b-41d4-a716-446655450009"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("660e8400-e29b-41d4-a716-446655450010"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("660e8400-e29b-41d4-a716-446655450011"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("660e8400-e29b-41d4-a716-446655450012"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("660e8400-e29b-41d4-a716-446655450013"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("660e8400-e29b-41d4-a716-446655450014"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("660e8400-e29b-41d4-a716-446655450015"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("660e8400-e29b-41d4-a716-446655450016"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("660e8400-e29b-41d4-a716-446655450017"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("660e8400-e29b-41d4-a716-446655450018"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("660e8400-e29b-41d4-a716-446655450019"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("660e8400-e29b-41d4-a716-446655450020"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("660e8400-e29b-41d4-a716-446655450021"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("660e8400-e29b-41d4-a716-446655450022"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("660e8400-e29b-41d4-a716-446655450023"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("660e8400-e29b-41d4-a716-446655450024"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("660e8400-e29b-41d4-a716-446655450025"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("660e8400-e29b-41d4-a716-446655450026"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("550e8400-e29b-41d4-a716-446655440026"));

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TaxUsers");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TaxUserProfiles");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "CustomerSessions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Companies");

            migrationBuilder.AddColumn<Guid>(
                name: "RoleId",
                table: "TaxUsers",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_TaxUsers_RoleId",
                table: "TaxUsers",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_TaxUsers_Roles_RoleId",
                table: "TaxUsers",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
