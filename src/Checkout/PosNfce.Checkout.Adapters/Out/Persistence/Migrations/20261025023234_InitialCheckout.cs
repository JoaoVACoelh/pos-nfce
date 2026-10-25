using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosNfce.Checkout.Adapters.Out.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCheckout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "checkout_sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    OpenedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Discount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Total = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_checkout_sales", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "checkout_payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SaleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Method = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Integrated = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_checkout_payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_checkout_payments_checkout_sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "checkout_sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "checkout_sale_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SaleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Line = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Gtin = table.Column<string>(type: "TEXT", maxLength: 14, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 6, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    Total = table.Column<decimal>(type: "TEXT", nullable: false),
                    Ncm = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    Cest = table.Column<string>(type: "TEXT", maxLength: 7, nullable: true),
                    Origin = table.Column<int>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_checkout_sale_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_checkout_sale_items_checkout_sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "checkout_sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_checkout_payments_SaleId",
                table: "checkout_payments",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_checkout_sale_items_SaleId",
                table: "checkout_sale_items",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_checkout_sales_Status",
                table: "checkout_sales",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "checkout_payments");

            migrationBuilder.DropTable(
                name: "checkout_sale_items");

            migrationBuilder.DropTable(
                name: "checkout_sales");
        }
    }
}
