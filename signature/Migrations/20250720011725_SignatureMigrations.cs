using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace signature.Migrations
{
    /// <inheritdoc />
    public partial class SignatureMigrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SignatureRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RejectedBySignerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RejectReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RejectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignatureRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Signers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    SignatureImage = table.Column<string>(type: "varchar(max)", nullable: true),
                    CertThumbprint = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CertSubject = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CertNotBefore = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CertNotAfter = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SignedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClientIp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConsentAgreedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConsentText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConsentButtonText = table.Column<bool>(type: "bit", nullable: true),
                    SignatureRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RejectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Signers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Signers_SignatureRequests_SignatureRequestId",
                        column: x => x.SignatureRequestId,
                        principalTable: "SignatureRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SignatureBoxes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SignerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PageNumber = table.Column<int>(type: "int", nullable: false),
                    PositionX = table.Column<double>(type: "float", nullable: false),
                    PositionY = table.Column<double>(type: "float", nullable: false),
                    Width = table.Column<double>(type: "float", nullable: false),
                    Height = table.Column<double>(type: "float", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    InitialValue = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    WidthIntial = table.Column<double>(type: "float", nullable: true),
                    HeightIntial = table.Column<double>(type: "float", nullable: true),
                    PositionXIntial = table.Column<double>(type: "float", nullable: true),
                    PositionYIntial = table.Column<double>(type: "float", nullable: true),
                    FechaValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WidthFechaSigner = table.Column<double>(type: "float", nullable: true),
                    HeightFechaSigner = table.Column<double>(type: "float", nullable: true),
                    PositionXFechaSigner = table.Column<double>(type: "float", nullable: true),
                    PositionYFechaSigner = table.Column<double>(type: "float", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignatureBoxes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SignatureBoxes_Signers_SignerId",
                        column: x => x.SignerId,
                        principalTable: "Signers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SignatureBoxes_SignerId",
                table: "SignatureBoxes",
                column: "SignerId");

            migrationBuilder.CreateIndex(
                name: "IX_SignatureBoxes_SignerId_PageNumber",
                table: "SignatureBoxes",
                columns: new[] { "SignerId", "PageNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_Signers_SignatureRequestId",
                table: "Signers",
                column: "SignatureRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SignatureBoxes");

            migrationBuilder.DropTable(
                name: "Signers");

            migrationBuilder.DropTable(
                name: "SignatureRequests");
        }
    }
}
