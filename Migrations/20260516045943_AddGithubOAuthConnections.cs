using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP_BE.Migrations
{
    /// <inheritdoc />
    public partial class AddGithubOAuthConnections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "github_connections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    GithubUserId = table.Column<long>(type: "bigint", nullable: true),
                    GithubUsername = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AccessToken = table.Column<string>(type: "text", nullable: false),
                    TokenType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Scope = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ConnectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_github_connections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_github_connections_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "github_oauth_states",
                columns: table => new
                {
                    State = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReturnUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_github_oauth_states", x => x.State);
                    table.ForeignKey(
                        name: "FK_github_oauth_states_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_github_connections_GithubUsername",
                table: "github_connections",
                column: "GithubUsername");

            migrationBuilder.CreateIndex(
                name: "IX_github_connections_UserId",
                table: "github_connections",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_github_oauth_states_ExpiresAt",
                table: "github_oauth_states",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_github_oauth_states_UserId",
                table: "github_oauth_states",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "github_connections");

            migrationBuilder.DropTable(
                name: "github_oauth_states");
        }
    }
}
