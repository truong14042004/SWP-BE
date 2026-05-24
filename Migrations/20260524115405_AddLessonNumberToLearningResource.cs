using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP_BE.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonNumberToLearningResource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LessonNumber",
                table: "learning_resources",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333101"),
                column: "LessonNumber",
                value: 1);

            migrationBuilder.UpdateData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333102"),
                column: "LessonNumber",
                value: 1);

            migrationBuilder.UpdateData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333103"),
                column: "LessonNumber",
                value: 1);

            migrationBuilder.UpdateData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333104"),
                column: "LessonNumber",
                value: 1);

            migrationBuilder.UpdateData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333105"),
                column: "LessonNumber",
                value: 1);

            migrationBuilder.UpdateData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333106"),
                column: "LessonNumber",
                value: 1);

            migrationBuilder.UpdateData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333107"),
                column: "LessonNumber",
                value: 1);

            migrationBuilder.UpdateData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333108"),
                column: "LessonNumber",
                value: 1);

            migrationBuilder.UpdateData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333109"),
                column: "LessonNumber",
                value: 1);

            migrationBuilder.UpdateData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333110"),
                column: "LessonNumber",
                value: 1);

            migrationBuilder.UpdateData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333111"),
                column: "LessonNumber",
                value: 1);

            migrationBuilder.UpdateData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333112"),
                column: "LessonNumber",
                value: 1);

            migrationBuilder.UpdateData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333113"),
                column: "LessonNumber",
                value: 1);

            migrationBuilder.UpdateData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333114"),
                column: "LessonNumber",
                value: 1);

            migrationBuilder.UpdateData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333115"),
                column: "LessonNumber",
                value: 1);

            migrationBuilder.UpdateData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333116"),
                column: "LessonNumber",
                value: 1);

            migrationBuilder.UpdateData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333117"),
                column: "LessonNumber",
                value: 1);

            migrationBuilder.UpdateData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333118"),
                column: "LessonNumber",
                value: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LessonNumber",
                table: "learning_resources");
        }
    }
}
