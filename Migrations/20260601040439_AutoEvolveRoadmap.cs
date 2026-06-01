using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP_BE.Migrations
{
    /// <inheritdoc />
    public partial class AutoEvolveRoadmap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoleSkillUpdateProposals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CareerRoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillName = table.Column<string>(type: "text", nullable: false),
                    ActionType = table.Column<string>(type: "text", nullable: false),
                    CurrentPriority = table.Column<int>(type: "integer", nullable: true),
                    ProposedPriority = table.Column<int>(type: "integer", nullable: true),
                    CurrentWeight = table.Column<decimal>(type: "numeric", nullable: true),
                    ProposedWeight = table.Column<decimal>(type: "numeric", nullable: true),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RespondedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleSkillUpdateProposals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleSkillUpdateProposals_career_roles_CareerRoleId",
                        column: x => x.CareerRoleId,
                        principalTable: "career_roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoleSkillUpdateProposals_skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoleSkillUpdateProposals_CareerRoleId",
                table: "RoleSkillUpdateProposals",
                column: "CareerRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleSkillUpdateProposals_SkillId",
                table: "RoleSkillUpdateProposals",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleSkillUpdateProposals_Status",
                table: "RoleSkillUpdateProposals",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleSkillUpdateProposals");
        }
    }
}
