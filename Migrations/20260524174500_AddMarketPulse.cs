using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP_BE.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketPulse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "job_posts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CompanyName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Location = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    SalaryText = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SalaryMinMillionVnd = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    SalaryMaxMillionVnd = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    SourceUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    PostedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ScrapedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_posts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "keyword_trend_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Keyword = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SkillId = table.Column<Guid>(type: "uuid", nullable: true),
                    SnapshotDate = table.Column<DateOnly>(type: "date", nullable: false),
                    WindowDays = table.Column<int>(type: "integer", nullable: false),
                    JobCount = table.Column<int>(type: "integer", nullable: false),
                    TotalMentions = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_keyword_trend_snapshots", x => x.Id);
                    table.CheckConstraint("CK_keyword_trend_snapshots_JobCount", "\"JobCount\" >= 0");
                    table.CheckConstraint("CK_keyword_trend_snapshots_TotalMentions", "\"TotalMentions\" >= 0");
                    table.ForeignKey(
                        name: "FK_keyword_trend_snapshots_skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "job_skill_mentions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobPostId = table.Column<Guid>(type: "uuid", nullable: false),
                    Keyword = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SkillId = table.Column<Guid>(type: "uuid", nullable: true),
                    MentionCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_skill_mentions", x => x.Id);
                    table.CheckConstraint("CK_job_skill_mentions_MentionCount", "\"MentionCount\" >= 0");
                    table.ForeignKey(
                        name: "FK_job_skill_mentions_job_posts_JobPostId",
                        column: x => x.JobPostId,
                        principalTable: "job_posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_job_skill_mentions_skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_job_posts_ScrapedAt",
                table: "job_posts",
                column: "ScrapedAt");

            migrationBuilder.CreateIndex(
                name: "IX_job_posts_Source",
                table: "job_posts",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_job_posts_Source_ExternalId",
                table: "job_posts",
                columns: new[] { "Source", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_job_skill_mentions_JobPostId_Keyword",
                table: "job_skill_mentions",
                columns: new[] { "JobPostId", "Keyword" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_job_skill_mentions_Keyword",
                table: "job_skill_mentions",
                column: "Keyword");

            migrationBuilder.CreateIndex(
                name: "IX_job_skill_mentions_SkillId",
                table: "job_skill_mentions",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_keyword_trend_snapshots_Keyword_SnapshotDate_WindowDays",
                table: "keyword_trend_snapshots",
                columns: new[] { "Keyword", "SnapshotDate", "WindowDays" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_keyword_trend_snapshots_SkillId",
                table: "keyword_trend_snapshots",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_keyword_trend_snapshots_SnapshotDate",
                table: "keyword_trend_snapshots",
                column: "SnapshotDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "job_skill_mentions");

            migrationBuilder.DropTable(
                name: "keyword_trend_snapshots");

            migrationBuilder.DropTable(
                name: "job_posts");
        }
    }
}
