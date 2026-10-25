using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosNfce.Fiscal.Adapters.Out.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSefazAnswerAndCpf : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoIssue",
                table: "fiscal_issuers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AuthorizedAt",
                table: "fiscal_documents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerCpf",
                table: "fiscal_documents",
                type: "TEXT",
                maxLength: 11,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastMessage",
                table: "fiscal_documents",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastStatusCode",
                table: "fiscal_documents",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Protocol",
                table: "fiscal_documents",
                type: "TEXT",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoIssue",
                table: "fiscal_issuers");

            migrationBuilder.DropColumn(
                name: "AuthorizedAt",
                table: "fiscal_documents");

            migrationBuilder.DropColumn(
                name: "CustomerCpf",
                table: "fiscal_documents");

            migrationBuilder.DropColumn(
                name: "LastMessage",
                table: "fiscal_documents");

            migrationBuilder.DropColumn(
                name: "LastStatusCode",
                table: "fiscal_documents");

            migrationBuilder.DropColumn(
                name: "Protocol",
                table: "fiscal_documents");
        }
    }
}
