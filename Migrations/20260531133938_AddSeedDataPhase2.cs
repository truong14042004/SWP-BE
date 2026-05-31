using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SWP_BE.Migrations
{
    /// <inheritdoc />
    public partial class AddSeedDataPhase2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "career_roles",
                columns: new[] { "Id", "CreatedAt", "Description", "IsActive", "Level", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111113"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Designs, secures, tunes, and operates relational and NoSQL databases for reliability and performance.", true, "Fresher", "Database Administrator", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("11111111-1111-1111-1111-111111111114"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Designs scalable system architectures, integrates services, and aligns technical decisions with business needs.", true, "Fresher", "Solutions Architect", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "role_skill_requirements",
                columns: new[] { "Id", "CareerRoleId", "CreatedAt", "Priority", "RequiredLevel", "SkillId", "UpdatedAt", "Weight" },
                values: new object[,]
                {
                    { new Guid("44444444-4444-4444-4444-444444444155"), new Guid("11111111-1111-1111-1111-111111111101"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1, "Beginner", new Guid("22222222-2222-2222-2222-222222222119"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.0m },
                    { new Guid("44444444-4444-4444-4444-444444444156"), new Guid("11111111-1111-1111-1111-111111111102"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1, "Beginner", new Guid("22222222-2222-2222-2222-222222222119"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.0m },
                    { new Guid("44444444-4444-4444-4444-444444444157"), new Guid("11111111-1111-1111-1111-111111111103"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1, "Beginner", new Guid("22222222-2222-2222-2222-222222222119"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.0m },
                    { new Guid("44444444-4444-4444-4444-444444444158"), new Guid("11111111-1111-1111-1111-111111111105"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1, "Beginner", new Guid("22222222-2222-2222-2222-222222222119"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.0m },
                    { new Guid("44444444-4444-4444-4444-444444444159"), new Guid("11111111-1111-1111-1111-111111111104"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 2, "Beginner", new Guid("22222222-2222-2222-2222-222222222119"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.9m },
                    { new Guid("44444444-4444-4444-4444-444444444160"), new Guid("11111111-1111-1111-1111-111111111106"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 2, "Beginner", new Guid("22222222-2222-2222-2222-222222222119"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.9m },
                    { new Guid("44444444-4444-4444-4444-444444444161"), new Guid("11111111-1111-1111-1111-111111111102"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1, "Intermediate", new Guid("22222222-2222-2222-2222-222222222120"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.2m },
                    { new Guid("44444444-4444-4444-4444-444444444162"), new Guid("11111111-1111-1111-1111-111111111103"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1, "Beginner", new Guid("22222222-2222-2222-2222-222222222120"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.1m },
                    { new Guid("44444444-4444-4444-4444-444444444163"), new Guid("11111111-1111-1111-1111-111111111102"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1, "Intermediate", new Guid("22222222-2222-2222-2222-222222222121"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.3m },
                    { new Guid("44444444-4444-4444-4444-444444444164"), new Guid("11111111-1111-1111-1111-111111111103"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1, "Intermediate", new Guid("22222222-2222-2222-2222-222222222121"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.2m },
                    { new Guid("44444444-4444-4444-4444-444444444165"), new Guid("11111111-1111-1111-1111-111111111101"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 2, "Beginner", new Guid("22222222-2222-2222-2222-222222222122"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.1m },
                    { new Guid("44444444-4444-4444-4444-444444444166"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 2, "Intermediate", new Guid("22222222-2222-2222-2222-222222222122"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.2m },
                    { new Guid("44444444-4444-4444-4444-444444444167"), new Guid("11111111-1111-1111-1111-111111111101"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 3, "Beginner", new Guid("22222222-2222-2222-2222-222222222123"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.0m },
                    { new Guid("44444444-4444-4444-4444-444444444168"), new Guid("11111111-1111-1111-1111-111111111103"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 3, "Beginner", new Guid("22222222-2222-2222-2222-222222222123"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.9m },
                    { new Guid("44444444-4444-4444-4444-444444444169"), new Guid("11111111-1111-1111-1111-111111111106"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 2, "Beginner", new Guid("22222222-2222-2222-2222-222222222125"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.1m },
                    { new Guid("44444444-4444-4444-4444-444444444170"), new Guid("11111111-1111-1111-1111-111111111101"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 4, "Beginner", new Guid("22222222-2222-2222-2222-222222222125"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.8m },
                    { new Guid("44444444-4444-4444-4444-444444444171"), new Guid("11111111-1111-1111-1111-111111111103"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 3, "Beginner", new Guid("22222222-2222-2222-2222-222222222126"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.9m },
                    { new Guid("44444444-4444-4444-4444-444444444172"), new Guid("11111111-1111-1111-1111-111111111101"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 4, "Beginner", new Guid("22222222-2222-2222-2222-222222222127"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.8m }
                });

            migrationBuilder.InsertData(
                table: "skill_prerequisites",
                columns: new[] { "Id", "CreatedAt", "PrerequisiteSkillId", "SkillId" },
                values: new object[,]
                {
                    { new Guid("55555555-5555-5555-5555-555555555524"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222122"), new Guid("22222222-2222-2222-2222-222222222123") },
                    { new Guid("55555555-5555-5555-5555-555555555525"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222123"), new Guid("22222222-2222-2222-2222-222222222127") },
                    { new Guid("55555555-5555-5555-5555-555555555526"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222122"), new Guid("22222222-2222-2222-2222-222222222130") },
                    { new Guid("55555555-5555-5555-5555-555555555527"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222121"), new Guid("22222222-2222-2222-2222-222222222109") },
                    { new Guid("55555555-5555-5555-5555-555555555528"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222128"), new Guid("22222222-2222-2222-2222-222222222124") },
                    { new Guid("55555555-5555-5555-5555-555555555529"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222128"), new Guid("22222222-2222-2222-2222-222222222129") }
                });

            migrationBuilder.InsertData(
                table: "skills",
                columns: new[] { "Id", "Category", "CreatedAt", "Description", "IsActive", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("22222222-2222-2222-2222-222222222131"), "Backend", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Caching strategies, session stores, and rate limiting with Redis.", true, "Redis & Caching", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("22222222-2222-2222-2222-222222222132"), "Backend", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Asynchronous communication with Kafka, RabbitMQ, and event-driven patterns.", true, "Message Queues", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("22222222-2222-2222-2222-222222222133"), "DevOps", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Provisioning and managing infrastructure declaratively with Terraform or Pulumi.", true, "Infrastructure as Code", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("22222222-2222-2222-2222-222222222134"), "Data", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Building charts, dashboards, and visual reports to communicate data insights.", true, "Data Visualization", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "role_skill_requirements",
                columns: new[] { "Id", "CareerRoleId", "CreatedAt", "Priority", "RequiredLevel", "SkillId", "UpdatedAt", "Weight" },
                values: new object[,]
                {
                    { new Guid("44444444-4444-4444-4444-444444444173"), new Guid("11111111-1111-1111-1111-111111111113"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1, "Intermediate", new Guid("22222222-2222-2222-2222-222222222113"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.5m },
                    { new Guid("44444444-4444-4444-4444-444444444174"), new Guid("11111111-1111-1111-1111-111111111113"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1, "Intermediate", new Guid("22222222-2222-2222-2222-222222222125"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.4m },
                    { new Guid("44444444-4444-4444-4444-444444444175"), new Guid("11111111-1111-1111-1111-111111111113"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 2, "Intermediate", new Guid("22222222-2222-2222-2222-222222222102"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.2m },
                    { new Guid("44444444-4444-4444-4444-444444444176"), new Guid("11111111-1111-1111-1111-111111111113"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 2, "Beginner", new Guid("22222222-2222-2222-2222-222222222128"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.1m },
                    { new Guid("44444444-4444-4444-4444-444444444177"), new Guid("11111111-1111-1111-1111-111111111113"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 3, "Beginner", new Guid("22222222-2222-2222-2222-222222222129"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.9m },
                    { new Guid("44444444-4444-4444-4444-444444444178"), new Guid("11111111-1111-1111-1111-111111111114"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1, "Intermediate", new Guid("22222222-2222-2222-2222-222222222123"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.5m },
                    { new Guid("44444444-4444-4444-4444-444444444179"), new Guid("11111111-1111-1111-1111-111111111114"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1, "Intermediate", new Guid("22222222-2222-2222-2222-222222222127"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.3m },
                    { new Guid("44444444-4444-4444-4444-444444444180"), new Guid("11111111-1111-1111-1111-111111111114"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 2, "Beginner", new Guid("22222222-2222-2222-2222-222222222112"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.2m },
                    { new Guid("44444444-4444-4444-4444-444444444181"), new Guid("11111111-1111-1111-1111-111111111114"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 2, "Beginner", new Guid("22222222-2222-2222-2222-222222222101"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.1m },
                    { new Guid("44444444-4444-4444-4444-444444444182"), new Guid("11111111-1111-1111-1111-111111111114"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 3, "Beginner", new Guid("22222222-2222-2222-2222-222222222102"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.0m }
                });

            migrationBuilder.InsertData(
                table: "skill_prerequisites",
                columns: new[] { "Id", "CreatedAt", "PrerequisiteSkillId", "SkillId" },
                values: new object[,]
                {
                    { new Guid("55555555-5555-5555-5555-555555555530"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222102"), new Guid("22222222-2222-2222-2222-222222222131") },
                    { new Guid("55555555-5555-5555-5555-555555555531"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222113"), new Guid("22222222-2222-2222-2222-222222222134") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444155"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444156"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444157"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444158"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444159"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444160"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444161"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444162"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444163"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444164"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444165"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444166"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444167"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444168"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444169"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444170"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444171"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444172"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444173"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444174"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444175"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444176"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444177"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444178"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444179"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444180"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444181"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444182"));

            migrationBuilder.DeleteData(
                table: "skill_prerequisites",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555524"));

            migrationBuilder.DeleteData(
                table: "skill_prerequisites",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555525"));

            migrationBuilder.DeleteData(
                table: "skill_prerequisites",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555526"));

            migrationBuilder.DeleteData(
                table: "skill_prerequisites",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555527"));

            migrationBuilder.DeleteData(
                table: "skill_prerequisites",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555528"));

            migrationBuilder.DeleteData(
                table: "skill_prerequisites",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555529"));

            migrationBuilder.DeleteData(
                table: "skill_prerequisites",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555530"));

            migrationBuilder.DeleteData(
                table: "skill_prerequisites",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555531"));

            migrationBuilder.DeleteData(
                table: "skills",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222132"));

            migrationBuilder.DeleteData(
                table: "skills",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222133"));

            migrationBuilder.DeleteData(
                table: "career_roles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111113"));

            migrationBuilder.DeleteData(
                table: "career_roles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111114"));

            migrationBuilder.DeleteData(
                table: "skills",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222131"));

            migrationBuilder.DeleteData(
                table: "skills",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222134"));
        }
    }
}
