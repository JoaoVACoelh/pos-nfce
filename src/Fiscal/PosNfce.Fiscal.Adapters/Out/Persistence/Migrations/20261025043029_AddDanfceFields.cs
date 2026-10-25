using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosNfce.Fiscal.Adapters.Out.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDanfceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdditionalInfo",
                table: "fiscal_documents",
                type: "TEXT",
                maxLength: 5000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConsultUrl",
                table: "fiscal_documents",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QrCode",
                table: "fiscal_documents",
                type: "TEXT",
                maxLength: 600,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdditionalInfo",
                table: "fiscal_documents");

            migrationBuilder.DropColumn(
                name: "ConsultUrl",
                table: "fiscal_documents");

            migrationBuilder.DropColumn(
                name: "QrCode",
                table: "fiscal_documents");
        }
    }
}
