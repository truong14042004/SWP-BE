using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP_BE.Migrations
{
    /// <inheritdoc />
    public partial class ChangeMentorSessionContextJsonToText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Empty: model-snapshot mismatch caused EF to skip generating an AlterColumn here.
            // The actual jsonb -> text conversion is performed by the AlterMentorSessionContextJsonToText migration.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
