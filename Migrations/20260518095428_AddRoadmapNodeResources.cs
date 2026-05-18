using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP_BE.Migrations
{
    /// <inheritdoc />
    public partial class AddRoadmapNodeResources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "roadmap_node_resources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoadmapNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    LearningResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roadmap_node_resources", x => x.Id);
                    table.CheckConstraint("CK_roadmap_node_resources_OrderIndex", "\"OrderIndex\" >= 1");
                    table.ForeignKey(
                        name: "FK_roadmap_node_resources_learning_resources_LearningResourceId",
                        column: x => x.LearningResourceId,
                        principalTable: "learning_resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_roadmap_node_resources_roadmap_nodes_RoadmapNodeId",
                        column: x => x.RoadmapNodeId,
                        principalTable: "roadmap_nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_roadmap_node_resources_LearningResourceId",
                table: "roadmap_node_resources",
                column: "LearningResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_roadmap_node_resources_RoadmapNodeId_LearningResourceId",
                table: "roadmap_node_resources",
                columns: new[] { "RoadmapNodeId", "LearningResourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_roadmap_node_resources_RoadmapNodeId_OrderIndex",
                table: "roadmap_node_resources",
                columns: new[] { "RoadmapNodeId", "OrderIndex" });

            migrationBuilder.Sql(
                """
                INSERT INTO roadmap_node_resources ("Id", "RoadmapNodeId", "LearningResourceId", "OrderIndex", "CreatedAt")
                SELECT "Id", "Id", "LearningResourceId", 1, "CreatedAt"
                FROM roadmap_nodes
                WHERE "LearningResourceId" IS NOT NULL
                ON CONFLICT DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "roadmap_node_resources");
        }
    }
}
