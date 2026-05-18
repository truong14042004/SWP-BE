using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP_BE.Migrations
{
    /// <inheritdoc />
    public partial class AddCounselorAssignmentsAndFeedbackImprovements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Comment",
                table: "counselor_feedbacks",
                newName: "FeedbackText");

            migrationBuilder.AddColumn<string>(
                name: "PrivateNotes",
                table: "counselor_feedbacks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Rating",
                table: "counselor_feedbacks",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Recommendations",
                table: "counselor_feedbacks",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "counselor_assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CounselorId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedByAdminId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_counselor_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_counselor_assignments_users_AssignedByAdminId",
                        column: x => x.AssignedByAdminId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_counselor_assignments_users_CounselorId",
                        column: x => x.CounselorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_counselor_assignments_users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_counselor_feedbacks_Rating",
                table: "counselor_feedbacks",
                sql: "\"Rating\" IS NULL OR (\"Rating\" >= 1 AND \"Rating\" <= 5)");

            migrationBuilder.CreateIndex(
                name: "IX_counselor_assignments_AssignedByAdminId",
                table: "counselor_assignments",
                column: "AssignedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_counselor_assignments_CounselorId_StudentId",
                table: "counselor_assignments",
                columns: new[] { "CounselorId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_counselor_assignments_StudentId",
                table: "counselor_assignments",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "counselor_assignments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_counselor_feedbacks_Rating",
                table: "counselor_feedbacks");

            migrationBuilder.DropColumn(
                name: "PrivateNotes",
                table: "counselor_feedbacks");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "counselor_feedbacks");

            migrationBuilder.DropColumn(
                name: "Recommendations",
                table: "counselor_feedbacks");

            migrationBuilder.RenameColumn(
                name: "FeedbackText",
                table: "counselor_feedbacks",
                newName: "Comment");
        }
    }
}
