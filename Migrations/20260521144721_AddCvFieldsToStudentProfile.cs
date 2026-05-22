using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP_BE.Migrations
{
    /// <inheritdoc />
    public partial class AddCvFieldsToStudentProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CvName",
                table: "student_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CvUrl",
                table: "student_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ContextJson",
                table: "mentor_sessions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CvName",
                table: "student_profiles");

            migrationBuilder.DropColumn(
                name: "CvUrl",
                table: "student_profiles");

            migrationBuilder.AlterColumn<string>(
                name: "ContextJson",
                table: "mentor_sessions",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
