using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosNfce.Fiscal.Adapters.Out.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIbsCbs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "IbsCbsReduction",
                table: "fiscal_document_items",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "fiscal_ibs_cbs_rates",
                columns: table => new
                {
                    ValidFrom = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    IbsState = table.Column<decimal>(type: "TEXT", nullable: false),
                    IbsMunicipal = table.Column<decimal>(type: "TEXT", nullable: false),
                    Cbs = table.Column<decimal>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fiscal_ibs_cbs_rates", x => x.ValidFrom);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fiscal_ibs_cbs_rates");

            migrationBuilder.DropColumn(
                name: "IbsCbsReduction",
                table: "fiscal_document_items");
        }
    }
}
