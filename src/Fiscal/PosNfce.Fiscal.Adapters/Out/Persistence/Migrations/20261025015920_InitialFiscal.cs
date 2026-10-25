using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosNfce.Fiscal.Adapters.Out.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialFiscal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fiscal_issuers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Cnpj = table.Column<string>(type: "TEXT", maxLength: 14, nullable: false),
                    StateRegistration = table.Column<string>(type: "TEXT", maxLength: 14, nullable: false),
                    LegalName = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    TradeName = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    TaxRegime = table.Column<int>(type: "INTEGER", nullable: false),
                    Street = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Number = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Complement = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    District = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    CityName = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    CityCode = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false),
                    State = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    PostalCode = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 14, nullable: true),
                    Series = table.Column<int>(type: "INTEGER", nullable: false),
                    NextNumber = table.Column<long>(type: "INTEGER", nullable: false),
                    CscTokenId = table.Column<string>(type: "TEXT", maxLength: 6, nullable: true),
                    CscEncrypted = table.Column<byte[]>(type: "BLOB", nullable: true),
                    CertificatePfxEncrypted = table.Column<byte[]>(type: "BLOB", nullable: true),
                    CertificatePasswordEncrypted = table.Column<byte[]>(type: "BLOB", nullable: true),
                    CertificateCnpj = table.Column<string>(type: "TEXT", maxLength: 14, nullable: true),
                    CertificateSubject = table.Column<string>(type: "TEXT", nullable: true),
                    CertificateNotAfter = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fiscal_issuers", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fiscal_issuers");
        }
    }
}
