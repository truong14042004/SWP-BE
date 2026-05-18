using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP_BE.Migrations
{
    /// <inheritdoc />
    public partial class AddRoadmapNodeHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Level",
                table: "roadmap_nodes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentNodeId",
                table: "roadmap_nodes",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_roadmap_nodes_ParentNodeId",
                table: "roadmap_nodes",
                column: "ParentNodeId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_roadmap_nodes_Level",
                table: "roadmap_nodes",
                sql: "\"Level\" >= 0 AND \"Level\" <= 8");

            migrationBuilder.AddForeignKey(
                name: "FK_roadmap_nodes_roadmap_nodes_ParentNodeId",
                table: "roadmap_nodes",
                column: "ParentNodeId",
                principalTable: "roadmap_nodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_roadmap_nodes_roadmap_nodes_ParentNodeId",
                table: "roadmap_nodes");

            migrationBuilder.DropIndex(
                name: "IX_roadmap_nodes_ParentNodeId",
                table: "roadmap_nodes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_roadmap_nodes_Level",
                table: "roadmap_nodes");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "roadmap_nodes");

            migrationBuilder.DropColumn(
                name: "ParentNodeId",
                table: "roadmap_nodes");
        }
    }
}
