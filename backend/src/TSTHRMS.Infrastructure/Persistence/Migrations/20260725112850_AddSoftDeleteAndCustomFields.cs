using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TSTHRMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteAndCustomFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IdentityDocuments_TenantId_EmployeeId_DocumentType",
                table: "IdentityDocuments");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "PreviousEmploymentRecords",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "PreviousEmploymentRecords",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "Nominees",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Nominees",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "IdentityDocuments",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "IdentityDocuments",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "FamilyMembers",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "FamilyMembers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "EducationRecords",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "EducationRecords",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CustomFieldDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Label = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FieldType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OptionsJson = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsRequired = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomFieldDefinitions", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EmployeeCustomFieldValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    EmployeeId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CustomFieldDefinitionId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Value = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeCustomFieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeCustomFieldValues_CustomFieldDefinitions_CustomField~",
                        column: x => x.CustomFieldDefinitionId,
                        principalTable: "CustomFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeCustomFieldValues_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_IdentityDocuments_TenantId_EmployeeId_DocumentType",
                table: "IdentityDocuments",
                columns: new[] { "TenantId", "EmployeeId", "DocumentType" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomFieldDefinitions_TenantId_Name",
                table: "CustomFieldDefinitions",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeCustomFieldValues_CustomFieldDefinitionId",
                table: "EmployeeCustomFieldValues",
                column: "CustomFieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeCustomFieldValues_EmployeeId",
                table: "EmployeeCustomFieldValues",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeCustomFieldValues_TenantId_EmployeeId_CustomFieldDef~",
                table: "EmployeeCustomFieldValues",
                columns: new[] { "TenantId", "EmployeeId", "CustomFieldDefinitionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeCustomFieldValues");

            migrationBuilder.DropTable(
                name: "CustomFieldDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_IdentityDocuments_TenantId_EmployeeId_DocumentType",
                table: "IdentityDocuments");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "PreviousEmploymentRecords");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "PreviousEmploymentRecords");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Nominees");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Nominees");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "IdentityDocuments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "IdentityDocuments");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "FamilyMembers");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "FamilyMembers");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "EducationRecords");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "EducationRecords");

            migrationBuilder.CreateIndex(
                name: "IX_IdentityDocuments_TenantId_EmployeeId_DocumentType",
                table: "IdentityDocuments",
                columns: new[] { "TenantId", "EmployeeId", "DocumentType" },
                unique: true);
        }
    }
}
