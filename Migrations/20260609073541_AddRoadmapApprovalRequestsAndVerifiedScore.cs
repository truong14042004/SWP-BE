using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP_BE.Migrations
{
    /// <inheritdoc />
    public partial class AddRoadmapApprovalRequestsAndVerifiedScore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "VerifiedMatchScore",
                table: "skill_gap_reports",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "roadmap_approval_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CounselorId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Pending"),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    RejectionReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    MaterializedRoadmapId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roadmap_approval_requests", x => x.Id);
                    table.CheckConstraint("CK_roadmap_approval_requests_Status", "\"Status\" IN ('Pending','Approved','Rejected')");
                    table.ForeignKey(
                        name: "FK_roadmap_approval_requests_roadmaps_MaterializedRoadmapId",
                        column: x => x.MaterializedRoadmapId,
                        principalTable: "roadmaps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_roadmap_approval_requests_users_CounselorId",
                        column: x => x.CounselorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_roadmap_approval_requests_users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_skill_gap_reports_VerifiedMatchScore",
                table: "skill_gap_reports",
                sql: "\"VerifiedMatchScore\" IS NULL OR (\"VerifiedMatchScore\" >= 0 AND \"VerifiedMatchScore\" <= 100)");

            migrationBuilder.CreateIndex(
                name: "IX_roadmap_approval_requests_CounselorId_Status",
                table: "roadmap_approval_requests",
                columns: new[] { "CounselorId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_roadmap_approval_requests_MaterializedRoadmapId",
                table: "roadmap_approval_requests",
                column: "MaterializedRoadmapId");

            migrationBuilder.CreateIndex(
                name: "IX_roadmap_approval_requests_StudentId_Status",
                table: "roadmap_approval_requests",
                columns: new[] { "StudentId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "roadmap_approval_requests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_skill_gap_reports_VerifiedMatchScore",
                table: "skill_gap_reports");

            migrationBuilder.DropColumn(
                name: "VerifiedMatchScore",
                table: "skill_gap_reports");
        }
    }
}
