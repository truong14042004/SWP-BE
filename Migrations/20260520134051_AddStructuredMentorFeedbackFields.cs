using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP_BE.Migrations
{
    /// <inheritdoc />
    public partial class AddStructuredMentorFeedbackFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JobReadinessLevel",
                table: "mentor_feedbacks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PortfolioQualityFeedback",
                table: "mentor_feedbacks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProjectQualityFeedback",
                table: "mentor_feedbacks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Recommendations",
                table: "mentor_feedbacks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TechnicalSkillsAssessment",
                table: "mentor_feedbacks",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JobReadinessLevel",
                table: "mentor_feedbacks");

            migrationBuilder.DropColumn(
                name: "PortfolioQualityFeedback",
                table: "mentor_feedbacks");

            migrationBuilder.DropColumn(
                name: "ProjectQualityFeedback",
                table: "mentor_feedbacks");

            migrationBuilder.DropColumn(
                name: "Recommendations",
                table: "mentor_feedbacks");

            migrationBuilder.DropColumn(
                name: "TechnicalSkillsAssessment",
                table: "mentor_feedbacks");
        }
    }
}
