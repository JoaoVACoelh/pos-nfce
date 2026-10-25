using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosNfce.Catalog.Adapters.Out.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalog_products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Gtin = table.Column<string>(type: "TEXT", maxLength: 14, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Ncm = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    Cest = table.Column<string>(type: "TEXT", maxLength: 7, nullable: true),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 6, nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", nullable: false),
                    Origin = table.Column<int>(type: "INTEGER", nullable: false),
                    TaxRuleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_products", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_products_Code",
                table: "catalog_products",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_products_Gtin",
                table: "catalog_products",
                column: "Gtin",
                unique: true,
                filter: "\"Gtin\" <> 'SEM GTIN'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_products");
        }
    }
}
