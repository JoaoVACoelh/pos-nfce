using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosNfce.Catalog.Adapters.Out.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalog_tax_rules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Cfop = table.Column<string>(type: "TEXT", maxLength: 4, nullable: false),
                    Csosn = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    PisCst = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    PisRate = table.Column<decimal>(type: "TEXT", nullable: false),
                    CofinsCst = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    CofinsRate = table.Column<decimal>(type: "TEXT", nullable: false),
                    IbsCbsCst = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    ClassTrib = table.Column<string>(type: "TEXT", maxLength: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_tax_rules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_products_TaxRuleId",
                table: "catalog_products",
                column: "TaxRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_tax_rules_Name",
                table: "catalog_tax_rules",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_catalog_products_catalog_tax_rules_TaxRuleId",
                table: "catalog_products",
                column: "TaxRuleId",
                principalTable: "catalog_tax_rules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_catalog_products_catalog_tax_rules_TaxRuleId",
                table: "catalog_products");

            migrationBuilder.DropTable(
                name: "catalog_tax_rules");

            migrationBuilder.DropIndex(
                name: "IX_catalog_products_TaxRuleId",
                table: "catalog_products");
        }
    }
}
