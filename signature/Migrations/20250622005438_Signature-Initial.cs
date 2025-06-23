using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace signature.Migrations
{
    /// <inheritdoc />
    public partial class SignatureInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "Signers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Signers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "FechaValue",
                table: "Signers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "Height",
                table: "Signers",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<double>(
                name: "HeightFechaSigner",
                table: "Signers",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "HeightIntial",
                table: "Signers",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InitialValue",
                table: "Signers",
                type: "nvarchar(4)",
                maxLength: 4,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PositionXFechaSigner",
                table: "Signers",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PositionXIntial",
                table: "Signers",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PositionYFechaSigner",
                table: "Signers",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PositionYIntial",
                table: "Signers",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "Width",
                table: "Signers",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<double>(
                name: "WidthFechaSigner",
                table: "Signers",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "WidthIntial",
                table: "Signers",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaValue",
                table: "Signers");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "Signers");

            migrationBuilder.DropColumn(
                name: "HeightFechaSigner",
                table: "Signers");

            migrationBuilder.DropColumn(
                name: "HeightIntial",
                table: "Signers");

            migrationBuilder.DropColumn(
                name: "InitialValue",
                table: "Signers");

            migrationBuilder.DropColumn(
                name: "PositionXFechaSigner",
                table: "Signers");

            migrationBuilder.DropColumn(
                name: "PositionXIntial",
                table: "Signers");

            migrationBuilder.DropColumn(
                name: "PositionYFechaSigner",
                table: "Signers");

            migrationBuilder.DropColumn(
                name: "PositionYIntial",
                table: "Signers");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "Signers");

            migrationBuilder.DropColumn(
                name: "WidthFechaSigner",
                table: "Signers");

            migrationBuilder.DropColumn(
                name: "WidthIntial",
                table: "Signers");

            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "Signers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Signers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
