using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP_BE.Migrations
{
    /// <inheritdoc />
    public partial class AddCvParsedTextToStudentProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CvParsedText",
                table: "student_profiles",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CvParsedText",
                table: "student_profiles");
        }
    }
}
