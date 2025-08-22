using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommLinkService.Migrations
{
    /// <inheritdoc />
    public partial class NewMigrationR : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Connections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserType = table.Column<int>(type: "int", nullable: false),
                    TaxUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConnectionId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ConnectedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DisconnectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Connections", x => x.Id);
                    table.CheckConstraint("CK_Connection_ValidUser", "([UserType] = 0 AND [TaxUserId] IS NOT NULL AND [CustomerId] IS NULL) OR ([UserType] = 1 AND [CustomerId] IS NOT NULL AND [TaxUserId] IS NULL)");
                });

            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    CreatedByCompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByTaxUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastModifiedByTaxUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastActivityAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    MaxParticipants = table.Column<int>(type: "int", nullable: false, defaultValue: 10),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SenderType = table.Column<int>(type: "int", nullable: false),
                    SenderTaxUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SenderCustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SenderCompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                    table.CheckConstraint("CK_Message_ValidSender", "([SenderType] = 0 AND [SenderTaxUserId] IS NOT NULL AND [SenderCustomerId] IS NULL) OR ([SenderType] = 1 AND [SenderCustomerId] IS NOT NULL AND [SenderTaxUserId] IS NULL)");
                    table.ForeignKey(
                        name: "FK_Messages_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoomParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParticipantType = table.Column<int>(type: "int", nullable: false),
                    TaxUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AddedByCompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AddedByTaxUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsMuted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsVideoEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomParticipants", x => x.Id);
                    table.CheckConstraint("CK_RoomParticipant_ValidParticipant", "([ParticipantType] = 0 AND [TaxUserId] IS NOT NULL AND [CustomerId] IS NULL) OR ([ParticipantType] = 1 AND [CustomerId] IS NOT NULL AND [TaxUserId] IS NULL)");
                    table.ForeignKey(
                        name: "FK_RoomParticipants_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MessageReactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReactorType = table.Column<int>(type: "int", nullable: false),
                    ReactorTaxUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReactorCustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReactorCompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Emoji = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReactedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageReactions", x => x.Id);
                    table.CheckConstraint("CK_MessageReaction_ValidReactor", "([ReactorType] = 0 AND [ReactorTaxUserId] IS NOT NULL AND [ReactorCustomerId] IS NULL) OR ([ReactorType] = 1 AND [ReactorCustomerId] IS NOT NULL AND [ReactorTaxUserId] IS NULL)");
                    table.ForeignKey(
                        name: "FK_MessageReactions_Messages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "Messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Connections_ConnectionId",
                table: "Connections",
                column: "ConnectionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Connections_CustomerId",
                table: "Connections",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Connections_IsActive",
                table: "Connections",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Connections_TaxUserId",
                table: "Connections",
                column: "TaxUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageReactions_Message_Customer_Emoji",
                table: "MessageReactions",
                columns: new[] { "MessageId", "ReactorCustomerId", "Emoji" },
                unique: true,
                filter: "[ReactorCustomerId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MessageReactions_Message_TaxUser_Emoji",
                table: "MessageReactions",
                columns: new[] { "MessageId", "ReactorTaxUserId", "Emoji" },
                unique: true,
                filter: "[ReactorTaxUserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MessageReactions_MessageId",
                table: "MessageReactions",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageReactions_ReactorCustomerId",
                table: "MessageReactions",
                column: "ReactorCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageReactions_ReactorTaxUserId",
                table: "MessageReactions",
                column: "ReactorTaxUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_Room_SentAt",
                table: "Messages",
                columns: new[] { "RoomId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_RoomId",
                table: "Messages",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SenderCustomerId",
                table: "Messages",
                column: "SenderCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SenderTaxUserId",
                table: "Messages",
                column: "SenderTaxUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SentAt",
                table: "Messages",
                column: "SentAt");

            migrationBuilder.CreateIndex(
                name: "IX_RoomParticipants_CompanyId",
                table: "RoomParticipants",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomParticipants_CustomerId",
                table: "RoomParticipants",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomParticipants_Room_Customer",
                table: "RoomParticipants",
                columns: new[] { "RoomId", "CustomerId" },
                unique: true,
                filter: "[CustomerId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RoomParticipants_Room_TaxUser",
                table: "RoomParticipants",
                columns: new[] { "RoomId", "TaxUserId" },
                unique: true,
                filter: "[TaxUserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RoomParticipants_RoomId",
                table: "RoomParticipants",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomParticipants_TaxUserId",
                table: "RoomParticipants",
                column: "TaxUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_CreatedByCompanyId",
                table: "Rooms",
                column: "CreatedByCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_CreatedByTaxUserId",
                table: "Rooms",
                column: "CreatedByTaxUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_LastActivityAt",
                table: "Rooms",
                column: "LastActivityAt");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_Type_IsActive",
                table: "Rooms",
                columns: new[] { "Type", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Connections");

            migrationBuilder.DropTable(
                name: "MessageReactions");

            migrationBuilder.DropTable(
                name: "RoomParticipants");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "Rooms");
        }
    }
}
