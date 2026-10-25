using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosNfce.Fiscal.Adapters.Out.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiptPrinter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fiscal_receipt_printer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Connection = table.Column<int>(type: "INTEGER", nullable: false),
                    PrinterName = table.Column<string>(type: "TEXT", maxLength: 260, nullable: true),
                    Host = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Port = table.Column<int>(type: "INTEGER", nullable: false),
                    Columns = table.Column<int>(type: "INTEGER", nullable: false),
                    CodePage = table.Column<int>(type: "INTEGER", nullable: false),
                    QrCode = table.Column<int>(type: "INTEGER", nullable: false),
                    CutPaper = table.Column<bool>(type: "INTEGER", nullable: false),
                    PrintAfterAuthorization = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fiscal_receipt_printer", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fiscal_receipt_printer");
        }
    }
}
