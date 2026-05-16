using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SWP_BE.Migrations
{
    /// <inheritdoc />
    public partial class SeedCareerRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "career_roles",
                columns: new[] { "Id", "CreatedAt", "Description", "IsActive", "Level", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111101"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Builds server-side APIs, databases, authentication, and backend services.", true, "Fresher", "Backend Developer", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("11111111-1111-1111-1111-111111111102"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Builds user interfaces, client-side application logic, and responsive web experiences.", true, "Fresher", "Frontend Developer", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("11111111-1111-1111-1111-111111111103"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Builds both frontend interfaces and backend APIs for complete web applications.", true, "Fresher", "Fullstack Developer", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("11111111-1111-1111-1111-111111111104"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Builds mobile applications and integrates them with backend services.", true, "Fresher", "Mobile Developer", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("11111111-1111-1111-1111-111111111105"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Works on CI/CD, deployment automation, infrastructure, monitoring, and reliability.", true, "Fresher", "DevOps Engineer", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("11111111-1111-1111-1111-111111111106"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Builds data pipelines, data storage, processing jobs, and analytics infrastructure.", true, "Fresher", "Data Engineer", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("11111111-1111-1111-1111-111111111107"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Designs automated tests, test frameworks, and quality assurance workflows.", true, "Fresher", "QA Automation Engineer", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("11111111-1111-1111-1111-111111111108"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Builds and operates cloud infrastructure, deployment environments, and managed services.", true, "Fresher", "Cloud Engineer", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("11111111-1111-1111-1111-111111111109"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Builds software applications that integrate AI APIs, prompts, agents, and AI-assisted workflows.", true, "Fresher", "AI Application Developer", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "career_roles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"));

            migrationBuilder.DeleteData(
                table: "career_roles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"));

            migrationBuilder.DeleteData(
                table: "career_roles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"));

            migrationBuilder.DeleteData(
                table: "career_roles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"));

            migrationBuilder.DeleteData(
                table: "career_roles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"));

            migrationBuilder.DeleteData(
                table: "career_roles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111106"));

            migrationBuilder.DeleteData(
                table: "career_roles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111107"));

            migrationBuilder.DeleteData(
                table: "career_roles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111108"));

            migrationBuilder.DeleteData(
                table: "career_roles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111109"));
        }
    }
}
