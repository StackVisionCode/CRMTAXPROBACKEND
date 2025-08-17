using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AuthService.Migrations
{
    /// <inheritdoc />
    public partial class NewMigrationsR : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Countries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenRequest = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpireTokenRequest = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TokenRefresh = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Device = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRevoke = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UserLimit = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    isRenewed = table.Column<bool>(type: "bit", nullable: false),
                    RenewedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RenewDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomPlans", x => x.Id);
                    table.CheckConstraint("CK_CustomPlans_Price", "[Price] >= 0");
                    table.CheckConstraint("CK_CustomPlans_RenewDate", "[RenewDate] IS NOT NULL");
                    table.CheckConstraint("CK_CustomPlans_UserLimit", "[UserLimit] >= 1");
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsGranted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PortalAccess = table.Column<int>(type: "int", nullable: false),
                    ServiceLevel = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Services",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Features = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UserLimit = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Services", x => x.Id);
                    table.CheckConstraint("CK_Services_Price", "[Price] >= 0");
                    table.CheckConstraint("CK_Services_UserLimit", "[UserLimit] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "States",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CountryId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_States", x => x.Id);
                    table.ForeignKey(
                        name: "FK_States_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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
                name: "RolePermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Modules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Modules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Modules_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Addresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CountryId = table.Column<int>(type: "int", nullable: false),
                    StateId = table.Column<int>(type: "int", nullable: false),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Street = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Line = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ZipCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Addresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Addresses_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Addresses_States_StateId",
                        column: x => x.StateId,
                        principalTable: "States",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomModules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsIncluded = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomModules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomModules_CustomPlans_CustomPlanId",
                        column: x => x.CustomPlanId,
                        principalTable: "CustomPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomModules_Modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "Modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsCompany = table.Column<bool>(type: "bit", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Brand = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AddressId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Domain = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CustomPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Companies_Addresses_AddressId",
                        column: x => x.AddressId,
                        principalTable: "Addresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Companies_CustomPlans_CustomPlanId",
                        column: x => x.CustomPlanId,
                        principalTable: "CustomPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaxUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsOwner = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhotoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Confirm = table.Column<bool>(type: "bit", nullable: true),
                    ConfirmToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResetPasswordToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResetPasswordExpires = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Factor2 = table.Column<bool>(type: "bit", nullable: true),
                    Otp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OtpVerified = table.Column<bool>(type: "bit", nullable: false),
                    OtpExpires = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AddressId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxUsers", x => x.Id);
                    table.CheckConstraint("CK_TaxUser_OneOwnerPerCompany", "([IsOwner] = 0) OR ([IsOwner] = 1)");
                    table.ForeignKey(
                        name: "FK_TaxUsers_Addresses_AddressId",
                        column: x => x.AddressId,
                        principalTable: "Addresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TaxUsers_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompanyPermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsGranted = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyPermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompanyPermissions_TaxUsers_TaxUserId",
                        column: x => x.TaxUserId,
                        principalTable: "TaxUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sessions_TaxUsers_TaxUserId",
                        column: x => x.TaxUserId,
                        principalTable: "TaxUsers",
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
                table: "Countries",
                columns: new[] { "Id", "DeleteAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, null, "Afganistán", null },
                    { 2, null, "Albania", null },
                    { 3, null, "Algeria", null },
                    { 4, null, "Samoa Americana", null },
                    { 5, null, "Andorra", null },
                    { 6, null, "Angola", null },
                    { 7, null, "Anguilla", null },
                    { 8, null, "Antártida", null },
                    { 9, null, "Antigua y Barbuda", null },
                    { 10, null, "Argentina", null },
                    { 11, null, "Armenia", null },
                    { 12, null, "Aruba", null },
                    { 13, null, "Australia", null },
                    { 14, null, "Austria", null },
                    { 15, null, "Azerbaiyán", null },
                    { 16, null, "Bahamas", null },
                    { 17, null, "Bahrein", null },
                    { 18, null, "Bangladesh", null },
                    { 19, null, "Barbados", null },
                    { 20, null, "Bielorrusia", null },
                    { 21, null, "Bélgica", null },
                    { 22, null, "Belice", null },
                    { 23, null, "Benín", null },
                    { 24, null, "Bermuda", null },
                    { 25, null, "Bután", null },
                    { 26, null, "Bolivia", null },
                    { 27, null, "Bosnia-Herzegovina", null },
                    { 28, null, "Botswana", null },
                    { 29, null, "Brasil", null },
                    { 30, null, "Brunei", null },
                    { 31, null, "Bulgaria", null },
                    { 32, null, "Burkina Faso", null },
                    { 33, null, "Burundi", null },
                    { 34, null, "Camboya", null },
                    { 35, null, "Camerún", null },
                    { 36, null, "Canadá", null },
                    { 37, null, "Cabo Verde", null },
                    { 38, null, "Islas Caimán", null },
                    { 39, null, "República Centroafricana", null },
                    { 40, null, "Chad", null },
                    { 41, null, "Chile", null },
                    { 42, null, "China", null },
                    { 43, null, "Isla de Navidad", null },
                    { 44, null, "Islas Cocos", null },
                    { 45, null, "Colombia", null },
                    { 46, null, "Comores", null },
                    { 47, null, "República del Congo", null },
                    { 48, null, "República Democrática del Congo", null },
                    { 49, null, "Islas Cook", null },
                    { 50, null, "Costa Rica", null },
                    { 51, null, "Costa de Marfíl", null },
                    { 52, null, "Croacia", null },
                    { 53, null, "Cuba", null },
                    { 54, null, "Chipre", null },
                    { 55, null, "República Checa", null },
                    { 56, null, "Dinamarca", null },
                    { 57, null, "Djibouti", null },
                    { 58, null, "Dominica", null },
                    { 59, null, "República Dominicana", null },
                    { 60, null, "Ecuador", null },
                    { 61, null, "Egipto", null },
                    { 62, null, "El Salvador", null },
                    { 63, null, "Guinea Ecuatorial", null },
                    { 64, null, "Eritrea", null },
                    { 65, null, "Estonia", null },
                    { 66, null, "Etiopía", null },
                    { 67, null, "Islas Malvinas", null },
                    { 68, null, "Islas Feroe", null },
                    { 69, null, "Fiji", null },
                    { 70, null, "Finlandia", null },
                    { 71, null, "Francia", null },
                    { 72, null, "Guyana Francesa", null },
                    { 73, null, "Polinesia Francesa", null },
                    { 74, null, "Tierras Australes y Antárticas Francesas", null },
                    { 75, null, "Gabón", null },
                    { 76, null, "Gambia", null },
                    { 77, null, "Georgia", null },
                    { 78, null, "Alemania", null },
                    { 79, null, "Ghana", null },
                    { 80, null, "Gibraltar", null },
                    { 81, null, "Grecia", null },
                    { 82, null, "Groenlandia", null },
                    { 83, null, "Granada", null },
                    { 84, null, "Guadalupe", null },
                    { 85, null, "Guam", null },
                    { 86, null, "Guatemala", null },
                    { 87, null, "Guinea", null },
                    { 88, null, "Guinea-Bissau", null },
                    { 89, null, "Guyana", null },
                    { 90, null, "Haití", null },
                    { 91, null, "Vaticano", null },
                    { 92, null, "Honduras", null },
                    { 93, null, "Hong Kong", null },
                    { 94, null, "Hungría", null },
                    { 95, null, "Islandia", null },
                    { 96, null, "India", null },
                    { 97, null, "Indonesia", null },
                    { 98, null, "Irán", null },
                    { 99, null, "Iraq", null },
                    { 100, null, "Irlanda", null },
                    { 101, null, "Israel", null },
                    { 102, null, "Italia", null },
                    { 103, null, "Jamaica", null },
                    { 104, null, "Japón", null },
                    { 105, null, "Jordania", null },
                    { 106, null, "Kazajstán", null },
                    { 107, null, "Kenia", null },
                    { 108, null, "Kiribati", null },
                    { 109, null, "Corea del Norte", null },
                    { 110, null, "Corea del Sur", null },
                    { 111, null, "Kuwait", null },
                    { 112, null, "Kirguistán", null },
                    { 113, null, "Laos", null },
                    { 114, null, "Letonia", null },
                    { 115, null, "Líbano", null },
                    { 116, null, "Lesotho", null },
                    { 117, null, "Liberia", null },
                    { 118, null, "Libia", null },
                    { 119, null, "Liechtenstein", null },
                    { 120, null, "Lituania", null },
                    { 121, null, "Luxemburgo", null },
                    { 122, null, "Macao", null },
                    { 123, null, "Macedonia", null },
                    { 124, null, "Madagascar", null },
                    { 125, null, "Malawi", null },
                    { 126, null, "Malasia", null },
                    { 127, null, "Maldivas", null },
                    { 128, null, "Mali", null },
                    { 129, null, "Malta", null },
                    { 130, null, "Islas Marshall", null },
                    { 131, null, "Martinica", null },
                    { 132, null, "Mauritania", null },
                    { 133, null, "Mauricio", null },
                    { 134, null, "Mayotte", null },
                    { 135, null, "México", null },
                    { 136, null, "Estados Federados de Micronesia", null },
                    { 137, null, "Moldavia", null },
                    { 138, null, "Mónaco", null },
                    { 139, null, "Mongolia", null },
                    { 140, null, "Montserrat", null },
                    { 141, null, "Marruecos", null },
                    { 142, null, "Mozambique", null },
                    { 143, null, "Myanmar", null },
                    { 144, null, "Namibia", null },
                    { 145, null, "Nauru", null },
                    { 146, null, "Nepal", null },
                    { 147, null, "Holanda", null },
                    { 148, null, "Antillas Holandesas", null },
                    { 149, null, "Nueva Caledonia", null },
                    { 150, null, "Nueva Zelanda", null },
                    { 151, null, "Nicaragua", null },
                    { 152, null, "Niger", null },
                    { 153, null, "Nigeria", null },
                    { 154, null, "Niue", null },
                    { 155, null, "Islas Norfolk", null },
                    { 156, null, "Islas Marianas del Norte", null },
                    { 157, null, "Noruega", null },
                    { 158, null, "Omán", null },
                    { 159, null, "Pakistán", null },
                    { 160, null, "Palau", null },
                    { 161, null, "Palestina", null },
                    { 162, null, "Panamá", null },
                    { 163, null, "Papua Nueva Guinea", null },
                    { 164, null, "Paraguay", null },
                    { 165, null, "Perú", null },
                    { 166, null, "Filipinas", null },
                    { 167, null, "Pitcairn", null },
                    { 168, null, "Polonia", null },
                    { 169, null, "Portugal", null },
                    { 170, null, "Puerto Rico", null },
                    { 171, null, "Qatar", null },
                    { 172, null, "Reunión", null },
                    { 173, null, "Rumanía", null },
                    { 174, null, "Rusia", null },
                    { 175, null, "Ruanda", null },
                    { 176, null, "Santa Helena", null },
                    { 177, null, "San Kitts y Nevis", null },
                    { 178, null, "Santa Lucía", null },
                    { 179, null, "San Vicente y Granadinas", null },
                    { 180, null, "Samoa", null },
                    { 181, null, "San Marino", null },
                    { 182, null, "Santo Tomé y Príncipe", null },
                    { 183, null, "Arabia Saudita", null },
                    { 184, null, "Senegal", null },
                    { 185, null, "Serbia", null },
                    { 186, null, "Seychelles", null },
                    { 187, null, "Sierra Leona", null },
                    { 188, null, "Singapur", null },
                    { 189, null, "Eslovaquía", null },
                    { 190, null, "Eslovenia", null },
                    { 191, null, "Islas Salomón", null },
                    { 192, null, "Somalia", null },
                    { 193, null, "Sudáfrica", null },
                    { 194, null, "España", null },
                    { 195, null, "Sri Lanka", null },
                    { 196, null, "Sudán", null },
                    { 197, null, "Surinam", null },
                    { 198, null, "Swazilandia", null },
                    { 199, null, "Suecia", null },
                    { 200, null, "Suiza", null },
                    { 201, null, "Siria", null },
                    { 202, null, "Taiwán", null },
                    { 203, null, "Tadjikistan", null },
                    { 204, null, "Tanzania", null },
                    { 205, null, "Tailandia", null },
                    { 206, null, "Timor Oriental", null },
                    { 207, null, "Togo", null },
                    { 208, null, "Tokelau", null },
                    { 209, null, "Tonga", null },
                    { 210, null, "Trinidad y Tobago", null },
                    { 211, null, "Túnez", null },
                    { 212, null, "Turquía", null },
                    { 213, null, "Turkmenistan", null },
                    { 214, null, "Islas Turcas y Caicos", null },
                    { 215, null, "Tuvalu", null },
                    { 216, null, "Uganda", null },
                    { 217, null, "Ucrania", null },
                    { 218, null, "Emiratos Árabes Unidos", null },
                    { 219, null, "Reino Unido", null },
                    { 220, null, "Estados Unidos", null },
                    { 221, null, "Uruguay", null },
                    { 222, null, "Uzbekistán", null },
                    { 223, null, "Vanuatu", null },
                    { 224, null, "Venezuela", null },
                    { 225, null, "Vietnam", null },
                    { 226, null, "Islas Vírgenes Británicas", null },
                    { 227, null, "Islas Vírgenes Americanas", null },
                    { 228, null, "Wallis y Futuna", null },
                    { 229, null, "Sáhara Occidental", null },
                    { 230, null, "Yemen", null },
                    { 231, null, "Zambia", null },
                    { 232, null, "Zimbabwe", null }
                });

            migrationBuilder.InsertData(
                table: "CustomPlans",
                columns: new[] { "Id", "CompanyId", "DeleteAt", "IsActive", "Price", "RenewDate", "RenewedDate", "StartDate", "UpdatedAt", "UserLimit", "isRenewed" },
                values: new object[] { new Guid("880e8400-e29b-41d4-a716-556655441001"), new Guid("770e8400-e29b-41d4-a716-556655441000"), null, true, 0.00m, new DateTime(2035, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 100, false });

            migrationBuilder.InsertData(
                table: "Modules",
                columns: new[] { "Id", "DeleteAt", "Description", "IsActive", "Name", "ServiceId", "UpdatedAt", "Url" },
                values: new object[,]
                {
                    { new Guid("770e8400-e29b-41d4-a716-556655440009"), null, "Manage multiple companies from one dashboard", true, "Multi-Company Management", null, null, "/multi-company" },
                    { new Guid("770e8400-e29b-41d4-a716-556655440010"), null, "Enhanced security features and compliance", true, "Advanced Security", null, null, "/security" }
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Code", "DeleteAt", "Description", "IsGranted", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("550e8400-e29b-41d4-a716-446655440001"), "Permission.Create", null, null, true, "Create Permissions", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440002"), "Permission.Read", null, null, true, "Read Permissions", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440003"), "Permission.View", null, null, true, "View Permissions", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440004"), "Permission.Delete", null, null, true, "Delete Permissions", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440005"), "Permission.Update", null, null, true, "Update Permissions", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440006"), "TaxUser.Create", null, null, true, "Create TaxUsers", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440007"), "TaxUser.Read", null, null, true, "Read TaxUsers", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440008"), "TaxUser.View", null, null, true, "View TaxUsers", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440009"), "TaxUser.Delete", null, null, true, "Delete TaxUsers", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440010"), "TaxUser.Update", null, null, true, "Update TaxUsers", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440011"), "Customer.Create", null, null, true, "Create Customers", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440012"), "Customer.Read", null, null, true, "Read Customers", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440013"), "Customer.View", null, null, true, "View Customers", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440014"), "Customer.Delete", null, null, true, "Delete Customers", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440015"), "Customer.Update", null, null, true, "Update Customers", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440016"), "Role.Create", null, null, true, "Create Roles", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440017"), "Role.Read", null, null, true, "Read Roles", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440018"), "Role.View", null, null, true, "View Roles", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440019"), "Role.Delete", null, null, true, "Delete Roles", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440020"), "Role.Update", null, null, true, "Update Roles", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440021"), "RolePermission.Create", null, null, true, "Create RolePermissions", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440022"), "RolePermission.Read", null, null, true, "Read RolePermissions", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440023"), "RolePermission.View", null, null, true, "View RolePermissions", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440024"), "RolePermission.Delete", null, null, true, "Delete RolePermissions", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440025"), "RolePermission.Update", null, null, true, "Update RolePermissions", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440026"), "Customer.SelfRead", null, null, true, "Read own profile", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440027"), "Customer.DisableLogin", null, null, true, "Disable customer login", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440028"), "Customer.EnableLogin", null, null, true, "Enable customer login", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440029"), "Sessions.Read", null, null, true, "Read sessions", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440030"), "Dependent.Create", null, null, true, "Create dependent", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440031"), "Dependent.Update", null, null, true, "Update dependent", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440032"), "Dependent.Delete", null, null, true, "Delete dependent", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440033"), "Dependent.Read", null, null, true, "Read dependent", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440034"), "Dependent.Viewer", null, null, true, "View dependent", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440035"), "TaxInformation.Create", null, null, true, "Create tax info", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440036"), "TaxInformation.Update", null, null, true, "Update tax info", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440037"), "TaxInformation.Delete", null, null, true, "Delete tax info", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440038"), "TaxInformation.Read", null, null, true, "Read tax info", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440039"), "TaxInformation.Viewer", null, null, true, "View tax info", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440040"), "Company.Create", null, null, true, "Create Companies", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440041"), "Company.Read", null, null, true, "Read Companies", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440042"), "Company.View", null, null, true, "View Companies", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440043"), "Company.Update", null, null, true, "Update Companies", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440044"), "Company.Delete", null, null, true, "Delete Companies", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440045"), "Service.Create", null, "Create new services in the system", true, "Create Services", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440046"), "Service.Read", null, "View and retrieve service information", true, "Read Services", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440047"), "Service.Update", null, "Modify existing services", true, "Update Services", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440048"), "Service.Delete", null, "Remove services from the system", true, "Delete Services", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440049"), "Service.ManageStatus", null, "Activate or deactivate services", true, "Manage Service Status", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440050"), "Module.Create", null, "Create new modules in the system", true, "Create Modules", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440051"), "Module.Read", null, "View and retrieve module information", true, "Read Modules", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440052"), "Module.Update", null, "Modify existing modules", true, "Update Modules", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440053"), "Module.Delete", null, "Remove modules from the system", true, "Delete Modules", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440054"), "Module.ManageStatus", null, "Activate or deactivate modules", true, "Manage Module Status", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440055"), "CustomPlan.Create", null, "Create new custom plans for companies", true, "Create CustomPlans", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440056"), "CustomPlan.Read", null, "View and retrieve custom plan information", true, "Read CustomPlans", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440057"), "CustomPlan.Update", null, "Modify existing custom plans", true, "Update CustomPlans", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440058"), "CustomPlan.Delete", null, "Remove custom plans from the system", true, "Delete CustomPlans", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440059"), "CustomPlan.ManageStatus", null, "Activate, deactivate or renew custom plans", true, "Manage CustomPlan Status", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440060"), "CustomModule.Create", null, "Assign modules to custom plans", true, "Create CustomModules", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440061"), "CustomModule.Read", null, "View and retrieve custom module information", true, "Read CustomModules", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440062"), "CustomModule.Update", null, "Modify existing custom modules", true, "Update CustomModules", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440063"), "CustomModule.Delete", null, "Remove custom modules from plans", true, "Delete CustomModules", null },
                    { new Guid("550e8400-e29b-41d4-a716-446655440064"), "CustomModule.ManageStatus", null, "Include or exclude modules from plans", true, "Manage CustomModule Status", null }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "DeleteAt", "Description", "Name", "PortalAccess", "ServiceLevel", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("550e8400-e29b-41d4-a716-446655441001"), null, "Has full access to all system features, settings, and user management. Responsible for maintaining and overseeing the platform.", "Developer", 3, null, null },
                    { new Guid("550e8400-e29b-41d4-a716-446655441002"), null, "Administrator with Basic service permissions and limitations.", "Administrator Basic", 1, 1, null },
                    { new Guid("550e8400-e29b-41d4-a716-446655441003"), null, "Administrator with Standard service permissions and features.", "Administrator Standard", 1, 2, null },
                    { new Guid("550e8400-e29b-41d4-a716-446655441004"), null, "Administrator with Pro service permissions and full features.", "Administrator Pro", 1, 3, null },
                    { new Guid("550e8400-e29b-41d4-a716-446655441005"), null, "User with limited access to specific functionalities of the company.", "User", 1, null, null },
                    { new Guid("550e8400-e29b-41d4-a716-446655441006"), null, "Has limited access to the system, can view and interact with allowed features based on their permissions.", "Customer", 2, null, null }
                });

            migrationBuilder.InsertData(
                table: "Services",
                columns: new[] { "Id", "DeleteAt", "Description", "Features", "IsActive", "Name", "Price", "Title", "UpdatedAt", "UserLimit" },
                values: new object[,]
                {
                    { new Guid("660e8400-e29b-41d4-a716-556655441001"), null, "Basic tax preparation service with essential features", "[\"Individual tax returns\",\"Basic invoicing\",\"Document storage\",\"Email support\"]", true, "Basic", 29.99m, "Basic Plan", null, 1 },
                    { new Guid("660e8400-e29b-41d4-a716-556655441002"), null, "Standard service with additional modules and more users", "[\"Individual \\u0026 business tax returns\",\"Advanced invoicing\",\"Document management\",\"Financial reports\",\"Customer portal\",\"Priority support\"]", true, "Standard", 59.99m, "Standard Plan", null, 4 },
                    { new Guid("660e8400-e29b-41d4-a716-556655441003"), null, "Professional service with all modules and unlimited features", "[\"All tax return types\",\"Complete invoicing suite\",\"Advanced document management\",\"Comprehensive reports\",\"Full customer portal\",\"Advanced analytics\",\"API integrations\",\"White label options\",\"24/7 premium support\"]", true, "Pro", 99.99m, "Professional Plan", null, 5 }
                });

            migrationBuilder.InsertData(
                table: "Modules",
                columns: new[] { "Id", "DeleteAt", "Description", "IsActive", "Name", "ServiceId", "UpdatedAt", "Url" },
                values: new object[,]
                {
                    { new Guid("770e8400-e29b-41d4-a716-556655440001"), null, "Individual and business tax return preparation", true, "Tax Returns", new Guid("660e8400-e29b-41d4-a716-556655441001"), null, "/tax-returns" },
                    { new Guid("770e8400-e29b-41d4-a716-556655440002"), null, "Create and manage invoices", true, "Invoicing", new Guid("660e8400-e29b-41d4-a716-556655441001"), null, "/invoicing" },
                    { new Guid("770e8400-e29b-41d4-a716-556655440003"), null, "Upload and organize tax documents", true, "Document Management", new Guid("660e8400-e29b-41d4-a716-556655441001"), null, "/documents" },
                    { new Guid("770e8400-e29b-41d4-a716-556655440004"), null, "Generate financial and tax reports", true, "Reports", new Guid("660e8400-e29b-41d4-a716-556655441002"), null, "/reports" },
                    { new Guid("770e8400-e29b-41d4-a716-556655440005"), null, "Dedicated portal for client communication", true, "Customer Portal", new Guid("660e8400-e29b-41d4-a716-556655441002"), null, "/customer-portal" },
                    { new Guid("770e8400-e29b-41d4-a716-556655440006"), null, "Business insights and analytics", true, "Advanced Analytics", new Guid("660e8400-e29b-41d4-a716-556655441003"), null, "/analytics" },
                    { new Guid("770e8400-e29b-41d4-a716-556655440007"), null, "Connect with third-party services", true, "API Integration", new Guid("660e8400-e29b-41d4-a716-556655441003"), null, "/api-integration" },
                    { new Guid("770e8400-e29b-41d4-a716-556655440008"), null, "Custom branding options", true, "White Label", new Guid("660e8400-e29b-41d4-a716-556655441003"), null, "/white-label" }
                });

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
                    { new Guid("660e8400-e29b-41d4-a716-446655450026"), null, new Guid("550e8400-e29b-41d4-a716-446655440026"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
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
                    { new Guid("660e8400-e29b-41d4-a716-446655450040"), null, new Guid("550e8400-e29b-41d4-a716-446655440040"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450041"), null, new Guid("550e8400-e29b-41d4-a716-446655440041"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450042"), null, new Guid("550e8400-e29b-41d4-a716-446655440042"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450043"), null, new Guid("550e8400-e29b-41d4-a716-446655440043"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450044"), null, new Guid("550e8400-e29b-41d4-a716-446655440044"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450045"), null, new Guid("550e8400-e29b-41d4-a716-446655440045"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450046"), null, new Guid("550e8400-e29b-41d4-a716-446655440046"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450047"), null, new Guid("550e8400-e29b-41d4-a716-446655440047"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450048"), null, new Guid("550e8400-e29b-41d4-a716-446655440048"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450049"), null, new Guid("550e8400-e29b-41d4-a716-446655440049"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450050"), null, new Guid("550e8400-e29b-41d4-a716-446655440050"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450051"), null, new Guid("550e8400-e29b-41d4-a716-446655440051"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450052"), null, new Guid("550e8400-e29b-41d4-a716-446655440052"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450053"), null, new Guid("550e8400-e29b-41d4-a716-446655440053"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450054"), null, new Guid("550e8400-e29b-41d4-a716-446655440054"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450055"), null, new Guid("550e8400-e29b-41d4-a716-446655440055"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450056"), null, new Guid("550e8400-e29b-41d4-a716-446655440056"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450057"), null, new Guid("550e8400-e29b-41d4-a716-446655440057"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450058"), null, new Guid("550e8400-e29b-41d4-a716-446655440058"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450059"), null, new Guid("550e8400-e29b-41d4-a716-446655440059"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450060"), null, new Guid("550e8400-e29b-41d4-a716-446655440060"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450061"), null, new Guid("550e8400-e29b-41d4-a716-446655440061"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450062"), null, new Guid("550e8400-e29b-41d4-a716-446655440062"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450063"), null, new Guid("550e8400-e29b-41d4-a716-446655440063"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("660e8400-e29b-41d4-a716-446655450064"), null, new Guid("550e8400-e29b-41d4-a716-446655440064"), new Guid("550e8400-e29b-41d4-a716-446655441001"), null },
                    { new Guid("770e8400-e29b-41d4-a716-556655450026"), null, new Guid("550e8400-e29b-41d4-a716-446655440026"), new Guid("550e8400-e29b-41d4-a716-446655441006"), null },
                    { new Guid("770e8400-e29b-41d4-a716-556655460000"), null, new Guid("550e8400-e29b-41d4-a716-446655440011"), new Guid("550e8400-e29b-41d4-a716-446655441002"), null },
                    { new Guid("770e8400-e29b-41d4-a716-556655460001"), null, new Guid("550e8400-e29b-41d4-a716-446655440012"), new Guid("550e8400-e29b-41d4-a716-446655441002"), null },
                    { new Guid("770e8400-e29b-41d4-a716-556655460002"), null, new Guid("550e8400-e29b-41d4-a716-446655440013"), new Guid("550e8400-e29b-41d4-a716-446655441002"), null },
                    { new Guid("770e8400-e29b-41d4-a716-556655460003"), null, new Guid("550e8400-e29b-41d4-a716-446655440015"), new Guid("550e8400-e29b-41d4-a716-446655441002"), null },
                    { new Guid("770e8400-e29b-41d4-a716-556655460004"), null, new Guid("550e8400-e29b-41d4-a716-446655440030"), new Guid("550e8400-e29b-41d4-a716-446655441002"), null },
                    { new Guid("770e8400-e29b-41d4-a716-556655460005"), null, new Guid("550e8400-e29b-41d4-a716-446655440033"), new Guid("550e8400-e29b-41d4-a716-446655441002"), null },
                    { new Guid("770e8400-e29b-41d4-a716-556655460006"), null, new Guid("550e8400-e29b-41d4-a716-446655440034"), new Guid("550e8400-e29b-41d4-a716-446655441002"), null },
                    { new Guid("770e8400-e29b-41d4-a716-556655460007"), null, new Guid("550e8400-e29b-41d4-a716-446655440035"), new Guid("550e8400-e29b-41d4-a716-446655441002"), null },
                    { new Guid("770e8400-e29b-41d4-a716-556655460008"), null, new Guid("550e8400-e29b-41d4-a716-446655440038"), new Guid("550e8400-e29b-41d4-a716-446655441002"), null },
                    { new Guid("770e8400-e29b-41d4-a716-556655460009"), null, new Guid("550e8400-e29b-41d4-a716-446655440039"), new Guid("550e8400-e29b-41d4-a716-446655441002"), null },
                    { new Guid("770e8400-e29b-41d4-a716-556655460010"), null, new Guid("550e8400-e29b-41d4-a716-446655440041"), new Guid("550e8400-e29b-41d4-a716-446655441002"), null },
                    { new Guid("770e8400-e29b-41d4-a716-556655460011"), null, new Guid("550e8400-e29b-41d4-a716-446655440042"), new Guid("550e8400-e29b-41d4-a716-446655441002"), null },
                    { new Guid("770e8400-e29b-41d4-a716-556655460012"), null, new Guid("550e8400-e29b-41d4-a716-446655440029"), new Guid("550e8400-e29b-41d4-a716-446655441002"), null },
                    { new Guid("780e8400-e29b-41d4-a716-556655460000"), null, new Guid("550e8400-e29b-41d4-a716-446655440011"), new Guid("550e8400-e29b-41d4-a716-446655441003"), null },
                    { new Guid("780e8400-e29b-41d4-a716-556655460001"), null, new Guid("550e8400-e29b-41d4-a716-446655440012"), new Guid("550e8400-e29b-41d4-a716-446655441003"), null },
                    { new Guid("780e8400-e29b-41d4-a716-556655460002"), null, new Guid("550e8400-e29b-41d4-a716-446655440013"), new Guid("550e8400-e29b-41d4-a716-446655441003"), null },
                    { new Guid("780e8400-e29b-41d4-a716-556655460003"), null, new Guid("550e8400-e29b-41d4-a716-446655440015"), new Guid("550e8400-e29b-41d4-a716-446655441003"), null },
                    { new Guid("780e8400-e29b-41d4-a716-556655460004"), null, new Guid("550e8400-e29b-41d4-a716-446655440030"), new Guid("550e8400-e29b-41d4-a716-446655441003"), null },
                    { new Guid("780e8400-e29b-41d4-a716-556655460005"), null, new Guid("550e8400-e29b-41d4-a716-446655440033"), new Guid("550e8400-e29b-41d4-a716-446655441003"), null },
                    { new Guid("780e8400-e29b-41d4-a716-556655460006"), null, new Guid("550e8400-e29b-41d4-a716-446655440034"), new Guid("550e8400-e29b-41d4-a716-446655441003"), null },
                    { new Guid("780e8400-e29b-41d4-a716-556655460007"), null, new Guid("550e8400-e29b-41d4-a716-446655440035"), new Guid("550e8400-e29b-41d4-a716-446655441003"), null },
                    { new Guid("780e8400-e29b-41d4-a716-556655460008"), null, new Guid("550e8400-e29b-41d4-a716-446655440038"), new Guid("550e8400-e29b-41d4-a716-446655441003"), null },
                    { new Guid("780e8400-e29b-41d4-a716-556655460009"), null, new Guid("550e8400-e29b-41d4-a716-446655440039"), new Guid("550e8400-e29b-41d4-a716-446655441003"), null },
                    { new Guid("780e8400-e29b-41d4-a716-556655460010"), null, new Guid("550e8400-e29b-41d4-a716-446655440041"), new Guid("550e8400-e29b-41d4-a716-446655441003"), null },
                    { new Guid("780e8400-e29b-41d4-a716-556655460011"), null, new Guid("550e8400-e29b-41d4-a716-446655440042"), new Guid("550e8400-e29b-41d4-a716-446655441003"), null },
                    { new Guid("780e8400-e29b-41d4-a716-556655460012"), null, new Guid("550e8400-e29b-41d4-a716-446655440029"), new Guid("550e8400-e29b-41d4-a716-446655441003"), null },
                    { new Guid("780e8400-e29b-41d4-a716-556655460013"), null, new Guid("550e8400-e29b-41d4-a716-446655440027"), new Guid("550e8400-e29b-41d4-a716-446655441003"), null },
                    { new Guid("780e8400-e29b-41d4-a716-556655460014"), null, new Guid("550e8400-e29b-41d4-a716-446655440028"), new Guid("550e8400-e29b-41d4-a716-446655441003"), null },
                    { new Guid("780e8400-e29b-41d4-a716-556655460015"), null, new Guid("550e8400-e29b-41d4-a716-446655440031"), new Guid("550e8400-e29b-41d4-a716-446655441003"), null },
                    { new Guid("780e8400-e29b-41d4-a716-556655460016"), null, new Guid("550e8400-e29b-41d4-a716-446655440036"), new Guid("550e8400-e29b-41d4-a716-446655441003"), null },
                    { new Guid("780e8400-e29b-41d4-a716-556655460017"), null, new Guid("550e8400-e29b-41d4-a716-446655440018"), new Guid("550e8400-e29b-41d4-a716-446655441003"), null },
                    { new Guid("780e8400-e29b-41d4-a716-556655460018"), null, new Guid("550e8400-e29b-41d4-a716-446655440003"), new Guid("550e8400-e29b-41d4-a716-446655441003"), null },
                    { new Guid("780e8400-e29b-41d4-a716-556655460019"), null, new Guid("550e8400-e29b-41d4-a716-446655440007"), new Guid("550e8400-e29b-41d4-a716-446655441003"), null },
                    { new Guid("780e8400-e29b-41d4-a716-556655460020"), null, new Guid("550e8400-e29b-41d4-a716-446655440008"), new Guid("550e8400-e29b-41d4-a716-446655441003"), null },
                    { new Guid("790e8400-e29b-41d4-a716-556655460000"), null, new Guid("550e8400-e29b-41d4-a716-446655440011"), new Guid("550e8400-e29b-41d4-a716-446655441004"), null },
                    { new Guid("790e8400-e29b-41d4-a716-556655460001"), null, new Guid("550e8400-e29b-41d4-a716-446655440012"), new Guid("550e8400-e29b-41d4-a716-446655441004"), null },
                    { new Guid("790e8400-e29b-41d4-a716-556655460002"), null, new Guid("550e8400-e29b-41d4-a716-446655440013"), new Guid("550e8400-e29b-41d4-a716-446655441004"), null },
                    { new Guid("790e8400-e29b-41d4-a716-556655460003"), null, new Guid("550e8400-e29b-41d4-a716-446655440015"), new Guid("550e8400-e29b-41d4-a716-446655441004"), null },
                    { new Guid("790e8400-e29b-41d4-a716-556655460004"), null, new Guid("550e8400-e29b-41d4-a716-446655440030"), new Guid("550e8400-e29b-41d4-a716-446655441004"), null },
                    { new Guid("790e8400-e29b-41d4-a716-556655460005"), null, new Guid("550e8400-e29b-41d4-a716-446655440033"), new Guid("550e8400-e29b-41d4-a716-446655441004"), null },
                    { new Guid("790e8400-e29b-41d4-a716-556655460006"), null, new Guid("550e8400-e29b-41d4-a716-446655440034"), new Guid("550e8400-e29b-41d4-a716-446655441004"), null },
                    { new Guid("790e8400-e29b-41d4-a716-556655460007"), null, new Guid("550e8400-e29b-41d4-a716-446655440035"), new Guid("550e8400-e29b-41d4-a716-446655441004"), null },
                    { new Guid("790e8400-e29b-41d4-a716-556655460008"), null, new Guid("550e8400-e29b-41d4-a716-446655440038"), new Guid("550e8400-e29b-41d4-a716-446655441004"), null },
                    { new Guid("790e8400-e29b-41d4-a716-556655460009"), null, new Guid("550e8400-e29b-41d4-a716-446655440039"), new Guid("550e8400-e29b-41d4-a716-446655441004"), null },
                    { new Guid("790e8400-e29b-41d4-a716-556655460010"), null, new Guid("550e8400-e29b-41d4-a716-446655440041"), new Guid("550e8400-e29b-41d4-a716-446655441004"), null },
                    { new Guid("790e8400-e29b-41d4-a716-556655460011"), null, new Guid("550e8400-e29b-41d4-a716-446655440042"), new Guid("550e8400-e29b-41d4-a716-446655441004"), null },
                    { new Guid("790e8400-e29b-41d4-a716-556655460012"), null, new Guid("550e8400-e29b-41d4-a716-446655440029"), new Guid("550e8400-e29b-41d4-a716-446655441004"), null },
                    { new Guid("790e8400-e29b-41d4-a716-556655460013"), null, new Guid("550e8400-e29b-41d4-a716-446655440027"), new Guid("550e8400-e29b-41d4-a716-446655441004"), null },
                    { new Guid("790e8400-e29b-41d4-a716-556655460014"), null, new Guid("550e8400-e29b-41d4-a716-446655440028"), new Guid("550e8400-e29b-41d4-a716-446655441004"), null },
                    { new Guid("790e8400-e29b-41d4-a716-556655460015"), null, new Guid("550e8400-e29b-41d4-a716-446655440031"), new Guid("550e8400-e29b-41d4-a716-446655441004"), null },
                    { new Guid("790e8400-e29b-41d4-a716-556655460016"), null, new Guid("550e8400-e29b-41d4-a716-446655440036"), new Guid("550e8400-e29b-41d4-a716-446655441004"), null },
                    { new Guid("790e8400-e29b-41d4-a716-556655460017"), null, new Guid("550e8400-e29b-41d4-a716-446655440018"), new Guid("550e8400-e29b-41d4-a716-446655441004"), null },
                    { new Guid("790e8400-e29b-41d4-a716-556655460018"), null, new Guid("550e8400-e29b-41d4-a716-446655440003"), new Guid("550e8400-e29b-41d4-a716-446655441004"), null },
                    { new Guid("790e8400-e29b-41d4-a716-556655460019"), null, new Guid("550e8400-e29b-41d4-a716-446655440007"), new Guid("550e8400-e29b-41d4-a716-446655441004"), null },
                    { new Guid("790e8400-e29b-41d4-a716-556655460020"), null, new Guid("550e8400-e29b-41d4-a716-446655440008"), new Guid("550e8400-e29b-41d4-a716-446655441004"), null },
                    { new Guid("790e8400-e29b-41d4-a716-556655460021"), null, new Guid("550e8400-e29b-41d4-a716-446655440006"), new Guid("550e8400-e29b-41d4-a716-446655441004"), null },
                    { new Guid("790e8400-e29b-41d4-a716-556655460022"), null, new Guid("550e8400-e29b-41d4-a716-446655440010"), new Guid("550e8400-e29b-41d4-a716-446655441004"), null },
                    { new Guid("790e8400-e29b-41d4-a716-556655460023"), null, new Guid("550e8400-e29b-41d4-a716-446655440009"), new Guid("550e8400-e29b-41d4-a716-446655441004"), null },
                    { new Guid("790e8400-e29b-41d4-a716-556655460024"), null, new Guid("550e8400-e29b-41d4-a716-446655440032"), new Guid("550e8400-e29b-41d4-a716-446655441004"), null },
                    { new Guid("790e8400-e29b-41d4-a716-556655460025"), null, new Guid("550e8400-e29b-41d4-a716-446655440037"), new Guid("550e8400-e29b-41d4-a716-446655441004"), null },
                    { new Guid("790e8400-e29b-41d4-a716-556655460026"), null, new Guid("550e8400-e29b-41d4-a716-446655440043"), new Guid("550e8400-e29b-41d4-a716-446655441004"), null },
                    { new Guid("790e8400-e29b-41d4-a716-556655460027"), null, new Guid("550e8400-e29b-41d4-a716-446655440046"), new Guid("550e8400-e29b-41d4-a716-446655441004"), null },
                    { new Guid("790e8400-e29b-41d4-a716-556655460028"), null, new Guid("550e8400-e29b-41d4-a716-446655440051"), new Guid("550e8400-e29b-41d4-a716-446655441004"), null },
                    { new Guid("790e8400-e29b-41d4-a716-556655460029"), null, new Guid("550e8400-e29b-41d4-a716-446655440056"), new Guid("550e8400-e29b-41d4-a716-446655441004"), null },
                    { new Guid("790e8400-e29b-41d4-a716-556655460030"), null, new Guid("550e8400-e29b-41d4-a716-446655440061"), new Guid("550e8400-e29b-41d4-a716-446655441004"), null },
                    { new Guid("880e8400-e29b-41d4-a716-556655470000"), null, new Guid("550e8400-e29b-41d4-a716-446655440029"), new Guid("550e8400-e29b-41d4-a716-446655441005"), null },
                    { new Guid("880e8400-e29b-41d4-a716-556655470001"), null, new Guid("550e8400-e29b-41d4-a716-446655440026"), new Guid("550e8400-e29b-41d4-a716-446655441005"), null }
                });

            migrationBuilder.InsertData(
                table: "States",
                columns: new[] { "Id", "CountryId", "DeleteAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, 220, null, "Alabama", null },
                    { 2, 220, null, "Alaska", null },
                    { 3, 220, null, "Arizona", null },
                    { 4, 220, null, "Arkansas", null },
                    { 5, 220, null, "California", null },
                    { 6, 220, null, "Colorado", null },
                    { 7, 220, null, "Connecticut", null },
                    { 8, 220, null, "Delaware", null },
                    { 9, 220, null, "Florida", null },
                    { 10, 220, null, "Georgia", null },
                    { 11, 220, null, "Hawaii", null },
                    { 12, 220, null, "Idaho", null },
                    { 13, 220, null, "Illinois", null },
                    { 14, 220, null, "Indiana", null },
                    { 15, 220, null, "Iowa", null },
                    { 16, 220, null, "Kansas", null },
                    { 17, 220, null, "Kentucky", null },
                    { 18, 220, null, "Louisiana", null },
                    { 19, 220, null, "Maine", null },
                    { 20, 220, null, "Maryland", null },
                    { 21, 220, null, "Massachusetts", null },
                    { 22, 220, null, "Michigan", null },
                    { 23, 220, null, "Minnesota", null },
                    { 24, 220, null, "Mississippi", null },
                    { 25, 220, null, "Missouri", null },
                    { 26, 220, null, "Montana", null },
                    { 27, 220, null, "Nebraska", null },
                    { 28, 220, null, "Nevada", null },
                    { 29, 220, null, "New Hampshire", null },
                    { 30, 220, null, "New Jersey", null },
                    { 31, 220, null, "New Mexico", null },
                    { 32, 220, null, "New York", null },
                    { 33, 220, null, "North Carolina", null },
                    { 34, 220, null, "North Dakota", null },
                    { 35, 220, null, "Ohio", null },
                    { 36, 220, null, "Oklahoma", null },
                    { 37, 220, null, "Oregon", null },
                    { 38, 220, null, "Pennsylvania", null },
                    { 39, 220, null, "Rhode Island", null },
                    { 40, 220, null, "South Carolina", null },
                    { 41, 220, null, "South Dakota", null },
                    { 42, 220, null, "Tennessee", null },
                    { 43, 220, null, "Texas", null },
                    { 44, 220, null, "Utah", null },
                    { 45, 220, null, "Vermont", null },
                    { 46, 220, null, "Virginia", null },
                    { 47, 220, null, "Washington", null },
                    { 48, 220, null, "West Virginia", null },
                    { 49, 220, null, "Wisconsin", null },
                    { 50, 220, null, "Wyoming", null },
                    { 51, 220, null, "District of Columbia", null }
                });

            migrationBuilder.InsertData(
                table: "Addresses",
                columns: new[] { "Id", "City", "CountryId", "DeleteAt", "Line", "StateId", "Street", "UpdatedAt", "ZipCode" },
                values: new object[,]
                {
                    { new Guid("990e8400-e29b-41d4-a716-556655443000"), "Miami", 220, null, "Suite 5", 9, "NW 2nd Ave 200", null, "33101" },
                    { new Guid("990e8400-e29b-41d4-a716-556655443001"), "Miami", 220, null, null, 9, "NW 1st Ave 100", null, "33101" }
                });

            migrationBuilder.InsertData(
                table: "Companies",
                columns: new[] { "Id", "AddressId", "Brand", "CompanyName", "CustomPlanId", "DeleteAt", "Description", "Domain", "FullName", "IsCompany", "Phone", "UpdatedAt" },
                values: new object[] { new Guid("770e8400-e29b-41d4-a716-556655441000"), new Guid("990e8400-e29b-41d4-a716-556655443001"), "https://images5.example.com/", "StackVision Software S.R.L.", new Guid("880e8400-e29b-41d4-a716-556655441001"), null, "Software Developers Assembly.", "stackvision", null, true, "8298981594", null });

            migrationBuilder.InsertData(
                table: "TaxUsers",
                columns: new[] { "Id", "AddressId", "CompanyId", "Confirm", "ConfirmToken", "DeleteAt", "Email", "Factor2", "IsActive", "IsOwner", "LastName", "Name", "Otp", "OtpExpires", "OtpVerified", "Password", "PhoneNumber", "PhotoUrl", "ResetPasswordExpires", "ResetPasswordToken", "UpdatedAt" },
                values: new object[] { new Guid("880e8400-e29b-41d4-a716-556655441000"), new Guid("990e8400-e29b-41d4-a716-556655443000"), new Guid("770e8400-e29b-41d4-a716-556655441000"), true, null, null, "stackvisionsoftware@gmail.com", null, true, true, "StackVision", "Developer", null, null, false, "zBLVJHyDUQKSp3ZYdgIeOEDnoeD61Zg566QoP2165AQAPHxzvJlAWjt1dV+Qinc7", "8298981594", null, null, null, null });

            migrationBuilder.InsertData(
                table: "UserRoles",
                columns: new[] { "Id", "DeleteAt", "RoleId", "TaxUserId", "UpdatedAt" },
                values: new object[] { new Guid("880e8400-e29b-41d4-a716-556655442000"), null, new Guid("550e8400-e29b-41d4-a716-446655441001"), new Guid("880e8400-e29b-41d4-a716-556655441000"), null });

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_CountryId",
                table: "Addresses",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_StateId",
                table: "Addresses",
                column: "StateId");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_AddressId",
                table: "Companies",
                column: "AddressId");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_CustomPlanId",
                table: "Companies",
                column: "CustomPlanId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Domain",
                table: "Companies",
                column: "Domain",
                unique: true,
                filter: "[Domain] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyPermissions_IsGranted",
                table: "CompanyPermissions",
                column: "IsGranted");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyPermissions_PermissionId",
                table: "CompanyPermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyPermissions_TaxUserId",
                table: "CompanyPermissions",
                column: "TaxUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyPermissions_TaxUserId_PermissionId",
                table: "CompanyPermissions",
                columns: new[] { "TaxUserId", "PermissionId" },
                unique: true);

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
                name: "IX_CustomModules_CustomPlanId",
                table: "CustomModules",
                column: "CustomPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomModules_CustomPlanId_ModuleId",
                table: "CustomModules",
                columns: new[] { "CustomPlanId", "ModuleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomModules_ModuleId",
                table: "CustomModules",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomPlans_CompanyId",
                table: "CustomPlans",
                column: "CompanyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomPlans_IsActive",
                table: "CustomPlans",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Modules_Name",
                table: "Modules",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Modules_ServiceId",
                table: "Modules",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Code",
                table: "Permissions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId_PermissionId",
                table: "RolePermissions",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Services_Name",
                table: "Services",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_TaxUserId",
                table: "Sessions",
                column: "TaxUserId");

            migrationBuilder.CreateIndex(
                name: "IX_States_CountryId",
                table: "States",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxUsers_AddressId",
                table: "TaxUsers",
                column: "AddressId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxUsers_CompanyId",
                table: "TaxUsers",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxUsers_Email",
                table: "TaxUsers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxUsers_IsOwner",
                table: "TaxUsers",
                column: "IsOwner");

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
                name: "CompanyPermissions");

            migrationBuilder.DropTable(
                name: "CustomerRoles");

            migrationBuilder.DropTable(
                name: "CustomerSessions");

            migrationBuilder.DropTable(
                name: "CustomModules");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "Modules");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "TaxUsers");

            migrationBuilder.DropTable(
                name: "Services");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropTable(
                name: "Addresses");

            migrationBuilder.DropTable(
                name: "CustomPlans");

            migrationBuilder.DropTable(
                name: "States");

            migrationBuilder.DropTable(
                name: "Countries");
        }
    }
}
