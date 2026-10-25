using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosNfce.Catalog.Adapters.Out.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIbptRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalog_ibpt_rates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Ncm = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    Ex = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    FederalNational = table.Column<decimal>(type: "TEXT", nullable: false),
                    FederalImported = table.Column<decimal>(type: "TEXT", nullable: false),
                    State = table.Column<decimal>(type: "TEXT", nullable: false),
                    Municipal = table.Column<decimal>(type: "TEXT", nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ValidUntil = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Version = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_ibpt_rates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_ibpt_rates_Ncm_Ex",
                table: "catalog_ibpt_rates",
                columns: new[] { "Ncm", "Ex" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_ibpt_rates");
        }
    }
}
