using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SWP_BE.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "career_roles",
                columns: new[] { "Id", "CreatedAt", "Description", "IsActive", "Level", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111110"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Secures applications and infrastructure through authentication, access control, hardening, and vulnerability management.", true, "Fresher", "Security Engineer", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("11111111-1111-1111-1111-111111111111"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Builds, trains, and deploys machine learning models and data-driven prediction services.", true, "Fresher", "Machine Learning Engineer", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("11111111-1111-1111-1111-111111111112"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Ensures system reliability, observability, scaling, and incident response for production services.", true, "Fresher", "Site Reliability Engineer", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "skills",
                columns: new[] { "Id", "Category", "CreatedAt", "Description", "IsActive", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("22222222-2222-2222-2222-222222222119"), "Engineering", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Tracking changes with branches, merges, pull requests, and collaborative workflows.", true, "Git & Version Control", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("22222222-2222-2222-2222-222222222120"), "Frontend", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Structuring web content and styling layouts, typography, and visual presentation.", true, "HTML & CSS", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("22222222-2222-2222-2222-222222222121"), "Frontend", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Core language features, DOM manipulation, async patterns, and ES modules.", true, "JavaScript Fundamentals", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("22222222-2222-2222-2222-222222222122"), "Engineering", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Core data structures, complexity analysis, and problem-solving techniques.", true, "Data Structures & Algorithms", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("22222222-2222-2222-2222-222222222123"), "Backend", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Designing scalable systems with caching, load balancing, and data partitioning.", true, "System Design", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("22222222-2222-2222-2222-222222222124"), "DevOps", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Orchestrating containers with deployments, services, scaling, and config management.", true, "Kubernetes", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("22222222-2222-2222-2222-222222222125"), "Data", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Modeling and querying document, key-value, and wide-column data stores.", true, "NoSQL Databases", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("22222222-2222-2222-2222-222222222126"), "Backend", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Building schema-driven APIs with queries, mutations, and resolvers.", true, "GraphQL", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("22222222-2222-2222-2222-222222222127"), "Backend", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Decomposing systems into independently deployable services with messaging.", true, "Microservices", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("22222222-2222-2222-2222-222222222128"), "DevOps", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Operating Linux systems, shell scripting, permissions, and process management.", true, "Linux & Shell", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("22222222-2222-2222-2222-222222222129"), "DevOps", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Instrumenting metrics, logs, traces, and alerts for production systems.", true, "Monitoring & Observability", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("22222222-2222-2222-2222-222222222130"), "AI", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Supervised learning, model evaluation, feature engineering, and training workflows.", true, "Machine Learning Fundamentals", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "role_skill_requirements",
                columns: new[] { "Id", "CareerRoleId", "CreatedAt", "Priority", "RequiredLevel", "SkillId", "UpdatedAt", "Weight" },
                values: new object[,]
                {
                    { new Guid("44444444-4444-4444-4444-444444444141"), new Guid("11111111-1111-1111-1111-111111111110"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1, "Intermediate", new Guid("22222222-2222-2222-2222-222222222103"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.5m },
                    { new Guid("44444444-4444-4444-4444-444444444142"), new Guid("11111111-1111-1111-1111-111111111110"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 2, "Intermediate", new Guid("22222222-2222-2222-2222-222222222128"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.2m },
                    { new Guid("44444444-4444-4444-4444-444444444143"), new Guid("11111111-1111-1111-1111-111111111110"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 2, "Beginner", new Guid("22222222-2222-2222-2222-222222222101"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.1m },
                    { new Guid("44444444-4444-4444-4444-444444444144"), new Guid("11111111-1111-1111-1111-111111111110"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 3, "Beginner", new Guid("22222222-2222-2222-2222-222222222104"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.0m },
                    { new Guid("44444444-4444-4444-4444-444444444145"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1, "Intermediate", new Guid("22222222-2222-2222-2222-222222222130"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.5m },
                    { new Guid("44444444-4444-4444-4444-444444444146"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1, "Intermediate", new Guid("22222222-2222-2222-2222-222222222113"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.3m },
                    { new Guid("44444444-4444-4444-4444-444444444147"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 2, "Beginner", new Guid("22222222-2222-2222-2222-222222222114"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.2m },
                    { new Guid("44444444-4444-4444-4444-444444444148"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 2, "Beginner", new Guid("22222222-2222-2222-2222-222222222116"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.1m },
                    { new Guid("44444444-4444-4444-4444-444444444149"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 3, "Beginner", new Guid("22222222-2222-2222-2222-222222222117"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.9m },
                    { new Guid("44444444-4444-4444-4444-444444444150"), new Guid("11111111-1111-1111-1111-111111111112"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1, "Intermediate", new Guid("22222222-2222-2222-2222-222222222128"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.4m },
                    { new Guid("44444444-4444-4444-4444-444444444151"), new Guid("11111111-1111-1111-1111-111111111112"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1, "Intermediate", new Guid("22222222-2222-2222-2222-222222222129"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.4m },
                    { new Guid("44444444-4444-4444-4444-444444444152"), new Guid("11111111-1111-1111-1111-111111111112"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 2, "Beginner", new Guid("22222222-2222-2222-2222-222222222124"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.2m },
                    { new Guid("44444444-4444-4444-4444-444444444153"), new Guid("11111111-1111-1111-1111-111111111112"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 2, "Beginner", new Guid("22222222-2222-2222-2222-222222222105"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.1m },
                    { new Guid("44444444-4444-4444-4444-444444444154"), new Guid("11111111-1111-1111-1111-111111111112"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 3, "Beginner", new Guid("22222222-2222-2222-2222-222222222111"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.0m }
                });

            migrationBuilder.InsertData(
                table: "skill_prerequisites",
                columns: new[] { "Id", "CreatedAt", "PrerequisiteSkillId", "SkillId" },
                values: new object[,]
                {
                    { new Guid("55555555-5555-5555-5555-555555555510"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222120"), new Guid("22222222-2222-2222-2222-222222222121") },
                    { new Guid("55555555-5555-5555-5555-555555555511"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222121"), new Guid("22222222-2222-2222-2222-222222222106") },
                    { new Guid("55555555-5555-5555-5555-555555555512"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222120"), new Guid("22222222-2222-2222-2222-222222222106") },
                    { new Guid("55555555-5555-5555-5555-555555555513"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222121"), new Guid("22222222-2222-2222-2222-222222222107") },
                    { new Guid("55555555-5555-5555-5555-555555555514"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222120"), new Guid("22222222-2222-2222-2222-222222222108") },
                    { new Guid("55555555-5555-5555-5555-555555555515"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222105"), new Guid("22222222-2222-2222-2222-222222222124") },
                    { new Guid("55555555-5555-5555-5555-555555555516"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222101"), new Guid("22222222-2222-2222-2222-222222222127") },
                    { new Guid("55555555-5555-5555-5555-555555555517"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222105"), new Guid("22222222-2222-2222-2222-222222222127") },
                    { new Guid("55555555-5555-5555-5555-555555555518"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222101"), new Guid("22222222-2222-2222-2222-222222222126") },
                    { new Guid("55555555-5555-5555-5555-555555555519"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222102"), new Guid("22222222-2222-2222-2222-222222222123") },
                    { new Guid("55555555-5555-5555-5555-555555555520"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222101"), new Guid("22222222-2222-2222-2222-222222222123") },
                    { new Guid("55555555-5555-5555-5555-555555555521"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222102"), new Guid("22222222-2222-2222-2222-222222222125") },
                    { new Guid("55555555-5555-5555-5555-555555555522"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222112"), new Guid("22222222-2222-2222-2222-222222222129") },
                    { new Guid("55555555-5555-5555-5555-555555555523"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222113"), new Guid("22222222-2222-2222-2222-222222222130") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444141"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444142"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444143"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444144"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444145"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444146"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444147"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444148"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444149"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444150"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444151"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444152"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444153"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444154"));

            migrationBuilder.DeleteData(
                table: "skill_prerequisites",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555510"));

            migrationBuilder.DeleteData(
                table: "skill_prerequisites",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555511"));

            migrationBuilder.DeleteData(
                table: "skill_prerequisites",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555512"));

            migrationBuilder.DeleteData(
                table: "skill_prerequisites",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555513"));

            migrationBuilder.DeleteData(
                table: "skill_prerequisites",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555514"));

            migrationBuilder.DeleteData(
                table: "skill_prerequisites",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555515"));

            migrationBuilder.DeleteData(
                table: "skill_prerequisites",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555516"));

            migrationBuilder.DeleteData(
                table: "skill_prerequisites",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555517"));

            migrationBuilder.DeleteData(
                table: "skill_prerequisites",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555518"));

            migrationBuilder.DeleteData(
                table: "skill_prerequisites",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555519"));

            migrationBuilder.DeleteData(
                table: "skill_prerequisites",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555520"));

            migrationBuilder.DeleteData(
                table: "skill_prerequisites",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555521"));

            migrationBuilder.DeleteData(
                table: "skill_prerequisites",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555522"));

            migrationBuilder.DeleteData(
                table: "skill_prerequisites",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555523"));

            migrationBuilder.DeleteData(
                table: "skills",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222119"));

            migrationBuilder.DeleteData(
                table: "skills",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222122"));

            migrationBuilder.DeleteData(
                table: "career_roles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111110"));

            migrationBuilder.DeleteData(
                table: "career_roles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "career_roles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111112"));

            migrationBuilder.DeleteData(
                table: "skills",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222120"));

            migrationBuilder.DeleteData(
                table: "skills",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222121"));

            migrationBuilder.DeleteData(
                table: "skills",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222123"));

            migrationBuilder.DeleteData(
                table: "skills",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222124"));

            migrationBuilder.DeleteData(
                table: "skills",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222125"));

            migrationBuilder.DeleteData(
                table: "skills",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222126"));

            migrationBuilder.DeleteData(
                table: "skills",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222127"));

            migrationBuilder.DeleteData(
                table: "skills",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222128"));

            migrationBuilder.DeleteData(
                table: "skills",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222129"));

            migrationBuilder.DeleteData(
                table: "skills",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222130"));
        }
    }
}
