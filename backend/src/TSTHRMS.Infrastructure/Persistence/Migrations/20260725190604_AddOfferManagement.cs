using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TSTHRMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOfferManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Offers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ApplicationId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Token = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SentAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    RespondedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    DeclineReason = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApprovedByUserId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Offers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Offers_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "OfferVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    OfferId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Designation = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateOfJoining = table.Column<DateOnly>(type: "date", nullable: false),
                    AnnualCtc = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    FixedComponent = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    VariableComponent = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    JoiningBonus = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    OfferLetterText = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RevisionReason = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedByUserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfferVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OfferVersions_Offers_OfferId",
                        column: x => x.OfferId,
                        principalTable: "Offers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_ApplicationId",
                table: "Offers",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_TenantId_ApplicationId",
                table: "Offers",
                columns: new[] { "TenantId", "ApplicationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Offers_Token",
                table: "Offers",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfferVersions_OfferId",
                table: "OfferVersions",
                column: "OfferId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferVersions_TenantId_OfferId_VersionNumber",
                table: "OfferVersions",
                columns: new[] { "TenantId", "OfferId", "VersionNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OfferVersions");

            migrationBuilder.DropTable(
                name: "Offers");
        }
    }
}
