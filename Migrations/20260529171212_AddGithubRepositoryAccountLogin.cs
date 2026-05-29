using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP_BE.Migrations
{
    /// <inheritdoc />
    public partial class AddGithubRepositoryAccountLogin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GithubAccountLogin",
                table: "github_repositories",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE github_repositories AS r
                SET "GithubAccountLogin" = c."GithubUsername"
                FROM github_connections AS c
                WHERE c."UserId" = r."UserId"
                  AND r."GithubAccountLogin" IS NULL;
                """);

            migrationBuilder.Sql(
                """
                UPDATE github_repositories AS r
                SET "GithubAccountLogin" = p."GithubUsername"
                FROM student_profiles AS p
                WHERE p."UserId" = r."UserId"
                  AND p."GithubUsername" IS NOT NULL
                  AND r."GithubAccountLogin" IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_github_repositories_UserId_GithubAccountLogin",
                table: "github_repositories",
                columns: new[] { "UserId", "GithubAccountLogin" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_github_repositories_UserId_GithubAccountLogin",
                table: "github_repositories");

            migrationBuilder.DropColumn(
                name: "GithubAccountLogin",
                table: "github_repositories");
        }
    }
}
