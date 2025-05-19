using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmailServices.Migrations
{
    /// <inheritdoc />
    public partial class InitialEmailServiceMigrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ProviderType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    SmtpServer = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    SmtpPort = table.Column<int>(type: "int", nullable: true),
                    EnableSsl = table.Column<bool>(type: "bit", nullable: true),
                    SmtpUsername = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    SmtpPassword = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    GmailClientId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    GmailClientSecret = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GmailRefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GmailAccessToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GmailTokenExpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GmailEmailAddress = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    DailyLimit = table.Column<int>(type: "int", nullable: false, defaultValue: 100),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Emails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConfigId = table.Column<int>(type: "int", nullable: false),
                    FromAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ToAddresses = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CcAddresses = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BccAddresses = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    SentOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SentByUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Emails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Emails_EmailConfigs_ConfigId",
                        column: x => x.ConfigId,
                        principalTable: "EmailConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailConfigs_CompanyId",
                table: "EmailConfigs",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailConfigs_UserId",
                table: "EmailConfigs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Emails_ConfigId",
                table: "Emails",
                column: "ConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_Emails_SentOn",
                table: "Emails",
                column: "SentOn");

            migrationBuilder.CreateIndex(
                name: "IX_Emails_Status_CreatedOn",
                table: "Emails",
                columns: new[] { "Status", "CreatedOn" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Emails");

            migrationBuilder.DropTable(
                name: "EmailConfigs");
        }
    }
}
