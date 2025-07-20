using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace signature.Migrations
{
    /// <inheritdoc />
    public partial class NewSignDocPreview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SignPreviewDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SignatureRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SignerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SealedDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccessToken = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SessionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequestFingerprint = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AccessCount = table.Column<int>(type: "int", nullable: false),
                    MaxAccessCount = table.Column<int>(type: "int", nullable: false),
                    LastAccessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastAccessIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    LastAccessUserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignPreviewDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SignPreviewDocuments_SignatureRequests_SignatureRequestId",
                        column: x => x.SignatureRequestId,
                        principalTable: "SignatureRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SignPreviewDocuments_Signers_SignerId",
                        column: x => x.SignerId,
                        principalTable: "Signers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SignPreviewDocuments_AccessToken_SessionId",
                table: "SignPreviewDocuments",
                columns: new[] { "AccessToken", "SessionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SignPreviewDocuments_Active_Expires",
                table: "SignPreviewDocuments",
                columns: new[] { "IsActive", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SignPreviewDocuments_ExpiresAt",
                table: "SignPreviewDocuments",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_SignPreviewDocuments_SealedDocumentId",
                table: "SignPreviewDocuments",
                column: "SealedDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_SignPreviewDocuments_SignatureRequestId",
                table: "SignPreviewDocuments",
                column: "SignatureRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_SignPreviewDocuments_SignerId",
                table: "SignPreviewDocuments",
                column: "SignerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SignPreviewDocuments");
        }
    }
}
