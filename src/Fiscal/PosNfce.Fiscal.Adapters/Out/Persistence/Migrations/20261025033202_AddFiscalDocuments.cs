using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosNfce.Fiscal.Adapters.Out.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFiscalDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fiscal_documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SaleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SoldAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Series = table.Column<int>(type: "INTEGER", nullable: true),
                    Number = table.Column<long>(type: "INTEGER", nullable: true),
                    Discount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Total = table.Column<decimal>(type: "TEXT", nullable: false),
                    Change = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fiscal_documents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "fiscal_document_items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Line = table.Column<int>(type: "INTEGER", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Gtin = table.Column<string>(type: "TEXT", maxLength: 14, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 6, nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    GrossTotal = table.Column<decimal>(type: "TEXT", nullable: false),
                    Discount = table.Column<decimal>(type: "TEXT", nullable: false),
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
                    table.PrimaryKey("PK_fiscal_document_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_fiscal_document_items_fiscal_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "fiscal_documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fiscal_document_payments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Method = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Integrated = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fiscal_document_payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_fiscal_document_payments_fiscal_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "fiscal_documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fiscal_document_items_DocumentId",
                table: "fiscal_document_items",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_fiscal_document_payments_DocumentId",
                table: "fiscal_document_payments",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_fiscal_documents_SaleId",
                table: "fiscal_documents",
                column: "SaleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fiscal_documents_Series_Number",
                table: "fiscal_documents",
                columns: new[] { "Series", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fiscal_documents_Status",
                table: "fiscal_documents",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fiscal_document_items");

            migrationBuilder.DropTable(
                name: "fiscal_document_payments");

            migrationBuilder.DropTable(
                name: "fiscal_documents");
        }
    }
}
