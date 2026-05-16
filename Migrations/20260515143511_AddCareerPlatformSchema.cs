using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP_BE.Migrations
{
    /// <inheritdoc />
    public partial class AddCareerPlatformSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Student",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.CreateTable(
                name: "career_roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_career_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "coupons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    DiscountType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DiscountValue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxUsage = table.Column<int>(type: "integer", nullable: true),
                    UsedCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ExpiredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_coupons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "github_repositories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RepoName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RepoUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    MainLanguage = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ReadmeContent = table.Column<string>(type: "text", nullable: true),
                    AiSummary = table.Column<string>(type: "text", nullable: true),
                    TechStackJson = table.Column<string>(type: "jsonb", nullable: true),
                    QualityScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    LastSyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_github_repositories", x => x.Id);
                    table.CheckConstraint("CK_github_repositories_QualityScore", "\"QualityScore\" IS NULL OR (\"QualityScore\" >= 0 AND \"QualityScore\" <= 100)");
                    table.ForeignKey(
                        name: "FK_github_repositories_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mentor_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Question = table.Column<string>(type: "text", nullable: false),
                    Answer = table.Column<string>(type: "text", nullable: false),
                    ContextJson = table.Column<string>(type: "jsonb", nullable: true),
                    Model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TokensUsed = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mentor_sessions", x => x.Id);
                    table.CheckConstraint("CK_mentor_sessions_TokensUsed", "\"TokensUsed\" IS NULL OR \"TokensUsed\" >= 0");
                    table.ForeignKey(
                        name: "FK_mentor_sessions_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payment_webhook_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EventId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_webhook_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "portfolios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Bio = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Theme = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true, defaultValue: "Default"),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_portfolios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_portfolios_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "skills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skills", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "subscription_plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    BillingCycle = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    FeaturesJson = table.Column<string>(type: "jsonb", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_plans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "skill_gap_reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CareerRoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Summary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skill_gap_reports", x => x.Id);
                    table.CheckConstraint("CK_skill_gap_reports_MatchScore", "\"MatchScore\" >= 0 AND \"MatchScore\" <= 100");
                    table.ForeignKey(
                        name: "FK_skill_gap_reports_career_roles_CareerRoleId",
                        column: x => x.CareerRoleId,
                        principalTable: "career_roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_skill_gap_reports_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "student_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    School = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Major = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Year = table.Column<int>(type: "integer", nullable: true),
                    Gpa = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: true),
                    TargetRoleId = table.Column<Guid>(type: "uuid", nullable: true),
                    GithubUsername = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CareerGoal = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PreferredLearningHoursPerWeek = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_profiles", x => x.Id);
                    table.CheckConstraint("CK_student_profiles_Gpa", "\"Gpa\" IS NULL OR \"Gpa\" >= 0");
                    table.CheckConstraint("CK_student_profiles_PreferredLearningHoursPerWeek", "\"PreferredLearningHoursPerWeek\" IS NULL OR \"PreferredLearningHoursPerWeek\" >= 0");
                    table.CheckConstraint("CK_student_profiles_Year", "\"Year\" IS NULL OR (\"Year\" >= 1 AND \"Year\" <= 8)");
                    table.ForeignKey(
                        name: "FK_student_profiles_career_roles_TargetRoleId",
                        column: x => x.TargetRoleId,
                        principalTable: "career_roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_student_profiles_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mentor_feedbacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MentorId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PortfolioId = table.Column<Guid>(type: "uuid", nullable: true),
                    GithubRepositoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Comment = table.Column<string>(type: "text", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mentor_feedbacks", x => x.Id);
                    table.CheckConstraint("CK_mentor_feedbacks_Rating", "\"Rating\" IS NULL OR (\"Rating\" >= 1 AND \"Rating\" <= 5)");
                    table.ForeignKey(
                        name: "FK_mentor_feedbacks_github_repositories_GithubRepositoryId",
                        column: x => x.GithubRepositoryId,
                        principalTable: "github_repositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_mentor_feedbacks_portfolios_PortfolioId",
                        column: x => x.PortfolioId,
                        principalTable: "portfolios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_mentor_feedbacks_users_MentorId",
                        column: x => x.MentorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_mentor_feedbacks_users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "portfolio_projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PortfolioId = table.Column<Guid>(type: "uuid", nullable: false),
                    GithubRepositoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    TechStackJson = table.Column<string>(type: "jsonb", nullable: true),
                    DemoUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    SourceUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_portfolio_projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_portfolio_projects_github_repositories_GithubRepositoryId",
                        column: x => x.GithubRepositoryId,
                        principalTable: "github_repositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_portfolio_projects_portfolios_PortfolioId",
                        column: x => x.PortfolioId,
                        principalTable: "portfolios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "github_repository_skills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GithubRepositoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    EvidenceText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_github_repository_skills", x => x.Id);
                    table.CheckConstraint("CK_github_repository_skills_ConfidenceScore", "\"ConfidenceScore\" IS NULL OR (\"ConfidenceScore\" >= 0 AND \"ConfidenceScore\" <= 100)");
                    table.ForeignKey(
                        name: "FK_github_repository_skills_github_repositories_GithubReposito~",
                        column: x => x.GithubRepositoryId,
                        principalTable: "github_repositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_github_repository_skills_skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "learning_resources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Url = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Difficulty = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EstimatedHours = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learning_resources", x => x.Id);
                    table.CheckConstraint("CK_learning_resources_EstimatedHours", "\"EstimatedHours\" IS NULL OR \"EstimatedHours\" >= 0");
                    table.ForeignKey(
                        name: "FK_learning_resources_skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "role_skill_requirements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CareerRoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequiredLevel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Weight = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 1m),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_skill_requirements", x => x.Id);
                    table.CheckConstraint("CK_role_skill_requirements_Priority", "\"Priority\" >= 1 AND \"Priority\" <= 5");
                    table.CheckConstraint("CK_role_skill_requirements_Weight", "\"Weight\" > 0");
                    table.ForeignKey(
                        name: "FK_role_skill_requirements_career_roles_CareerRoleId",
                        column: x => x.CareerRoleId,
                        principalTable: "career_roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_role_skill_requirements_skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_skills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillId = table.Column<Guid>(type: "uuid", nullable: false),
                    Level = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    EvidenceUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    EvidenceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    VerifiedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_skills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_skills_skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_skills_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_skills_users_VerifiedByUserId",
                        column: x => x.VerifiedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExpiredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ProviderSubscriptionId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_subscriptions_subscription_plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "subscription_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_subscriptions_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "roadmaps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CareerRoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillGapReportId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Draft"),
                    Progress = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0m),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roadmaps", x => x.Id);
                    table.CheckConstraint("CK_roadmaps_Progress", "\"Progress\" >= 0 AND \"Progress\" <= 100");
                    table.ForeignKey(
                        name: "FK_roadmaps_career_roles_CareerRoleId",
                        column: x => x.CareerRoleId,
                        principalTable: "career_roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_roadmaps_skill_gap_reports_SkillGapReportId",
                        column: x => x.SkillGapReportId,
                        principalTable: "skill_gap_reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_roadmaps_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "skill_gap_report_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillGapReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentLevel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    RequiredLevel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Recommendation = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skill_gap_report_items", x => x.Id);
                    table.CheckConstraint("CK_skill_gap_report_items_Priority", "\"Priority\" >= 1 AND \"Priority\" <= 5");
                    table.ForeignKey(
                        name: "FK_skill_gap_report_items_skill_gap_reports_SkillGapReportId",
                        column: x => x.SkillGapReportId,
                        principalTable: "skill_gap_reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_skill_gap_report_items_skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProviderTransactionId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CheckoutUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    PaidAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payment_transactions_subscription_plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "subscription_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payment_transactions_subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_payment_transactions_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "counselor_feedbacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CounselorId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoadmapId = table.Column<Guid>(type: "uuid", nullable: true),
                    SkillGapReportId = table.Column<Guid>(type: "uuid", nullable: true),
                    Comment = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_counselor_feedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_counselor_feedbacks_roadmaps_RoadmapId",
                        column: x => x.RoadmapId,
                        principalTable: "roadmaps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_counselor_feedbacks_skill_gap_reports_SkillGapReportId",
                        column: x => x.SkillGapReportId,
                        principalTable: "skill_gap_reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_counselor_feedbacks_users_CounselorId",
                        column: x => x.CounselorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_counselor_feedbacks_users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "roadmap_nodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoadmapId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillId = table.Column<Guid>(type: "uuid", nullable: true),
                    LearningResourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    NodeType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "NotStarted"),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    EstimatedHours = table.Column<int>(type: "integer", nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roadmap_nodes", x => x.Id);
                    table.CheckConstraint("CK_roadmap_nodes_EstimatedHours", "\"EstimatedHours\" IS NULL OR \"EstimatedHours\" >= 0");
                    table.CheckConstraint("CK_roadmap_nodes_Priority", "\"Priority\" >= 1 AND \"Priority\" <= 5");
                    table.ForeignKey(
                        name: "FK_roadmap_nodes_learning_resources_LearningResourceId",
                        column: x => x.LearningResourceId,
                        principalTable: "learning_resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_roadmap_nodes_roadmaps_RoadmapId",
                        column: x => x.RoadmapId,
                        principalTable: "roadmaps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_roadmap_nodes_skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "invoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PdfUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_invoices_payment_transactions_PaymentTransactionId",
                        column: x => x.PaymentTransactionId,
                        principalTable: "payment_transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_invoices_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_IsActive",
                table: "users",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_users_Role",
                table: "users",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_career_roles_IsActive",
                table: "career_roles",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_career_roles_Name",
                table: "career_roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_counselor_feedbacks_CounselorId",
                table: "counselor_feedbacks",
                column: "CounselorId");

            migrationBuilder.CreateIndex(
                name: "IX_counselor_feedbacks_RoadmapId",
                table: "counselor_feedbacks",
                column: "RoadmapId");

            migrationBuilder.CreateIndex(
                name: "IX_counselor_feedbacks_SkillGapReportId",
                table: "counselor_feedbacks",
                column: "SkillGapReportId");

            migrationBuilder.CreateIndex(
                name: "IX_counselor_feedbacks_StudentId",
                table: "counselor_feedbacks",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_coupons_Code",
                table: "coupons",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_coupons_ExpiredAt",
                table: "coupons",
                column: "ExpiredAt");

            migrationBuilder.CreateIndex(
                name: "IX_coupons_IsActive",
                table: "coupons",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_github_repositories_MainLanguage",
                table: "github_repositories",
                column: "MainLanguage");

            migrationBuilder.CreateIndex(
                name: "IX_github_repositories_UserId_RepoUrl",
                table: "github_repositories",
                columns: new[] { "UserId", "RepoUrl" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_github_repository_skills_GithubRepositoryId_SkillId",
                table: "github_repository_skills",
                columns: new[] { "GithubRepositoryId", "SkillId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_github_repository_skills_SkillId",
                table: "github_repository_skills",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_InvoiceNumber",
                table: "invoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoices_PaymentTransactionId",
                table: "invoices",
                column: "PaymentTransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoices_UserId",
                table: "invoices",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_learning_resources_IsActive",
                table: "learning_resources",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_learning_resources_SkillId",
                table: "learning_resources",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_mentor_feedbacks_GithubRepositoryId",
                table: "mentor_feedbacks",
                column: "GithubRepositoryId");

            migrationBuilder.CreateIndex(
                name: "IX_mentor_feedbacks_MentorId",
                table: "mentor_feedbacks",
                column: "MentorId");

            migrationBuilder.CreateIndex(
                name: "IX_mentor_feedbacks_PortfolioId",
                table: "mentor_feedbacks",
                column: "PortfolioId");

            migrationBuilder.CreateIndex(
                name: "IX_mentor_feedbacks_StudentId",
                table: "mentor_feedbacks",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_mentor_sessions_UserId_CreatedAt",
                table: "mentor_sessions",
                columns: new[] { "UserId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_PlanId",
                table: "payment_transactions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_Provider",
                table: "payment_transactions",
                column: "Provider");

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_ProviderTransactionId",
                table: "payment_transactions",
                column: "ProviderTransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_Status",
                table: "payment_transactions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_SubscriptionId",
                table: "payment_transactions",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_UserId_CreatedAt",
                table: "payment_transactions",
                columns: new[] { "UserId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_payment_webhook_events_EventType",
                table: "payment_webhook_events",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_payment_webhook_events_Provider_EventId",
                table: "payment_webhook_events",
                columns: new[] { "Provider", "EventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_portfolio_projects_GithubRepositoryId",
                table: "portfolio_projects",
                column: "GithubRepositoryId");

            migrationBuilder.CreateIndex(
                name: "IX_portfolio_projects_PortfolioId_OrderIndex",
                table: "portfolio_projects",
                columns: new[] { "PortfolioId", "OrderIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_portfolios_Slug",
                table: "portfolios",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_portfolios_UserId_IsPublished",
                table: "portfolios",
                columns: new[] { "UserId", "IsPublished" });

            migrationBuilder.CreateIndex(
                name: "IX_roadmap_nodes_LearningResourceId",
                table: "roadmap_nodes",
                column: "LearningResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_roadmap_nodes_RoadmapId_OrderIndex",
                table: "roadmap_nodes",
                columns: new[] { "RoadmapId", "OrderIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_roadmap_nodes_SkillId",
                table: "roadmap_nodes",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_roadmap_nodes_Status",
                table: "roadmap_nodes",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_roadmaps_CareerRoleId",
                table: "roadmaps",
                column: "CareerRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_roadmaps_SkillGapReportId",
                table: "roadmaps",
                column: "SkillGapReportId");

            migrationBuilder.CreateIndex(
                name: "IX_roadmaps_UserId_Status",
                table: "roadmaps",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_role_skill_requirements_CareerRoleId_SkillId",
                table: "role_skill_requirements",
                columns: new[] { "CareerRoleId", "SkillId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_role_skill_requirements_Priority",
                table: "role_skill_requirements",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_role_skill_requirements_SkillId",
                table: "role_skill_requirements",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_skill_gap_report_items_Priority",
                table: "skill_gap_report_items",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_skill_gap_report_items_SkillGapReportId_SkillId",
                table: "skill_gap_report_items",
                columns: new[] { "SkillGapReportId", "SkillId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_skill_gap_report_items_SkillId",
                table: "skill_gap_report_items",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_skill_gap_report_items_Status",
                table: "skill_gap_report_items",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_skill_gap_reports_CareerRoleId",
                table: "skill_gap_reports",
                column: "CareerRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_skill_gap_reports_UserId_CreatedAt",
                table: "skill_gap_reports",
                columns: new[] { "UserId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_skills_Category",
                table: "skills",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_skills_IsActive",
                table: "skills",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_skills_Name_Category",
                table: "skills",
                columns: new[] { "Name", "Category" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_student_profiles_GithubUsername",
                table: "student_profiles",
                column: "GithubUsername");

            migrationBuilder.CreateIndex(
                name: "IX_student_profiles_TargetRoleId",
                table: "student_profiles",
                column: "TargetRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_student_profiles_UserId",
                table: "student_profiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_plans_IsActive",
                table: "subscription_plans",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_plans_Name",
                table: "subscription_plans",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_ExpiredAt",
                table: "subscriptions",
                column: "ExpiredAt");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_PlanId",
                table: "subscriptions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_ProviderSubscriptionId",
                table: "subscriptions",
                column: "ProviderSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_UserId_Status",
                table: "subscriptions",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_user_skills_IsVerified",
                table: "user_skills",
                column: "IsVerified");

            migrationBuilder.CreateIndex(
                name: "IX_user_skills_Level",
                table: "user_skills",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_user_skills_SkillId",
                table: "user_skills",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_user_skills_UserId_SkillId",
                table: "user_skills",
                columns: new[] { "UserId", "SkillId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_skills_VerifiedByUserId",
                table: "user_skills",
                column: "VerifiedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "counselor_feedbacks");

            migrationBuilder.DropTable(
                name: "coupons");

            migrationBuilder.DropTable(
                name: "github_repository_skills");

            migrationBuilder.DropTable(
                name: "invoices");

            migrationBuilder.DropTable(
                name: "mentor_feedbacks");

            migrationBuilder.DropTable(
                name: "mentor_sessions");

            migrationBuilder.DropTable(
                name: "payment_webhook_events");

            migrationBuilder.DropTable(
                name: "portfolio_projects");

            migrationBuilder.DropTable(
                name: "roadmap_nodes");

            migrationBuilder.DropTable(
                name: "role_skill_requirements");

            migrationBuilder.DropTable(
                name: "skill_gap_report_items");

            migrationBuilder.DropTable(
                name: "student_profiles");

            migrationBuilder.DropTable(
                name: "user_skills");

            migrationBuilder.DropTable(
                name: "payment_transactions");

            migrationBuilder.DropTable(
                name: "github_repositories");

            migrationBuilder.DropTable(
                name: "portfolios");

            migrationBuilder.DropTable(
                name: "learning_resources");

            migrationBuilder.DropTable(
                name: "roadmaps");

            migrationBuilder.DropTable(
                name: "subscriptions");

            migrationBuilder.DropTable(
                name: "skills");

            migrationBuilder.DropTable(
                name: "skill_gap_reports");

            migrationBuilder.DropTable(
                name: "subscription_plans");

            migrationBuilder.DropTable(
                name: "career_roles");

            migrationBuilder.DropIndex(
                name: "IX_users_IsActive",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_Role",
                table: "users");

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldDefaultValue: "Student");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "users",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);
        }
    }
}
