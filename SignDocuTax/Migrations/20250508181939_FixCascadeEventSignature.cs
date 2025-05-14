using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SignDocuTax.Migrations
{
    /// <inheritdoc />
    public partial class FixCascadeEventSignature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Contacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserTaxId = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentStatus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentStatus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FirmStatus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FirmStatus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SignatureEventTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignatureEventTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SignatureStatus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignatureStatus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SignatureType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignatureType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StatusRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatusRequirements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    TaxUserId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DocumentStatusId = table.Column<int>(type: "int", nullable: false),
                    DocumentTypeId = table.Column<int>(type: "int", nullable: false),
                    OriginalHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignedHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignedDocumentPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsSigned = table.Column<bool>(type: "bit", nullable: false),
                    Path = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequirementSignatureId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documents_DocumentStatus_DocumentStatusId",
                        column: x => x.DocumentStatusId,
                        principalTable: "DocumentStatus",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Documents_DocumentType_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalTable: "DocumentType",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RequirementSignatures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    DocumentId = table.Column<int>(type: "int", nullable: true),
                    TaxUserId = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    ExternalSignerEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExternalSignerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StatusSignatureId = table.Column<int>(type: "int", nullable: false),
                    FirmId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    ConsentObtained = table.Column<bool>(type: "bit", nullable: false),
                    ConsentText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequirementSignatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequirementSignatures_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RequirementSignatures_StatusRequirements_StatusSignatureId",
                        column: x => x.StatusSignatureId,
                        principalTable: "StatusRequirements",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AnswerRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    TaxUserId = table.Column<int>(type: "int", nullable: false),
                    RequirementSignatureId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnswerRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnswerRequirements_RequirementSignatures_RequirementSignatureId",
                        column: x => x.RequirementSignatureId,
                        principalTable: "RequirementSignatures",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ExternalSigners",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentId = table.Column<int>(type: "int", nullable: false),
                    ContactId = table.Column<int>(type: "int", nullable: false),
                    SignatureStatusId = table.Column<int>(type: "int", nullable: false),
                    SigningToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InvitationSentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SignedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequirementSignatureId1 = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalSigners", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalSigners_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExternalSigners_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExternalSigners_RequirementSignatures_RequirementSignatureId1",
                        column: x => x.RequirementSignatureId1,
                        principalTable: "RequirementSignatures",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExternalSigners_SignatureStatus_SignatureStatusId",
                        column: x => x.SignatureStatusId,
                        principalTable: "SignatureStatus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Firms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    SignatureTypeId = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    TaxUserId = table.Column<int>(type: "int", nullable: false),
                    FirmStatusId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Path = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CertificateThumbprint = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CertificateExpiry = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequirementSignatureId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Firms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Firms_FirmStatus_FirmStatusId",
                        column: x => x.FirmStatusId,
                        principalTable: "FirmStatus",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Firms_RequirementSignatures_RequirementSignatureId",
                        column: x => x.RequirementSignatureId,
                        principalTable: "RequirementSignatures",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Firms_SignatureType_SignatureTypeId",
                        column: x => x.SignatureTypeId,
                        principalTable: "SignatureType",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EventSignatures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequirementSignatureId = table.Column<int>(type: "int", nullable: false),
                    AnswerRequirementId = table.Column<int>(type: "int", nullable: false),
                    DocumentId = table.Column<int>(type: "int", nullable: false),
                    TaxUserId = table.Column<int>(type: "int", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    ExternalSignerId = table.Column<int>(type: "int", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeviceName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeviceOs = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Browser = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignatureDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DigitalSignatureHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignatureImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuditTrailJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    SignatureEventTypeId = table.Column<int>(type: "int", nullable: false),
                    TimestampToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimestampAuthority = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignatureLevel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DocumentHashAtSigning = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequirementSignatureId1 = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventSignatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventSignatures_AnswerRequirements_AnswerRequirementId",
                        column: x => x.AnswerRequirementId,
                        principalTable: "AnswerRequirements",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EventSignatures_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventSignatures_ExternalSigners_ExternalSignerId",
                        column: x => x.ExternalSignerId,
                        principalTable: "ExternalSigners",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EventSignatures_RequirementSignatures_RequirementSignatureId",
                        column: x => x.RequirementSignatureId,
                        principalTable: "RequirementSignatures",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EventSignatures_RequirementSignatures_RequirementSignatureId1",
                        column: x => x.RequirementSignatureId1,
                        principalTable: "RequirementSignatures",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EventSignatures_SignatureEventTypes_SignatureEventTypeId",
                        column: x => x.SignatureEventTypeId,
                        principalTable: "SignatureEventTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnswerRequirements_RequirementSignatureId",
                table: "AnswerRequirements",
                column: "RequirementSignatureId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_DocumentStatusId",
                table: "Documents",
                column: "DocumentStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_DocumentTypeId",
                table: "Documents",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EventSignatures_AnswerRequirementId",
                table: "EventSignatures",
                column: "AnswerRequirementId");

            migrationBuilder.CreateIndex(
                name: "IX_EventSignatures_DocumentId",
                table: "EventSignatures",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_EventSignatures_ExternalSignerId",
                table: "EventSignatures",
                column: "ExternalSignerId");

            migrationBuilder.CreateIndex(
                name: "IX_EventSignatures_RequirementSignatureId",
                table: "EventSignatures",
                column: "RequirementSignatureId");

            migrationBuilder.CreateIndex(
                name: "IX_EventSignatures_RequirementSignatureId1",
                table: "EventSignatures",
                column: "RequirementSignatureId1");

            migrationBuilder.CreateIndex(
                name: "IX_EventSignatures_SignatureEventTypeId",
                table: "EventSignatures",
                column: "SignatureEventTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalSigners_ContactId",
                table: "ExternalSigners",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalSigners_DocumentId",
                table: "ExternalSigners",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalSigners_RequirementSignatureId1",
                table: "ExternalSigners",
                column: "RequirementSignatureId1");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalSigners_SignatureStatusId",
                table: "ExternalSigners",
                column: "SignatureStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Firms_FirmStatusId",
                table: "Firms",
                column: "FirmStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Firms_RequirementSignatureId",
                table: "Firms",
                column: "RequirementSignatureId");

            migrationBuilder.CreateIndex(
                name: "IX_Firms_SignatureTypeId",
                table: "Firms",
                column: "SignatureTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_RequirementSignatures_DocumentId",
                table: "RequirementSignatures",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_RequirementSignatures_StatusSignatureId",
                table: "RequirementSignatures",
                column: "StatusSignatureId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventSignatures");

            migrationBuilder.DropTable(
                name: "Firms");

            migrationBuilder.DropTable(
                name: "AnswerRequirements");

            migrationBuilder.DropTable(
                name: "ExternalSigners");

            migrationBuilder.DropTable(
                name: "SignatureEventTypes");

            migrationBuilder.DropTable(
                name: "FirmStatus");

            migrationBuilder.DropTable(
                name: "SignatureType");

            migrationBuilder.DropTable(
                name: "Contacts");

            migrationBuilder.DropTable(
                name: "RequirementSignatures");

            migrationBuilder.DropTable(
                name: "SignatureStatus");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "StatusRequirements");

            migrationBuilder.DropTable(
                name: "DocumentStatus");

            migrationBuilder.DropTable(
                name: "DocumentType");
        }
    }
}
