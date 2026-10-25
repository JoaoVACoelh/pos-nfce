using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosNfce.Catalog.Adapters.Out.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIbsCbsReduction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "IbsCbsReduction",
                table: "catalog_tax_rules",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IbsCbsReduction",
                table: "catalog_tax_rules");
        }
    }
}
