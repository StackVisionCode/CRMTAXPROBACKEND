using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SubscriptionsService.Migrations
{
    /// <inheritdoc />
    public partial class NewMigrationsR : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    RenewDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRenewed = table.Column<bool>(type: "bit", nullable: false),
                    RenewedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    ServiceLevel = table.Column<int>(type: "int", nullable: false),
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

            migrationBuilder.InsertData(
                table: "Modules",
                columns: new[] { "Id", "DeleteAt", "Description", "IsActive", "Name", "ServiceId", "UpdatedAt", "Url" },
                values: new object[,]
                {
                    { new Guid("770e8400-e29b-41d4-a716-556655440009"), null, "Manage multiple companies from one dashboard", true, "Multi-Company Management", null, null, "/multi-company" },
                    { new Guid("770e8400-e29b-41d4-a716-556655440010"), null, "Enhanced security features and compliance", true, "Advanced Security", null, null, "/security" }
                });

            migrationBuilder.InsertData(
                table: "Services",
                columns: new[] { "Id", "DeleteAt", "Description", "Features", "IsActive", "Name", "Price", "ServiceLevel", "Title", "UpdatedAt", "UserLimit" },
                values: new object[,]
                {
                    { new Guid("660e8400-e29b-41d4-a716-556655441001"), null, "Basic tax preparation service with essential features", "[\"Individual tax returns\",\"Basic invoicing\",\"Document storage\",\"Email support\"]", true, "Basic", 29.99m, 1, "Basic Plan", null, 1 },
                    { new Guid("660e8400-e29b-41d4-a716-556655441002"), null, "Standard service with additional modules and more users", "[\"Individual \\u0026 business tax returns\",\"Advanced invoicing\",\"Document management\",\"Financial reports\",\"Customer portal\",\"Priority support\"]", true, "Standard", 59.99m, 2, "Standard Plan", null, 4 },
                    { new Guid("660e8400-e29b-41d4-a716-556655441003"), null, "Professional service with all modules and unlimited features", "[\"All tax return types\",\"Complete invoicing suite\",\"Advanced document management\",\"Comprehensive reports\",\"Full customer portal\",\"Advanced analytics\",\"API integrations\",\"White label options\",\"24/7 premium support\"]", true, "Pro", 99.99m, 3, "Professional Plan", null, 5 },
                    { new Guid("660e8400-e29b-41d4-a716-556655441004"), null, "Unlimited access for system developers and administrators", "[\"Full System Access\",\"Unlimited Users\",\"All Modules\",\"Developer Tools\",\"System Administration\"]", true, "Developer", 0m, 0, "Developer Access", null, 2147483647 }
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
                name: "IX_Services_Name",
                table: "Services",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomModules");

            migrationBuilder.DropTable(
                name: "CustomPlans");

            migrationBuilder.DropTable(
                name: "Modules");

            migrationBuilder.DropTable(
                name: "Services");
        }
    }
}
