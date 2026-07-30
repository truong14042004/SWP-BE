using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP_BE.Migrations
{
    /// <inheritdoc />
    public partial class AllowCareerRoleSameNameDifferentLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_career_roles_Name",
                table: "career_roles");

            migrationBuilder.CreateIndex(
                name: "IX_career_roles_Name_Level",
                table: "career_roles",
                columns: new[] { "Name", "Level" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_career_roles_Name_Level",
                table: "career_roles");

            migrationBuilder.CreateIndex(
                name: "IX_career_roles_Name",
                table: "career_roles",
                column: "Name",
                unique: true);
        }
    }
}
