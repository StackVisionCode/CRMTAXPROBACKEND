using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmailServices.Migrations
{
    /// <inheritdoc />
    public partial class NewMigrationsR : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IncomingEmails_MessageId_ConfigId_Unique",
                table: "IncomingEmails");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "IncomingEmails",
                newName: "CreatedByTaxUserId");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "EmailTemplates",
                newName: "CreatedByTaxUserId");

            migrationBuilder.RenameIndex(
                name: "IX_EmailTemplates_UserId",
                table: "EmailTemplates",
                newName: "IX_EmailTemplates_CreatedByTaxUserId");

            migrationBuilder.RenameColumn(
                name: "SentByUserId",
                table: "Emails",
                newName: "SentByTaxUserId");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "EmailConfigs",
                newName: "CreatedByTaxUserId");

            migrationBuilder.RenameIndex(
                name: "IX_EmailConfigs_UserId",
                table: "EmailConfigs",
                newName: "IX_EmailConfigs_CreatedByTaxUserId");

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "IncomingEmails",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "EmailTemplates",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "LastModifiedByTaxUserId",
                table: "EmailTemplates",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CcAddresses",
                table: "Emails",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BccAddresses",
                table: "Emails",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "Emails",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByTaxUserId",
                table: "Emails",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "LastModifiedByTaxUserId",
                table: "Emails",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "Emails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedOn",
                table: "EmailConfigs",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "EmailConfigs",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "EmailConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "LastModifiedByTaxUserId",
                table: "EmailConfigs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "EmailAttachments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_IncomingEmails_CompanyId",
                table: "IncomingEmails",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_IncomingEmails_CompanyId_IsRead",
                table: "IncomingEmails",
                columns: new[] { "CompanyId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_IncomingEmails_CompanyId_ReceivedOn",
                table: "IncomingEmails",
                columns: new[] { "CompanyId", "ReceivedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_IncomingEmails_MessageId_ConfigId_CompanyId_Unique",
                table: "IncomingEmails",
                columns: new[] { "MessageId", "ConfigId", "CompanyId" },
                unique: true,
                filter: "[MessageId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_CompanyId",
                table: "EmailTemplates",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_CompanyId_IsActive",
                table: "EmailTemplates",
                columns: new[] { "CompanyId", "IsActive" },
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_CompanyId_Name_Unique",
                table: "EmailTemplates",
                columns: new[] { "CompanyId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Emails_CompanyId",
                table: "Emails",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Emails_CompanyId_ConfigId",
                table: "Emails",
                columns: new[] { "CompanyId", "ConfigId" });

            migrationBuilder.CreateIndex(
                name: "IX_Emails_CompanyId_Status_CreatedOn",
                table: "Emails",
                columns: new[] { "CompanyId", "Status", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_Emails_CreatedByTaxUserId",
                table: "Emails",
                column: "CreatedByTaxUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Emails_SentByTaxUserId",
                table: "Emails",
                column: "SentByTaxUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailConfigs_CompanyId",
                table: "EmailConfigs",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailConfigs_CompanyId_IsActive",
                table: "EmailConfigs",
                columns: new[] { "CompanyId", "IsActive" },
                filter: "[IsActive] = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EmailConfigs_DailyLimit",
                table: "EmailConfigs",
                sql: "[DailyLimit] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EmailConfigs_GmailConfig",
                table: "EmailConfigs",
                sql: "([ProviderType] != 'Gmail') OR ([GmailClientId] IS NOT NULL AND [GmailEmailAddress] IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EmailConfigs_SmtpConfig",
                table: "EmailConfigs",
                sql: "([ProviderType] != 'Smtp') OR ([SmtpServer] IS NOT NULL AND [SmtpPort] IS NOT NULL AND [SmtpUsername] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_EmailAttachments_CompanyId",
                table: "EmailAttachments",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailAttachments_CompanyId_EmailId",
                table: "EmailAttachments",
                columns: new[] { "CompanyId", "EmailId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IncomingEmails_CompanyId",
                table: "IncomingEmails");

            migrationBuilder.DropIndex(
                name: "IX_IncomingEmails_CompanyId_IsRead",
                table: "IncomingEmails");

            migrationBuilder.DropIndex(
                name: "IX_IncomingEmails_CompanyId_ReceivedOn",
                table: "IncomingEmails");

            migrationBuilder.DropIndex(
                name: "IX_IncomingEmails_MessageId_ConfigId_CompanyId_Unique",
                table: "IncomingEmails");

            migrationBuilder.DropIndex(
                name: "IX_EmailTemplates_CompanyId",
                table: "EmailTemplates");

            migrationBuilder.DropIndex(
                name: "IX_EmailTemplates_CompanyId_IsActive",
                table: "EmailTemplates");

            migrationBuilder.DropIndex(
                name: "IX_EmailTemplates_CompanyId_Name_Unique",
                table: "EmailTemplates");

            migrationBuilder.DropIndex(
                name: "IX_Emails_CompanyId",
                table: "Emails");

            migrationBuilder.DropIndex(
                name: "IX_Emails_CompanyId_ConfigId",
                table: "Emails");

            migrationBuilder.DropIndex(
                name: "IX_Emails_CompanyId_Status_CreatedOn",
                table: "Emails");

            migrationBuilder.DropIndex(
                name: "IX_Emails_CreatedByTaxUserId",
                table: "Emails");

            migrationBuilder.DropIndex(
                name: "IX_Emails_SentByTaxUserId",
                table: "Emails");

            migrationBuilder.DropIndex(
                name: "IX_EmailConfigs_CompanyId",
                table: "EmailConfigs");

            migrationBuilder.DropIndex(
                name: "IX_EmailConfigs_CompanyId_IsActive",
                table: "EmailConfigs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EmailConfigs_DailyLimit",
                table: "EmailConfigs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EmailConfigs_GmailConfig",
                table: "EmailConfigs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EmailConfigs_SmtpConfig",
                table: "EmailConfigs");

            migrationBuilder.DropIndex(
                name: "IX_EmailAttachments_CompanyId",
                table: "EmailAttachments");

            migrationBuilder.DropIndex(
                name: "IX_EmailAttachments_CompanyId_EmailId",
                table: "EmailAttachments");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "IncomingEmails");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "EmailTemplates");

            migrationBuilder.DropColumn(
                name: "LastModifiedByTaxUserId",
                table: "EmailTemplates");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Emails");

            migrationBuilder.DropColumn(
                name: "CreatedByTaxUserId",
                table: "Emails");

            migrationBuilder.DropColumn(
                name: "LastModifiedByTaxUserId",
                table: "Emails");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "Emails");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "EmailConfigs");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "EmailConfigs");

            migrationBuilder.DropColumn(
                name: "LastModifiedByTaxUserId",
                table: "EmailConfigs");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "EmailAttachments");

            migrationBuilder.RenameColumn(
                name: "CreatedByTaxUserId",
                table: "IncomingEmails",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "CreatedByTaxUserId",
                table: "EmailTemplates",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_EmailTemplates_CreatedByTaxUserId",
                table: "EmailTemplates",
                newName: "IX_EmailTemplates_UserId");

            migrationBuilder.RenameColumn(
                name: "SentByTaxUserId",
                table: "Emails",
                newName: "SentByUserId");

            migrationBuilder.RenameColumn(
                name: "CreatedByTaxUserId",
                table: "EmailConfigs",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_EmailConfigs_CreatedByTaxUserId",
                table: "EmailConfigs",
                newName: "IX_EmailConfigs_UserId");

            migrationBuilder.AlterColumn<string>(
                name: "CcAddresses",
                table: "Emails",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BccAddresses",
                table: "Emails",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedOn",
                table: "EmailConfigs",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.CreateIndex(
                name: "IX_IncomingEmails_MessageId_ConfigId_Unique",
                table: "IncomingEmails",
                columns: new[] { "MessageId", "ConfigId" },
                unique: true,
                filter: "[MessageId] IS NOT NULL");
        }
    }
}
