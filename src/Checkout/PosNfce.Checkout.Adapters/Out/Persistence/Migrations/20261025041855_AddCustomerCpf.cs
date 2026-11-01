using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosNfce.Checkout.Adapters.Out.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerCpf : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerCpf",
                table: "checkout_sales",
                type: "TEXT",
                maxLength: 11,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerCpf",
                table: "checkout_sales");
        }
    }
}
