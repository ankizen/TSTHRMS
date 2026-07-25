using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TSTHRMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStatutoryComplianceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEsicRegistered",
                table: "LegalEntities",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPfRegistered",
                table: "LegalEntities",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DateOfBirthProofType",
                table: "Employees",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyGrossSalary",
                table: "Employees",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PoshAcknowledgedAt",
                table: "Employees",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfessionalTaxState",
                table: "Employees",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsEsicRegistered",
                table: "LegalEntities");

            migrationBuilder.DropColumn(
                name: "IsPfRegistered",
                table: "LegalEntities");

            migrationBuilder.DropColumn(
                name: "DateOfBirthProofType",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "MonthlyGrossSalary",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "PoshAcknowledgedAt",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ProfessionalTaxState",
                table: "Employees");
        }
    }
}
