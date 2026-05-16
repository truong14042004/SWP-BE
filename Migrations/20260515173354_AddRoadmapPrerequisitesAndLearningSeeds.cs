using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SWP_BE.Migrations
{
    /// <inheritdoc />
    public partial class AddRoadmapPrerequisitesAndLearningSeeds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PrerequisiteNodeId",
                table: "roadmap_nodes",
                type: "uuid",
                nullable: true);

            migrationBuilder.InsertData(
                table: "skills",
                columns: new[] { "Id", "Category", "CreatedAt", "Description", "IsActive", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("22222222-2222-2222-2222-222222222101"), "Backend", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Designing HTTP APIs with resources, validation, authentication, pagination, and error contracts.", true, "REST API Design", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("22222222-2222-2222-2222-222222222102"), "Backend", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Modeling relational data, constraints, indexes, migrations, and query patterns.", true, "Database Design", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("22222222-2222-2222-2222-222222222103"), "Backend", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Implementing secure login, JWT, role-based access, and protected API flows.", true, "Authentication and Authorization", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("22222222-2222-2222-2222-222222222104"), "Engineering", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Writing focused automated tests for service logic, controllers, and integration boundaries.", true, "Unit Testing", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("22222222-2222-2222-2222-222222222105"), "DevOps", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Containerizing applications and composing local development services.", true, "Docker", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("22222222-2222-2222-2222-222222222106"), "Frontend", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Building component-based user interfaces and stateful client flows.", true, "React", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("22222222-2222-2222-2222-222222222107"), "Frontend", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Using static typing, interfaces, and type-safe application boundaries.", true, "TypeScript", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("22222222-2222-2222-2222-222222222108"), "Frontend", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Creating accessible layouts that work well across mobile and desktop screens.", true, "Responsive UI", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("22222222-2222-2222-2222-222222222109"), "Frontend", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Connecting frontend applications to authenticated APIs with loading and error states.", true, "API Integration", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("22222222-2222-2222-2222-222222222110"), "Mobile", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Designing and implementing mobile-first application screens and navigation.", true, "Mobile UI", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("22222222-2222-2222-2222-222222222111"), "DevOps", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Automating build, test, migration, and deployment workflows.", true, "CI/CD", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("22222222-2222-2222-2222-222222222112"), "Cloud", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Deploying applications to cloud infrastructure and managed platform services.", true, "Cloud Deployment", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("22222222-2222-2222-2222-222222222113"), "Data", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Writing relational queries, joins, aggregations, and data validation checks.", true, "SQL", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("22222222-2222-2222-2222-222222222114"), "Data", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Building reproducible data ingestion, transformation, and quality workflows.", true, "Data Pipeline", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("22222222-2222-2222-2222-222222222115"), "QA", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Automating API, UI, and regression tests with repeatable reporting.", true, "Test Automation", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("22222222-2222-2222-2222-222222222116"), "AI", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Calling AI providers with prompts, structured outputs, retries, and result persistence.", true, "AI API Integration", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("22222222-2222-2222-2222-222222222117"), "AI", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Designing prompts, evaluation criteria, and guardrails for AI-assisted features.", true, "Prompt Engineering", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("22222222-2222-2222-2222-222222222118"), "Career", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Presenting repositories with README, screenshots, demo links, and clear project evidence.", true, "GitHub Portfolio", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "learning_resources",
                columns: new[] { "Id", "CreatedAt", "Difficulty", "EstimatedHours", "IsActive", "ResourceType", "SkillId", "Title", "UpdatedAt", "Url" },
                values: new object[,]
                {
                    { new Guid("33333333-3333-3333-3333-333333333101"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Beginner", 8, true, "Documentation", new Guid("22222222-2222-2222-2222-222222222101"), "Microsoft Learn: Create web APIs with ASP.NET Core", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "https://learn.microsoft.com/en-us/aspnet/core/web-api/" },
                    { new Guid("33333333-3333-3333-3333-333333333102"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Beginner", 8, true, "Documentation", new Guid("22222222-2222-2222-2222-222222222102"), "Microsoft Learn: EF Core modeling", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "https://learn.microsoft.com/en-us/ef/core/modeling/" },
                    { new Guid("33333333-3333-3333-3333-333333333103"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Intermediate", 10, true, "Documentation", new Guid("22222222-2222-2222-2222-222222222103"), "Microsoft Learn: Authentication and authorization in ASP.NET Core", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "https://learn.microsoft.com/en-us/aspnet/core/security/authentication/" },
                    { new Guid("33333333-3333-3333-3333-333333333104"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Beginner", 6, true, "Documentation", new Guid("22222222-2222-2222-2222-222222222104"), "Microsoft Learn: Unit test C# code", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-dotnet-test" },
                    { new Guid("33333333-3333-3333-3333-333333333105"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Beginner", 6, true, "Documentation", new Guid("22222222-2222-2222-2222-222222222105"), "Docker Docs: Get started", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "https://docs.docker.com/get-started/" },
                    { new Guid("33333333-3333-3333-3333-333333333106"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Beginner", 10, true, "Documentation", new Guid("22222222-2222-2222-2222-222222222106"), "React Docs: Learn React", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "https://react.dev/learn" },
                    { new Guid("33333333-3333-3333-3333-333333333107"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Beginner", 8, true, "Documentation", new Guid("22222222-2222-2222-2222-222222222107"), "TypeScript Handbook", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "https://www.typescriptlang.org/docs/handbook/intro.html" },
                    { new Guid("33333333-3333-3333-3333-333333333108"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Beginner", 6, true, "Documentation", new Guid("22222222-2222-2222-2222-222222222108"), "MDN: Responsive design", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "https://developer.mozilla.org/en-US/docs/Learn_web_development/Core/CSS_layout/Responsive_Design" },
                    { new Guid("33333333-3333-3333-3333-333333333109"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Beginner", 5, true, "Documentation", new Guid("22222222-2222-2222-2222-222222222109"), "MDN: Fetch API", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "https://developer.mozilla.org/en-US/docs/Web/API/Fetch_API/Using_Fetch" },
                    { new Guid("33333333-3333-3333-3333-333333333110"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Beginner", 10, true, "Documentation", new Guid("22222222-2222-2222-2222-222222222110"), "Android Developers: App architecture", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "https://developer.android.com/topic/architecture" },
                    { new Guid("33333333-3333-3333-3333-333333333111"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Beginner", 6, true, "Documentation", new Guid("22222222-2222-2222-2222-222222222111"), "GitHub Actions: Learn GitHub Actions", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "https://docs.github.com/en/actions/learn-github-actions" },
                    { new Guid("33333333-3333-3333-3333-333333333112"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Beginner", 6, true, "Documentation", new Guid("22222222-2222-2222-2222-222222222112"), "Google Cloud: Cloud Run quickstart", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "https://cloud.google.com/run/docs/quickstarts" },
                    { new Guid("33333333-3333-3333-3333-333333333113"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Beginner", 8, true, "Documentation", new Guid("22222222-2222-2222-2222-222222222113"), "PostgreSQL Tutorial", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "https://www.postgresql.org/docs/current/tutorial.html" },
                    { new Guid("33333333-3333-3333-3333-333333333114"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Intermediate", 12, true, "Course", new Guid("22222222-2222-2222-2222-222222222114"), "Microsoft Learn: Data engineering on Microsoft Azure", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "https://learn.microsoft.com/en-us/training/paths/data-engineer-azure-databricks/" },
                    { new Guid("33333333-3333-3333-3333-333333333115"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Beginner", 6, true, "Documentation", new Guid("22222222-2222-2222-2222-222222222115"), "Playwright Docs: Getting started", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "https://playwright.dev/docs/intro" },
                    { new Guid("33333333-3333-3333-3333-333333333116"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Beginner", 6, true, "Documentation", new Guid("22222222-2222-2222-2222-222222222116"), "Google AI for Developers: Gemini API quickstart", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "https://ai.google.dev/gemini-api/docs/quickstart" },
                    { new Guid("33333333-3333-3333-3333-333333333117"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Intermediate", 6, true, "Documentation", new Guid("22222222-2222-2222-2222-222222222117"), "Google AI for Developers: Prompting strategies", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "https://ai.google.dev/gemini-api/docs/prompting-strategies" },
                    { new Guid("33333333-3333-3333-3333-333333333118"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Beginner", 4, true, "Documentation", new Guid("22222222-2222-2222-2222-222222222118"), "GitHub Docs: About READMEs", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/about-readmes" }
                });

            migrationBuilder.InsertData(
                table: "role_skill_requirements",
                columns: new[] { "Id", "CareerRoleId", "CreatedAt", "Priority", "RequiredLevel", "SkillId", "UpdatedAt", "Weight" },
                values: new object[,]
                {
                    { new Guid("44444444-4444-4444-4444-444444444101"), new Guid("11111111-1111-1111-1111-111111111101"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1, "Intermediate", new Guid("22222222-2222-2222-2222-222222222101"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.4m },
                    { new Guid("44444444-4444-4444-4444-444444444102"), new Guid("11111111-1111-1111-1111-111111111101"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1, "Intermediate", new Guid("22222222-2222-2222-2222-222222222102"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.3m },
                    { new Guid("44444444-4444-4444-4444-444444444103"), new Guid("11111111-1111-1111-1111-111111111101"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 2, "Beginner", new Guid("22222222-2222-2222-2222-222222222103"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.2m },
                    { new Guid("44444444-4444-4444-4444-444444444104"), new Guid("11111111-1111-1111-1111-111111111101"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 3, "Beginner", new Guid("22222222-2222-2222-2222-222222222104"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.0m },
                    { new Guid("44444444-4444-4444-4444-444444444105"), new Guid("11111111-1111-1111-1111-111111111101"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 4, "Beginner", new Guid("22222222-2222-2222-2222-222222222105"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.8m },
                    { new Guid("44444444-4444-4444-4444-444444444106"), new Guid("11111111-1111-1111-1111-111111111102"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1, "Intermediate", new Guid("22222222-2222-2222-2222-222222222106"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.4m },
                    { new Guid("44444444-4444-4444-4444-444444444107"), new Guid("11111111-1111-1111-1111-111111111102"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1, "Intermediate", new Guid("22222222-2222-2222-2222-222222222107"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.3m },
                    { new Guid("44444444-4444-4444-4444-444444444108"), new Guid("11111111-1111-1111-1111-111111111102"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 2, "Beginner", new Guid("22222222-2222-2222-2222-222222222108"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.1m },
                    { new Guid("44444444-4444-4444-4444-444444444109"), new Guid("11111111-1111-1111-1111-111111111102"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 2, "Beginner", new Guid("22222222-2222-2222-2222-222222222109"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.0m },
                    { new Guid("44444444-4444-4444-4444-444444444110"), new Guid("11111111-1111-1111-1111-111111111102"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 4, "Beginner", new Guid("22222222-2222-2222-2222-222222222118"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.8m },
                    { new Guid("44444444-4444-4444-4444-444444444111"), new Guid("11111111-1111-1111-1111-111111111103"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1, "Intermediate", new Guid("22222222-2222-2222-2222-222222222101"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.3m },
                    { new Guid("44444444-4444-4444-4444-444444444112"), new Guid("11111111-1111-1111-1111-111111111103"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1, "Intermediate", new Guid("22222222-2222-2222-2222-222222222106"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.3m },
                    { new Guid("44444444-4444-4444-4444-444444444113"), new Guid("11111111-1111-1111-1111-111111111103"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 2, "Intermediate", new Guid("22222222-2222-2222-2222-222222222107"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.2m },
                    { new Guid("44444444-4444-4444-4444-444444444114"), new Guid("11111111-1111-1111-1111-111111111103"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 2, "Beginner", new Guid("22222222-2222-2222-2222-222222222102"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.1m },
                    { new Guid("44444444-4444-4444-4444-444444444115"), new Guid("11111111-1111-1111-1111-111111111103"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 3, "Beginner", new Guid("22222222-2222-2222-2222-222222222103"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.0m },
                    { new Guid("44444444-4444-4444-4444-444444444116"), new Guid("11111111-1111-1111-1111-111111111104"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1, "Intermediate", new Guid("22222222-2222-2222-2222-222222222110"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.4m },
                    { new Guid("44444444-4444-4444-4444-444444444117"), new Guid("11111111-1111-1111-1111-111111111104"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 2, "Beginner", new Guid("22222222-2222-2222-2222-222222222109"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.2m },
                    { new Guid("44444444-4444-4444-4444-444444444118"), new Guid("11111111-1111-1111-1111-111111111104"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 3, "Beginner", new Guid("22222222-2222-2222-2222-222222222107"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.0m },
                    { new Guid("44444444-4444-4444-4444-444444444119"), new Guid("11111111-1111-1111-1111-111111111104"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 4, "Beginner", new Guid("22222222-2222-2222-2222-222222222118"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.8m },
                    { new Guid("44444444-4444-4444-4444-444444444120"), new Guid("11111111-1111-1111-1111-111111111105"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1, "Intermediate", new Guid("22222222-2222-2222-2222-222222222105"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.4m },
                    { new Guid("44444444-4444-4444-4444-444444444121"), new Guid("11111111-1111-1111-1111-111111111105"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1, "Intermediate", new Guid("22222222-2222-2222-2222-222222222111"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.4m },
                    { new Guid("44444444-4444-4444-4444-444444444122"), new Guid("11111111-1111-1111-1111-111111111105"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 2, "Beginner", new Guid("22222222-2222-2222-2222-222222222112"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.2m },
                    { new Guid("44444444-4444-4444-4444-444444444123"), new Guid("11111111-1111-1111-1111-111111111105"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 3, "Beginner", new Guid("22222222-2222-2222-2222-222222222104"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.9m },
                    { new Guid("44444444-4444-4444-4444-444444444124"), new Guid("11111111-1111-1111-1111-111111111106"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1, "Intermediate", new Guid("22222222-2222-2222-2222-222222222113"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.4m },
                    { new Guid("44444444-4444-4444-4444-444444444125"), new Guid("11111111-1111-1111-1111-111111111106"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1, "Intermediate", new Guid("22222222-2222-2222-2222-222222222114"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.4m },
                    { new Guid("44444444-4444-4444-4444-444444444126"), new Guid("11111111-1111-1111-1111-111111111106"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 3, "Beginner", new Guid("22222222-2222-2222-2222-222222222105"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.0m },
                    { new Guid("44444444-4444-4444-4444-444444444127"), new Guid("11111111-1111-1111-1111-111111111106"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 3, "Beginner", new Guid("22222222-2222-2222-2222-222222222112"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.9m },
                    { new Guid("44444444-4444-4444-4444-444444444128"), new Guid("11111111-1111-1111-1111-111111111107"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1, "Intermediate", new Guid("22222222-2222-2222-2222-222222222115"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.4m },
                    { new Guid("44444444-4444-4444-4444-444444444129"), new Guid("11111111-1111-1111-1111-111111111107"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 2, "Intermediate", new Guid("22222222-2222-2222-2222-222222222104"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.2m },
                    { new Guid("44444444-4444-4444-4444-444444444130"), new Guid("11111111-1111-1111-1111-111111111107"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 2, "Beginner", new Guid("22222222-2222-2222-2222-222222222109"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.0m },
                    { new Guid("44444444-4444-4444-4444-444444444131"), new Guid("11111111-1111-1111-1111-111111111107"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 3, "Beginner", new Guid("22222222-2222-2222-2222-222222222111"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.9m },
                    { new Guid("44444444-4444-4444-4444-444444444132"), new Guid("11111111-1111-1111-1111-111111111108"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1, "Intermediate", new Guid("22222222-2222-2222-2222-222222222112"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.5m },
                    { new Guid("44444444-4444-4444-4444-444444444133"), new Guid("11111111-1111-1111-1111-111111111108"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1, "Intermediate", new Guid("22222222-2222-2222-2222-222222222105"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.2m },
                    { new Guid("44444444-4444-4444-4444-444444444134"), new Guid("11111111-1111-1111-1111-111111111108"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 2, "Beginner", new Guid("22222222-2222-2222-2222-222222222111"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.1m },
                    { new Guid("44444444-4444-4444-4444-444444444135"), new Guid("11111111-1111-1111-1111-111111111108"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 3, "Beginner", new Guid("22222222-2222-2222-2222-222222222113"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.8m },
                    { new Guid("44444444-4444-4444-4444-444444444136"), new Guid("11111111-1111-1111-1111-111111111109"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1, "Intermediate", new Guid("22222222-2222-2222-2222-222222222116"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.5m },
                    { new Guid("44444444-4444-4444-4444-444444444137"), new Guid("11111111-1111-1111-1111-111111111109"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1, "Intermediate", new Guid("22222222-2222-2222-2222-222222222117"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.3m },
                    { new Guid("44444444-4444-4444-4444-444444444138"), new Guid("11111111-1111-1111-1111-111111111109"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 2, "Beginner", new Guid("22222222-2222-2222-2222-222222222101"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.0m },
                    { new Guid("44444444-4444-4444-4444-444444444139"), new Guid("11111111-1111-1111-1111-111111111109"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 3, "Beginner", new Guid("22222222-2222-2222-2222-222222222107"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.9m },
                    { new Guid("44444444-4444-4444-4444-444444444140"), new Guid("11111111-1111-1111-1111-111111111109"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 4, "Beginner", new Guid("22222222-2222-2222-2222-222222222118"), new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.8m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_roadmap_nodes_PrerequisiteNodeId",
                table: "roadmap_nodes",
                column: "PrerequisiteNodeId");

            migrationBuilder.AddForeignKey(
                name: "FK_roadmap_nodes_roadmap_nodes_PrerequisiteNodeId",
                table: "roadmap_nodes",
                column: "PrerequisiteNodeId",
                principalTable: "roadmap_nodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_roadmap_nodes_roadmap_nodes_PrerequisiteNodeId",
                table: "roadmap_nodes");

            migrationBuilder.DropIndex(
                name: "IX_roadmap_nodes_PrerequisiteNodeId",
                table: "roadmap_nodes");

            migrationBuilder.DeleteData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333101"));

            migrationBuilder.DeleteData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333102"));

            migrationBuilder.DeleteData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333103"));

            migrationBuilder.DeleteData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333104"));

            migrationBuilder.DeleteData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333105"));

            migrationBuilder.DeleteData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333106"));

            migrationBuilder.DeleteData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333107"));

            migrationBuilder.DeleteData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333108"));

            migrationBuilder.DeleteData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333109"));

            migrationBuilder.DeleteData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333110"));

            migrationBuilder.DeleteData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333111"));

            migrationBuilder.DeleteData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333112"));

            migrationBuilder.DeleteData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333113"));

            migrationBuilder.DeleteData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333114"));

            migrationBuilder.DeleteData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333115"));

            migrationBuilder.DeleteData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333116"));

            migrationBuilder.DeleteData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333117"));

            migrationBuilder.DeleteData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333118"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444101"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444102"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444103"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444104"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444105"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444106"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444107"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444108"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444109"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444110"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444111"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444112"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444113"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444114"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444115"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444116"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444117"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444118"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444119"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444120"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444121"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444122"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444123"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444124"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444125"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444126"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444127"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444128"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444129"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444130"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444131"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444132"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444133"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444134"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444135"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444136"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444137"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444138"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444139"));

            migrationBuilder.DeleteData(
                table: "role_skill_requirements",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444140"));

            migrationBuilder.DeleteData(
                table: "skills",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222101"));

            migrationBuilder.DeleteData(
                table: "skills",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222102"));

            migrationBuilder.DeleteData(
                table: "skills",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222103"));

            migrationBuilder.DeleteData(
                table: "skills",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222104"));

            migrationBuilder.DeleteData(
                table: "skills",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222105"));

            migrationBuilder.DeleteData(
                table: "skills",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222106"));

            migrationBuilder.DeleteData(
                table: "skills",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222107"));

            migrationBuilder.DeleteData(
                table: "skills",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222108"));

            migrationBuilder.DeleteData(
                table: "skills",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222109"));

            migrationBuilder.DeleteData(
                table: "skills",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222110"));

            migrationBuilder.DeleteData(
                table: "skills",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222111"));

            migrationBuilder.DeleteData(
                table: "skills",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222112"));

            migrationBuilder.DeleteData(
                table: "skills",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222113"));

            migrationBuilder.DeleteData(
                table: "skills",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222114"));

            migrationBuilder.DeleteData(
                table: "skills",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222115"));

            migrationBuilder.DeleteData(
                table: "skills",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222116"));

            migrationBuilder.DeleteData(
                table: "skills",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222117"));

            migrationBuilder.DeleteData(
                table: "skills",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222118"));

            migrationBuilder.DropColumn(
                name: "PrerequisiteNodeId",
                table: "roadmap_nodes");
        }
    }
}
