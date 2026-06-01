using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP_BE.Migrations
{
    /// <inheritdoc />
    public partial class LatentTalentProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "student_talent_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnalyzedRepoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    LogicalThinkingScore = table.Column<int>(type: "integer", nullable: false),
                    SystemArchitectureScore = table.Column<int>(type: "integer", nullable: false),
                    VisualDesignScore = table.Column<int>(type: "integer", nullable: false),
                    AiFeedback = table.Column<string>(type: "text", nullable: false),
                    AnalyzedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_talent_profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_student_talent_profiles_users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_student_talent_profiles_StudentId",
                table: "student_talent_profiles",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "student_talent_profiles");
        }
    }
}
