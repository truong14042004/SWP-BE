using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP_BE.Migrations
{
    /// <inheritdoc />
    public partial class AddVerificationStatusToUserSkill : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VerificationStatus",
                table: "user_skills",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "SelfDeclared");

            migrationBuilder.AddCheckConstraint(
                name: "CK_user_skills_VerificationStatus",
                table: "user_skills",
                sql: "\"VerificationStatus\" IN ('SelfDeclared','PendingVerification','Verified','Unverified')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_user_skills_VerificationStatus",
                table: "user_skills");

            migrationBuilder.DropColumn(
                name: "VerificationStatus",
                table: "user_skills");
        }
    }
}
