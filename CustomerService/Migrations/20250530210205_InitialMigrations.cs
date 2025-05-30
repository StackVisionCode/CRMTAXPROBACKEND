using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CustomerService.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FilingStatuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FilingStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaritalStatuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaritalStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Occupations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Occupations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PreferredContacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreferredContacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Relationships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Relationships", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccupationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MiddleName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SsnOrItin = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaritalStatusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Customers_MaritalStatuses_MaritalStatusId",
                        column: x => x.MaritalStatusId,
                        principalTable: "MaritalStatuses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Customers_Occupations_OccupationId",
                        column: x => x.OccupationId,
                        principalTable: "Occupations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Addresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StreetAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApartmentNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ZipCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Addresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Addresses_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ContactInfos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PreferredContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsLoggin = table.Column<bool>(type: "bit", nullable: false),
                    PasswordClient = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactInfos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactInfos_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ContactInfos_PreferredContacts_PreferredContactId",
                        column: x => x.PreferredContactId,
                        principalTable: "PreferredContacts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Dependents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RelationshipId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dependents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dependents_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Dependents_Relationships_RelationshipId",
                        column: x => x.RelationshipId,
                        principalTable: "Relationships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaxInformations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FilingStatusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastYearAGI = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BankAccountNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankRoutingNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsReturningCustomer = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxInformations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxInformations_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TaxInformations_FilingStatuses_FilingStatusId",
                        column: x => x.FilingStatusId,
                        principalTable: "FilingStatuses",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "FilingStatuses",
                columns: new[] { "Id", "CreatedAt", "DeleteAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000001"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Single", null },
                    { new Guid("20000000-0000-0000-0000-000000000002"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "MarriedJoint", null },
                    { new Guid("20000000-0000-0000-0000-000000000003"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "MarriedSeparate", null },
                    { new Guid("20000000-0000-0000-0000-000000000004"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "HeadOfHousehold", null }
                });

            migrationBuilder.InsertData(
                table: "MaritalStatuses",
                columns: new[] { "Id", "CreatedAt", "DeleteAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("30000000-0000-0000-0000-000000000001"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Single", null },
                    { new Guid("30000000-0000-0000-0000-000000000002"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Married", null },
                    { new Guid("30000000-0000-0000-0000-000000000003"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Divorced", null },
                    { new Guid("30000000-0000-0000-0000-000000000004"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Widowed", null }
                });

            migrationBuilder.InsertData(
                table: "Occupations",
                columns: new[] { "Id", "CreatedAt", "DeleteAt", "Description", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000001"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Designs and develops software applications.", "Software Developer", null },
                    { new Guid("00000000-0000-0000-0000-000000000002"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Prepares and examines financial records.", "Accountant", null },
                    { new Guid("00000000-0000-0000-0000-000000000003"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Instructs students at various educational levels.", "Teacher", null },
                    { new Guid("00000000-0000-0000-0000-000000000004"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Provides medical care and support to patients.", "Nurse", null },
                    { new Guid("00000000-0000-0000-0000-000000000005"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Diagnoses and treats illnesses and injuries.", "Doctor", null },
                    { new Guid("00000000-0000-0000-0000-000000000006"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Installs and repairs electrical systems.", "Electrician", null },
                    { new Guid("00000000-0000-0000-0000-000000000007"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Maintains and repairs water systems.", "Plumber", null },
                    { new Guid("00000000-0000-0000-0000-000000000008"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Builds and repairs buildings and infrastructure.", "Construction Worker", null },
                    { new Guid("00000000-0000-0000-0000-000000000009"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Enforces laws and protects citizens.", "Police Officer", null },
                    { new Guid("00000000-0000-0000-0000-000000000010"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Responds to fire and rescue emergencies.", "Firefighter", null },
                    { new Guid("00000000-0000-0000-0000-000000000011"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Transports goods over long distances.", "Truck Driver", null },
                    { new Guid("00000000-0000-0000-0000-000000000012"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Prepares meals and manages kitchen staff.", "Chef", null },
                    { new Guid("00000000-0000-0000-0000-000000000013"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Handles customer transactions at a store.", "Cashier", null },
                    { new Guid("00000000-0000-0000-0000-000000000014"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Sells products or services to customers.", "Salesperson", null },
                    { new Guid("00000000-0000-0000-0000-000000000015"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Monitors and protects property and people.", "Security Guard", null },
                    { new Guid("00000000-0000-0000-0000-000000000016"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Cuts, colors, and styles hair.", "Hairdresser", null },
                    { new Guid("00000000-0000-0000-0000-000000000017"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Repairs and maintains vehicles and machinery.", "Mechanic", null },
                    { new Guid("00000000-0000-0000-0000-000000000018"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Cleans and maintains buildings.", "Janitor", null },
                    { new Guid("00000000-0000-0000-0000-000000000019"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Manages front desk and greets visitors.", "Receptionist", null },
                    { new Guid("00000000-0000-0000-0000-000000000020"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Handles administrative and clerical tasks.", "Secretary", null },
                    { new Guid("00000000-0000-0000-0000-000000000021"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Designs and oversees projects in various fields.", "Engineer", null },
                    { new Guid("00000000-0000-0000-0000-000000000022"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Builds and maintains websites and web apps.", "Web Developer", null },
                    { new Guid("00000000-0000-0000-0000-000000000023"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Provides legal advice and representation.", "Lawyer", null },
                    { new Guid("00000000-0000-0000-0000-000000000024"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Treats issues related to teeth and oral health.", "Dentist", null },
                    { new Guid("00000000-0000-0000-0000-000000000025"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Captures images professionally.", "Photographer", null },
                    { new Guid("00000000-0000-0000-0000-000000000026"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Drives clients using the Uber app.", "Uber Driver", null },
                    { new Guid("00000000-0000-0000-0000-000000000027"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Drives clients using the Lyft platform.", "Lyft Driver", null },
                    { new Guid("00000000-0000-0000-0000-000000000028"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Provides transport via digital platforms.", "Rideshare Driver", null },
                    { new Guid("00000000-0000-0000-0000-000000000029"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Delivers food or packages locally.", "Delivery Driver", null },
                    { new Guid("00000000-0000-0000-0000-000000000030"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Transports documents or items locally.", "Courier", null },
                    { new Guid("00000000-0000-0000-0000-000000000031"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Works independently in various fields.", "Freelancer", null },
                    { new Guid("00000000-0000-0000-0000-000000000032"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Runs their own business or services.", "Self-Employed", null }
                });

            migrationBuilder.InsertData(
                table: "PreferredContacts",
                columns: new[] { "Id", "CreatedAt", "DeleteAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("40000000-0000-0000-0000-000000000001"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Email", null },
                    { new Guid("40000000-0000-0000-0000-000000000002"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "SMS", null },
                    { new Guid("40000000-0000-0000-0000-000000000003"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Call", null }
                });

            migrationBuilder.InsertData(
                table: "Relationships",
                columns: new[] { "Id", "CreatedAt", "DeleteAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Son", null },
                    { new Guid("10000000-0000-0000-0000-000000000002"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Daughter", null },
                    { new Guid("10000000-0000-0000-0000-000000000003"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Spouse", null },
                    { new Guid("10000000-0000-0000-0000-000000000004"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Father", null },
                    { new Guid("10000000-0000-0000-0000-000000000005"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Mother", null },
                    { new Guid("10000000-0000-0000-0000-000000000006"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Brother", null },
                    { new Guid("10000000-0000-0000-0000-000000000007"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Sister", null },
                    { new Guid("10000000-0000-0000-0000-000000000008"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Grandparent", null },
                    { new Guid("10000000-0000-0000-0000-000000000009"), new DateTime(2025, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), null, "Other", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_CustomerId",
                table: "Addresses",
                column: "CustomerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContactInfos_CustomerId",
                table: "ContactInfos",
                column: "CustomerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContactInfos_PreferredContactId",
                table: "ContactInfos",
                column: "PreferredContactId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_MaritalStatusId",
                table: "Customers",
                column: "MaritalStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_OccupationId",
                table: "Customers",
                column: "OccupationId");

            migrationBuilder.CreateIndex(
                name: "IX_Dependents_CustomerId",
                table: "Dependents",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Dependents_RelationshipId",
                table: "Dependents",
                column: "RelationshipId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxInformations_CustomerId",
                table: "TaxInformations",
                column: "CustomerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxInformations_FilingStatusId",
                table: "TaxInformations",
                column: "FilingStatusId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Addresses");

            migrationBuilder.DropTable(
                name: "ContactInfos");

            migrationBuilder.DropTable(
                name: "Dependents");

            migrationBuilder.DropTable(
                name: "TaxInformations");

            migrationBuilder.DropTable(
                name: "PreferredContacts");

            migrationBuilder.DropTable(
                name: "Relationships");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "FilingStatuses");

            migrationBuilder.DropTable(
                name: "MaritalStatuses");

            migrationBuilder.DropTable(
                name: "Occupations");
        }
    }
}
