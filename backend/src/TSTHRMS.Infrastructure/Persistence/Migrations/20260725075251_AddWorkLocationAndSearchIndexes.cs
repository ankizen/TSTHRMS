using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TSTHRMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkLocationAndSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WorkLocation",
                table: "Employees",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_TenantId_Department",
                table: "Employees",
                columns: new[] { "TenantId", "Department" });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_TenantId_Designation",
                table: "Employees",
                columns: new[] { "TenantId", "Designation" });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_TenantId_WorkLocation",
                table: "Employees",
                columns: new[] { "TenantId", "WorkLocation" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Employees_TenantId_Department",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_TenantId_Designation",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_TenantId_WorkLocation",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "WorkLocation",
                table: "Employees");
        }
    }
}
