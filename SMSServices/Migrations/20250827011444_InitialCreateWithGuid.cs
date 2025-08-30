using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SMSServices.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateWithGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SmsMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageSid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    From = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    To = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(1600)", maxLength: 1600, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(10,4)", nullable: true),
                    PriceUnit = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    NumSegments = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    NumMedia = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ErrorCode = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AccountSid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MessagingServiceSid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmsMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SmsTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Template = table.Column<string>(type: "nvarchar(1600)", maxLength: 1600, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmsTemplates", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "SmsTemplates",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "Description", "IsActive", "Name", "Template", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", "Plantilla para códigos de verificación", true, "Verification", "Tu código de verificación es: {codigo}. Válido por 10 minutos.", null },
                    { new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", "Mensaje de bienvenida", true, "Welcome", "¡Bienvenido {nombre}! Gracias por registrarte en nuestro servicio.", null },
                    { new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", "Confirmación de pedido", true, "OrderConfirmation", "Tu pedido #{orderId} ha sido confirmado. Total: ${total}. Tiempo estimado: {tiempo}.", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_SmsMessages_CreatedAt",
                table: "SmsMessages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SmsMessages_Direction",
                table: "SmsMessages",
                column: "Direction");

            migrationBuilder.CreateIndex(
                name: "IX_SmsMessages_From",
                table: "SmsMessages",
                column: "From");

            migrationBuilder.CreateIndex(
                name: "IX_SmsMessages_MessageSid",
                table: "SmsMessages",
                column: "MessageSid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SmsMessages_Status",
                table: "SmsMessages",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SmsMessages_To",
                table: "SmsMessages",
                column: "To");

            migrationBuilder.CreateIndex(
                name: "IX_SmsTemplates_Name",
                table: "SmsTemplates",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SmsMessages");

            migrationBuilder.DropTable(
                name: "SmsTemplates");
        }
    }
}
