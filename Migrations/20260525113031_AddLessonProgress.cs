using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP_BE.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "lesson_progresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoadmapNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    LearningResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lesson_progresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lesson_progresses_learning_resources_LearningResourceId",
                        column: x => x.LearningResourceId,
                        principalTable: "learning_resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_lesson_progresses_roadmap_nodes_RoadmapNodeId",
                        column: x => x.RoadmapNodeId,
                        principalTable: "roadmap_nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_lesson_progresses_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_lesson_progresses_LearningResourceId",
                table: "lesson_progresses",
                column: "LearningResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_lesson_progresses_RoadmapNodeId",
                table: "lesson_progresses",
                column: "RoadmapNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_lesson_progresses_UserId_RoadmapNodeId",
                table: "lesson_progresses",
                columns: new[] { "UserId", "RoadmapNodeId" });

            migrationBuilder.CreateIndex(
                name: "IX_lesson_progresses_UserId_RoadmapNodeId_LearningResourceId",
                table: "lesson_progresses",
                columns: new[] { "UserId", "RoadmapNodeId", "LearningResourceId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lesson_progresses");
        }
    }
}
