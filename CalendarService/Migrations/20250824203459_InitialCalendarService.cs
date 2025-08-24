using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CalendarService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCalendarService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "CalendarEvents");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "CalendarEvents",
                type: "nvarchar(21)",
                maxLength: 21,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerId",
                table: "CalendarEvents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EndUtc",
                table: "CalendarEvents",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "CalendarEvents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MeetingLink",
                table: "CalendarEvents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "CalendarEvents",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartUtc",
                table: "CalendarEvents",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "CalendarEvents",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "WithWhom",
                table: "CalendarEvents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EventParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventParticipants_CalendarEvents_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "CalendarEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_UserId_StartUtc_EndUtc",
                table: "CalendarEvents",
                columns: new[] { "UserId", "StartUtc", "EndUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EventParticipants_MeetingId",
                table: "EventParticipants",
                column: "MeetingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventParticipants");

            migrationBuilder.DropIndex(
                name: "IX_CalendarEvents_UserId_StartUtc_EndUtc",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "EndUtc",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "MeetingLink",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "StartUtc",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "WithWhom",
                table: "CalendarEvents");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "CalendarEvents",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(21)",
                oldMaxLength: 21);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndTime",
                table: "CalendarEvents",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "StartTime",
                table: "CalendarEvents",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
