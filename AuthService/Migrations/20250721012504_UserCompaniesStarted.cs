using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AuthService.Migrations
{
    /// <inheritdoc />
    public partial class UserCompaniesStarted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompanyUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Confirm = table.Column<bool>(type: "bit", nullable: true),
                    ConfirmToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResetPasswordToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResetPasswordExpires = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Factor2 = table.Column<bool>(type: "bit", nullable: true),
                    Otp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OtpVerified = table.Column<bool>(type: "bit", nullable: false),
                    OtpExpires = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyUsers_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompanyUserProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhotoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Position = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyUserProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyUserProfiles_CompanyUsers_CompanyUserId",
                        column: x => x.CompanyUserId,
                        principalTable: "CompanyUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompanyUserRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyUserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyUserRoles_CompanyUsers_CompanyUserId",
                        column: x => x.CompanyUserId,
                        principalTable: "CompanyUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompanyUserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompanyUserSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenRequest = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpireTokenRequest = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TokenRefresh = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Latitude = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Longitude = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Device = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRevoke = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyUserSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyUserSessions_CompanyUsers_CompanyUserId",
                        column: x => x.CompanyUserId,
                        principalTable: "CompanyUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Id", "DeleteAt", "PermissionId", "RoleId", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("880e8400-e29b-41d4-a716-556655470000"), null, new Guid("550e8400-e29b-41d4-a716-446655440026"), new Guid("550e8400-e29b-41d4-a716-446655441002"), null },
                    { new Guid("880e8400-e29b-41d4-a716-556655470001"), null, new Guid("550e8400-e29b-41d4-a716-446655440029"), new Guid("550e8400-e29b-41d4-a716-446655441002"), null },
                    { new Guid("880e8400-e29b-41d4-a716-556655470002"), null, new Guid("550e8400-e29b-41d4-a716-446655440003"), new Guid("550e8400-e29b-41d4-a716-446655441002"), null },
                    { new Guid("880e8400-e29b-41d4-a716-556655470003"), null, new Guid("550e8400-e29b-41d4-a716-446655440018"), new Guid("550e8400-e29b-41d4-a716-446655441002"), null },
                    { new Guid("880e8400-e29b-41d4-a716-556655470004"), null, new Guid("550e8400-e29b-41d4-a716-446655440008"), new Guid("550e8400-e29b-41d4-a716-446655441002"), null },
                    { new Guid("880e8400-e29b-41d4-a716-556655470005"), null, new Guid("550e8400-e29b-41d4-a716-446655440013"), new Guid("550e8400-e29b-41d4-a716-446655441002"), null },
                    { new Guid("880e8400-e29b-41d4-a716-556655470006"), null, new Guid("550e8400-e29b-41d4-a716-446655440034"), new Guid("550e8400-e29b-41d4-a716-446655441002"), null },
                    { new Guid("880e8400-e29b-41d4-a716-556655470007"), null, new Guid("550e8400-e29b-41d4-a716-446655440039"), new Guid("550e8400-e29b-41d4-a716-446655441002"), null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyUserProfiles_CompanyUserId",
                table: "CompanyUserProfiles",
                column: "CompanyUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyUserRoles_CompanyUserId_RoleId",
                table: "CompanyUserRoles",
                columns: new[] { "CompanyUserId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyUserRoles_RoleId",
                table: "CompanyUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyUsers_CompanyId",
                table: "CompanyUsers",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyUsers_Email",
                table: "CompanyUsers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyUserSessions_CompanyUserId",
                table: "CompanyUserSessions",
                column: "CompanyUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyUserProfiles");

            migrationBuilder.DropTable(
                name: "CompanyUserRoles");

            migrationBuilder.DropTable(
                name: "CompanyUserSessions");

            migrationBuilder.DropTable(
                name: "CompanyUsers");

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("880e8400-e29b-41d4-a716-556655470000"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("880e8400-e29b-41d4-a716-556655470001"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("880e8400-e29b-41d4-a716-556655470002"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("880e8400-e29b-41d4-a716-556655470003"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("880e8400-e29b-41d4-a716-556655470004"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("880e8400-e29b-41d4-a716-556655470005"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("880e8400-e29b-41d4-a716-556655470006"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("880e8400-e29b-41d4-a716-556655470007"));
        }
    }
}
