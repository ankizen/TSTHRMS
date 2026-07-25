using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TSTHRMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProbationAndContractTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "ConfirmationDate",
                table: "Employees",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConfirmationStatus",
                table: "Employees",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<Guid>(
                name: "ConfirmingManagerId",
                table: "Employees",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<DateOnly>(
                name: "ContractEndDate",
                table: "Employees",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ContractStartDate",
                table: "Employees",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ProbationEndDate",
                table: "Employees",
                type: "date",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_ConfirmingManagerId",
                table: "Employees",
                column: "ConfirmingManagerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Employees_ConfirmingManagerId",
                table: "Employees",
                column: "ConfirmingManagerId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Employees_ConfirmingManagerId",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_ConfirmingManagerId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ConfirmationDate",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ConfirmationStatus",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ConfirmingManagerId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ContractEndDate",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ContractStartDate",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ProbationEndDate",
                table: "Employees");
        }
    }
}
