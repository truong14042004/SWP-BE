using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP_BE.Migrations
{
    /// <inheritdoc />
    public partial class AddAiReviewSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AiSummaryId",
                table: "roadmap_node_review_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ai_review_summaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    GeneratedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    EvidenceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EvidenceUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TokensUsed = table.Column<int>(type: "integer", nullable: true),
                    TechStackJson = table.Column<string>(type: "jsonb", nullable: false),
                    StrengthsJson = table.Column<string>(type: "jsonb", nullable: false),
                    WeaknessesJson = table.Column<string>(type: "jsonb", nullable: false),
                    SuggestedQuestionsJson = table.Column<string>(type: "jsonb", nullable: false),
                    SkillMappingJson = table.Column<string>(type: "jsonb", nullable: false),
                    OverallSummary = table.Column<string>(type: "text", nullable: true),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_review_summaries", x => x.Id);
                    table.CheckConstraint("CK_ai_review_summaries_TokensUsed", "\"TokensUsed\" IS NULL OR \"TokensUsed\" >= 0");
                    table.ForeignKey(
                        name: "FK_ai_review_summaries_users_GeneratedByUserId",
                        column: x => x.GeneratedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_roadmap_node_review_requests_AiSummaryId",
                table: "roadmap_node_review_requests",
                column: "AiSummaryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ai_review_summaries_GeneratedAt",
                table: "ai_review_summaries",
                column: "GeneratedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ai_review_summaries_GeneratedByUserId",
                table: "ai_review_summaries",
                column: "GeneratedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_review_summaries_ReviewRequestId",
                table: "ai_review_summaries",
                column: "ReviewRequestId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_roadmap_node_review_requests_ai_review_summaries_AiSummaryId",
                table: "roadmap_node_review_requests",
                column: "AiSummaryId",
                principalTable: "ai_review_summaries",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_roadmap_node_review_requests_ai_review_summaries_AiSummaryId",
                table: "roadmap_node_review_requests");

            migrationBuilder.DropTable(
                name: "ai_review_summaries");

            migrationBuilder.DropIndex(
                name: "IX_roadmap_node_review_requests_AiSummaryId",
                table: "roadmap_node_review_requests");

            migrationBuilder.DropColumn(
                name: "AiSummaryId",
                table: "roadmap_node_review_requests");
        }
    }
}
