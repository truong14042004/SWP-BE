using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SWP_BE.Migrations
{
    /// <inheritdoc />
    public partial class AddSkillPrerequisites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "skill_prerequisites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrerequisiteSkillId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skill_prerequisites", x => x.Id);
                    table.CheckConstraint("CK_skill_prerequisites_NoSelfReference", "\"SkillId\" <> \"PrerequisiteSkillId\"");
                    table.ForeignKey(
                        name: "FK_skill_prerequisites_skills_PrerequisiteSkillId",
                        column: x => x.PrerequisiteSkillId,
                        principalTable: "skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_skill_prerequisites_skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "skill_prerequisites",
                columns: new[] { "Id", "CreatedAt", "PrerequisiteSkillId", "SkillId" },
                values: new object[,]
                {
                    { new Guid("55555555-5555-5555-5555-555555555501"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222113"), new Guid("22222222-2222-2222-2222-222222222102") },
                    { new Guid("55555555-5555-5555-5555-555555555502"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222113"), new Guid("22222222-2222-2222-2222-222222222114") },
                    { new Guid("55555555-5555-5555-5555-555555555503"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222101"), new Guid("22222222-2222-2222-2222-222222222103") },
                    { new Guid("55555555-5555-5555-5555-555555555504"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222101"), new Guid("22222222-2222-2222-2222-222222222109") },
                    { new Guid("55555555-5555-5555-5555-555555555505"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222106"), new Guid("22222222-2222-2222-2222-222222222108") },
                    { new Guid("55555555-5555-5555-5555-555555555506"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222105"), new Guid("22222222-2222-2222-2222-222222222112") },
                    { new Guid("55555555-5555-5555-5555-555555555507"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222105"), new Guid("22222222-2222-2222-2222-222222222111") },
                    { new Guid("55555555-5555-5555-5555-555555555508"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222104"), new Guid("22222222-2222-2222-2222-222222222115") },
                    { new Guid("55555555-5555-5555-5555-555555555509"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222117"), new Guid("22222222-2222-2222-2222-222222222116") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_skill_prerequisites_PrerequisiteSkillId",
                table: "skill_prerequisites",
                column: "PrerequisiteSkillId");

            migrationBuilder.CreateIndex(
                name: "IX_skill_prerequisites_SkillId_PrerequisiteSkillId",
                table: "skill_prerequisites",
                columns: new[] { "SkillId", "PrerequisiteSkillId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "skill_prerequisites");
        }
    }
}
