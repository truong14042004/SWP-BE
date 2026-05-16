using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP_BE.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRoleConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE users
                SET "Role" = 'Student'
                WHERE "Role" = 'User' OR "Role" IS NULL OR "Role" = '';
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_users_Role",
                table: "users",
                sql: "\"Role\" IN ('Student', 'Admin', 'AcademicCounselor', 'IndustryMentor')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_users_Role",
                table: "users");
        }
    }
}
