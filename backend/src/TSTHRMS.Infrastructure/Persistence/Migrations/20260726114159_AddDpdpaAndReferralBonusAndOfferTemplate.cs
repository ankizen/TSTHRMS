using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TSTHRMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDpdpaAndReferralBonusAndOfferTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OfferLetterTemplate",
                table: "Tenants",
                type: "text",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "ReferralBonusAmount",
                table: "Tenants",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RejectedCandidateRetentionDays",
                table: "Tenants",
                type: "int",
                nullable: false,
                defaultValue: 180);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AnonymizedAt",
                table: "Candidates",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAnonymized",
                table: "Candidates",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "ReferralBonusAmount",
                table: "Candidates",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReferralBonusPaidAt",
                table: "Candidates",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferralBonusStatus",
                table: "Candidates",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "NotApplicable")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CandidateDataDeletionRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CandidateId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    RequestedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HrDecisionNotes = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DecidedByUserId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    DecidedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateDataDeletionRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateDataDeletionRequests_Candidates_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "Candidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateDataDeletionRequests_CandidateId",
                table: "CandidateDataDeletionRequests",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateDataDeletionRequests_TenantId_CandidateId",
                table: "CandidateDataDeletionRequests",
                columns: new[] { "TenantId", "CandidateId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CandidateDataDeletionRequests");

            migrationBuilder.DropColumn(
                name: "OfferLetterTemplate",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ReferralBonusAmount",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "RejectedCandidateRetentionDays",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "AnonymizedAt",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "IsAnonymized",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "ReferralBonusAmount",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "ReferralBonusPaidAt",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "ReferralBonusStatus",
                table: "Candidates");
        }
    }
}
