using Microsoft.EntityFrameworkCore;
using SWP_BE.Models;

namespace SWP_BE.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    private static readonly DateTimeOffset SeededAt = new(2026, 5, 15, 0, 0, 0, TimeSpan.Zero);

    public DbSet<User> Users => Set<User>();
    public DbSet<PendingRegistration> PendingRegistrations => Set<PendingRegistration>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<UserSkill> UserSkills => Set<UserSkill>();
    public DbSet<CareerRole> CareerRoles => Set<CareerRole>();
    public DbSet<RoleSkillRequirement> RoleSkillRequirements => Set<RoleSkillRequirement>();
    public DbSet<SkillGapReport> SkillGapReports => Set<SkillGapReport>();
    public DbSet<SkillGapReportItem> SkillGapReportItems => Set<SkillGapReportItem>();
    public DbSet<Roadmap> Roadmaps => Set<Roadmap>();
    public DbSet<RoadmapNode> RoadmapNodes => Set<RoadmapNode>();
    public DbSet<LearningResource> LearningResources => Set<LearningResource>();
    public DbSet<MentorSession> MentorSessions => Set<MentorSession>();
    public DbSet<GithubRepository> GithubRepositories => Set<GithubRepository>();
    public DbSet<GithubRepositorySkill> GithubRepositorySkills => Set<GithubRepositorySkill>();
    public DbSet<GithubConnection> GithubConnections => Set<GithubConnection>();
    public DbSet<GithubOAuthState> GithubOAuthStates => Set<GithubOAuthState>();
    public DbSet<Portfolio> Portfolios => Set<Portfolio>();
    public DbSet<PortfolioProject> PortfolioProjects => Set<PortfolioProject>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<PaymentWebhookEvent> PaymentWebhookEvents => Set<PaymentWebhookEvent>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<MentorFeedback> MentorFeedbacks => Set<MentorFeedback>();
    public DbSet<CounselorFeedback> CounselorFeedbacks => Set<CounselorFeedback>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");

            entity.HasKey(user => user.Id);

            entity.Property(user => user.Username)
                .HasMaxLength(100);

            entity.HasIndex(user => user.Username)
                .IsUnique();

            entity.Property(user => user.Email)
                .HasMaxLength(256)
                .IsRequired();

            entity.HasIndex(user => user.Email)
                .IsUnique();

            entity.Property(user => user.FullName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(user => user.AvatarUrl)
                .HasMaxLength(1024);

            entity.Property(user => user.GoogleSubject)
                .HasMaxLength(128);

            entity.HasIndex(user => user.GoogleSubject)
                .IsUnique();

            entity.Property(user => user.PasswordHash)
                .HasMaxLength(512);

            entity.Property(user => user.EmailVerificationOtpHash)
                .HasMaxLength(512);

            entity.Property(user => user.Role)
                .HasMaxLength(50)
                .HasDefaultValue(UserRoles.Student)
                .IsRequired();

            entity.HasIndex(user => user.Role);
            entity.HasCheckConstraint("CK_users_Role",
                "\"Role\" IN ('Student', 'Admin', 'AcademicCounselor', 'IndustryMentor')");

            entity.Property(user => user.IsActive)
                .HasDefaultValue(true);

            entity.HasIndex(user => user.IsActive);
        });

        modelBuilder.Entity<PendingRegistration>(entity =>
        {
            entity.ToTable("pending_registrations");
            entity.HasKey(registration => registration.Id);

            entity.Property(registration => registration.Username)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(registration => registration.Username)
                .IsUnique();

            entity.Property(registration => registration.Email)
                .HasMaxLength(256)
                .IsRequired();

            entity.HasIndex(registration => registration.Email)
                .IsUnique();

            entity.Property(registration => registration.FullName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(registration => registration.PasswordHash)
                .HasMaxLength(512)
                .IsRequired();

            entity.Property(registration => registration.EmailVerificationOtpHash)
                .HasMaxLength(512)
                .IsRequired();

            entity.HasIndex(registration => registration.EmailVerificationOtpExpiresAt);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(token => token.Id);
            entity.HasIndex(token => token.TokenHash).IsUnique();
            entity.HasIndex(token => token.UserId);
            entity.HasIndex(token => token.ExpiresAt);
            entity.Property(token => token.TokenHash).HasMaxLength(64).IsRequired();
            entity.Property(token => token.ReplacedByTokenHash).HasMaxLength(64);
            entity.HasOne(token => token.User)
                .WithMany()
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StudentProfile>(entity =>
        {
            entity.ToTable("student_profiles");
            entity.HasKey(profile => profile.Id);
            entity.HasIndex(profile => profile.UserId).IsUnique();
            entity.HasIndex(profile => profile.TargetRoleId);
            entity.HasIndex(profile => profile.GithubUsername);
            entity.Property(profile => profile.School).HasMaxLength(200);
            entity.Property(profile => profile.Major).HasMaxLength(200);
            entity.Property(profile => profile.Gpa).HasPrecision(4, 2);
            entity.Property(profile => profile.GithubUsername).HasMaxLength(100);
            entity.Property(profile => profile.CareerGoal).HasMaxLength(1000);
            entity.HasCheckConstraint("CK_student_profiles_Year", "\"Year\" IS NULL OR (\"Year\" >= 1 AND \"Year\" <= 8)");
            entity.HasCheckConstraint("CK_student_profiles_Gpa", "\"Gpa\" IS NULL OR \"Gpa\" >= 0");
            entity.HasCheckConstraint("CK_student_profiles_PreferredLearningHoursPerWeek",
                "\"PreferredLearningHoursPerWeek\" IS NULL OR \"PreferredLearningHoursPerWeek\" >= 0");
            entity.HasOne(profile => profile.User)
                .WithOne()
                .HasForeignKey<StudentProfile>(profile => profile.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(profile => profile.TargetRole)
                .WithMany()
                .HasForeignKey(profile => profile.TargetRoleId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Skill>(entity =>
        {
            entity.ToTable("skills");
            entity.HasKey(skill => skill.Id);
            entity.HasIndex(skill => new { skill.Name, skill.Category }).IsUnique();
            entity.HasIndex(skill => skill.Category);
            entity.HasIndex(skill => skill.IsActive);
            entity.Property(skill => skill.Name).HasMaxLength(150).IsRequired();
            entity.Property(skill => skill.Category).HasMaxLength(80).IsRequired();
            entity.Property(skill => skill.Description).HasMaxLength(1000);
            entity.Property(skill => skill.IsActive).HasDefaultValue(true);
            entity.HasData(
                new Skill { Id = Guid.Parse("22222222-2222-2222-2222-222222222101"), Name = "REST API Design", Category = "Backend", Description = "Designing HTTP APIs with resources, validation, authentication, pagination, and error contracts.", IsActive = true, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new Skill { Id = Guid.Parse("22222222-2222-2222-2222-222222222102"), Name = "Database Design", Category = "Backend", Description = "Modeling relational data, constraints, indexes, migrations, and query patterns.", IsActive = true, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new Skill { Id = Guid.Parse("22222222-2222-2222-2222-222222222103"), Name = "Authentication and Authorization", Category = "Backend", Description = "Implementing secure login, JWT, role-based access, and protected API flows.", IsActive = true, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new Skill { Id = Guid.Parse("22222222-2222-2222-2222-222222222104"), Name = "Unit Testing", Category = "Engineering", Description = "Writing focused automated tests for service logic, controllers, and integration boundaries.", IsActive = true, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new Skill { Id = Guid.Parse("22222222-2222-2222-2222-222222222105"), Name = "Docker", Category = "DevOps", Description = "Containerizing applications and composing local development services.", IsActive = true, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new Skill { Id = Guid.Parse("22222222-2222-2222-2222-222222222106"), Name = "React", Category = "Frontend", Description = "Building component-based user interfaces and stateful client flows.", IsActive = true, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new Skill { Id = Guid.Parse("22222222-2222-2222-2222-222222222107"), Name = "TypeScript", Category = "Frontend", Description = "Using static typing, interfaces, and type-safe application boundaries.", IsActive = true, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new Skill { Id = Guid.Parse("22222222-2222-2222-2222-222222222108"), Name = "Responsive UI", Category = "Frontend", Description = "Creating accessible layouts that work well across mobile and desktop screens.", IsActive = true, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new Skill { Id = Guid.Parse("22222222-2222-2222-2222-222222222109"), Name = "API Integration", Category = "Frontend", Description = "Connecting frontend applications to authenticated APIs with loading and error states.", IsActive = true, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new Skill { Id = Guid.Parse("22222222-2222-2222-2222-222222222110"), Name = "Mobile UI", Category = "Mobile", Description = "Designing and implementing mobile-first application screens and navigation.", IsActive = true, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new Skill { Id = Guid.Parse("22222222-2222-2222-2222-222222222111"), Name = "CI/CD", Category = "DevOps", Description = "Automating build, test, migration, and deployment workflows.", IsActive = true, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new Skill { Id = Guid.Parse("22222222-2222-2222-2222-222222222112"), Name = "Cloud Deployment", Category = "Cloud", Description = "Deploying applications to cloud infrastructure and managed platform services.", IsActive = true, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new Skill { Id = Guid.Parse("22222222-2222-2222-2222-222222222113"), Name = "SQL", Category = "Data", Description = "Writing relational queries, joins, aggregations, and data validation checks.", IsActive = true, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new Skill { Id = Guid.Parse("22222222-2222-2222-2222-222222222114"), Name = "Data Pipeline", Category = "Data", Description = "Building reproducible data ingestion, transformation, and quality workflows.", IsActive = true, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new Skill { Id = Guid.Parse("22222222-2222-2222-2222-222222222115"), Name = "Test Automation", Category = "QA", Description = "Automating API, UI, and regression tests with repeatable reporting.", IsActive = true, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new Skill { Id = Guid.Parse("22222222-2222-2222-2222-222222222116"), Name = "AI API Integration", Category = "AI", Description = "Calling AI providers with prompts, structured outputs, retries, and result persistence.", IsActive = true, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new Skill { Id = Guid.Parse("22222222-2222-2222-2222-222222222117"), Name = "Prompt Engineering", Category = "AI", Description = "Designing prompts, evaluation criteria, and guardrails for AI-assisted features.", IsActive = true, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new Skill { Id = Guid.Parse("22222222-2222-2222-2222-222222222118"), Name = "GitHub Portfolio", Category = "Career", Description = "Presenting repositories with README, screenshots, demo links, and clear project evidence.", IsActive = true, CreatedAt = SeededAt, UpdatedAt = SeededAt });
        });

        modelBuilder.Entity<UserSkill>(entity =>
        {
            entity.ToTable("user_skills");
            entity.HasKey(userSkill => userSkill.Id);
            entity.HasIndex(userSkill => new { userSkill.UserId, userSkill.SkillId }).IsUnique();
            entity.HasIndex(userSkill => userSkill.SkillId);
            entity.HasIndex(userSkill => userSkill.Level);
            entity.HasIndex(userSkill => userSkill.IsVerified);
            entity.Property(userSkill => userSkill.Level).HasMaxLength(30).IsRequired();
            entity.Property(userSkill => userSkill.EvidenceUrl).HasMaxLength(1024);
            entity.Property(userSkill => userSkill.EvidenceType).HasMaxLength(50);
            entity.Property(userSkill => userSkill.IsVerified).HasDefaultValue(false);
            entity.HasOne(userSkill => userSkill.User)
                .WithMany()
                .HasForeignKey(userSkill => userSkill.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(userSkill => userSkill.Skill)
                .WithMany()
                .HasForeignKey(userSkill => userSkill.SkillId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(userSkill => userSkill.VerifiedByUser)
                .WithMany()
                .HasForeignKey(userSkill => userSkill.VerifiedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<CareerRole>(entity =>
        {
            entity.ToTable("career_roles");
            entity.HasKey(role => role.Id);
            entity.HasIndex(role => role.Name).IsUnique();
            entity.HasIndex(role => role.IsActive);
            entity.Property(role => role.Name).HasMaxLength(150).IsRequired();
            entity.Property(role => role.Description).HasMaxLength(2000);
            entity.Property(role => role.Level).HasMaxLength(50);
            entity.Property(role => role.IsActive).HasDefaultValue(true);
            entity.HasData(
                new CareerRole
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111101"),
                    Name = "Backend Developer",
                    Description = "Builds server-side APIs, databases, authentication, and backend services.",
                    Level = "Fresher",
                    IsActive = true,
                    CreatedAt = SeededAt,
                    UpdatedAt = SeededAt
                },
                new CareerRole
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111102"),
                    Name = "Frontend Developer",
                    Description = "Builds user interfaces, client-side application logic, and responsive web experiences.",
                    Level = "Fresher",
                    IsActive = true,
                    CreatedAt = SeededAt,
                    UpdatedAt = SeededAt
                },
                new CareerRole
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111103"),
                    Name = "Fullstack Developer",
                    Description = "Builds both frontend interfaces and backend APIs for complete web applications.",
                    Level = "Fresher",
                    IsActive = true,
                    CreatedAt = SeededAt,
                    UpdatedAt = SeededAt
                },
                new CareerRole
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111104"),
                    Name = "Mobile Developer",
                    Description = "Builds mobile applications and integrates them with backend services.",
                    Level = "Fresher",
                    IsActive = true,
                    CreatedAt = SeededAt,
                    UpdatedAt = SeededAt
                },
                new CareerRole
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111105"),
                    Name = "DevOps Engineer",
                    Description = "Works on CI/CD, deployment automation, infrastructure, monitoring, and reliability.",
                    Level = "Fresher",
                    IsActive = true,
                    CreatedAt = SeededAt,
                    UpdatedAt = SeededAt
                },
                new CareerRole
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111106"),
                    Name = "Data Engineer",
                    Description = "Builds data pipelines, data storage, processing jobs, and analytics infrastructure.",
                    Level = "Fresher",
                    IsActive = true,
                    CreatedAt = SeededAt,
                    UpdatedAt = SeededAt
                },
                new CareerRole
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111107"),
                    Name = "QA Automation Engineer",
                    Description = "Designs automated tests, test frameworks, and quality assurance workflows.",
                    Level = "Fresher",
                    IsActive = true,
                    CreatedAt = SeededAt,
                    UpdatedAt = SeededAt
                },
                new CareerRole
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111108"),
                    Name = "Cloud Engineer",
                    Description = "Builds and operates cloud infrastructure, deployment environments, and managed services.",
                    Level = "Fresher",
                    IsActive = true,
                    CreatedAt = SeededAt,
                    UpdatedAt = SeededAt
                },
                new CareerRole
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111109"),
                    Name = "AI Application Developer",
                    Description = "Builds software applications that integrate AI APIs, prompts, agents, and AI-assisted workflows.",
                    Level = "Fresher",
                    IsActive = true,
                    CreatedAt = SeededAt,
                    UpdatedAt = SeededAt
                });
        });

        modelBuilder.Entity<RoleSkillRequirement>(entity =>
        {
            entity.ToTable("role_skill_requirements");
            entity.HasKey(requirement => requirement.Id);
            entity.HasIndex(requirement => new { requirement.CareerRoleId, requirement.SkillId }).IsUnique();
            entity.HasIndex(requirement => requirement.SkillId);
            entity.HasIndex(requirement => requirement.Priority);
            entity.Property(requirement => requirement.RequiredLevel).HasMaxLength(30).IsRequired();
            entity.Property(requirement => requirement.Weight).HasPrecision(5, 2).HasDefaultValue(1m);
            entity.HasCheckConstraint("CK_role_skill_requirements_Priority",
                "\"Priority\" >= 1 AND \"Priority\" <= 5");
            entity.HasCheckConstraint("CK_role_skill_requirements_Weight", "\"Weight\" > 0");
            entity.HasOne(requirement => requirement.CareerRole)
                .WithMany()
                .HasForeignKey(requirement => requirement.CareerRoleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(requirement => requirement.Skill)
                .WithMany()
                .HasForeignKey(requirement => requirement.SkillId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasData(
                new RoleSkillRequirement { Id = Guid.Parse("44444444-4444-4444-4444-444444444101"), CareerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111101"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222101"), RequiredLevel = "Intermediate", Priority = 1, Weight = 1.4m, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new RoleSkillRequirement { Id = Guid.Parse("44444444-4444-4444-4444-444444444102"), CareerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111101"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222102"), RequiredLevel = "Intermediate", Priority = 1, Weight = 1.3m, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new RoleSkillRequirement { Id = Guid.Parse("44444444-4444-4444-4444-444444444103"), CareerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111101"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222103"), RequiredLevel = "Beginner", Priority = 2, Weight = 1.2m, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new RoleSkillRequirement { Id = Guid.Parse("44444444-4444-4444-4444-444444444104"), CareerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111101"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222104"), RequiredLevel = "Beginner", Priority = 3, Weight = 1.0m, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new RoleSkillRequirement { Id = Guid.Parse("44444444-4444-4444-4444-444444444105"), CareerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111101"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222105"), RequiredLevel = "Beginner", Priority = 4, Weight = 0.8m, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new RoleSkillRequirement { Id = Guid.Parse("44444444-4444-4444-4444-444444444106"), CareerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111102"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222106"), RequiredLevel = "Intermediate", Priority = 1, Weight = 1.4m, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new RoleSkillRequirement { Id = Guid.Parse("44444444-4444-4444-4444-444444444107"), CareerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111102"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222107"), RequiredLevel = "Intermediate", Priority = 1, Weight = 1.3m, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new RoleSkillRequirement { Id = Guid.Parse("44444444-4444-4444-4444-444444444108"), CareerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111102"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222108"), RequiredLevel = "Beginner", Priority = 2, Weight = 1.1m, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new RoleSkillRequirement { Id = Guid.Parse("44444444-4444-4444-4444-444444444109"), CareerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111102"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222109"), RequiredLevel = "Beginner", Priority = 2, Weight = 1.0m, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new RoleSkillRequirement { Id = Guid.Parse("44444444-4444-4444-4444-444444444110"), CareerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111102"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222118"), RequiredLevel = "Beginner", Priority = 4, Weight = 0.8m, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new RoleSkillRequirement { Id = Guid.Parse("44444444-4444-4444-4444-444444444111"), CareerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111103"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222101"), RequiredLevel = "Intermediate", Priority = 1, Weight = 1.3m, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new RoleSkillRequirement { Id = Guid.Parse("44444444-4444-4444-4444-444444444112"), CareerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111103"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222106"), RequiredLevel = "Intermediate", Priority = 1, Weight = 1.3m, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new RoleSkillRequirement { Id = Guid.Parse("44444444-4444-4444-4444-444444444113"), CareerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111103"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222107"), RequiredLevel = "Intermediate", Priority = 2, Weight = 1.2m, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new RoleSkillRequirement { Id = Guid.Parse("44444444-4444-4444-4444-444444444114"), CareerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111103"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222102"), RequiredLevel = "Beginner", Priority = 2, Weight = 1.1m, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new RoleSkillRequirement { Id = Guid.Parse("44444444-4444-4444-4444-444444444115"), CareerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111103"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222103"), RequiredLevel = "Beginner", Priority = 3, Weight = 1.0m, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new RoleSkillRequirement { Id = Guid.Parse("44444444-4444-4444-4444-444444444116"), CareerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111104"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222110"), RequiredLevel = "Intermediate", Priority = 1, Weight = 1.4m, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new RoleSkillRequirement { Id = Guid.Parse("44444444-4444-4444-4444-444444444117"), CareerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111104"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222109"), RequiredLevel = "Beginner", Priority = 2, Weight = 1.2m, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new RoleSkillRequirement { Id = Guid.Parse("44444444-4444-4444-4444-444444444118"), CareerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111104"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222107"), RequiredLevel = "Beginner", Priority = 3, Weight = 1.0m, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new RoleSkillRequirement { Id = Guid.Parse("44444444-4444-4444-4444-444444444119"), CareerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111104"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222118"), RequiredLevel = "Beginner", Priority = 4, Weight = 0.8m, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new RoleSkillRequirement { Id = Guid.Parse("44444444-4444-4444-4444-444444444120"), CareerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111105"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222105"), RequiredLevel = "Intermediate", Priority = 1, Weight = 1.4m, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new RoleSkillRequirement { Id = Guid.Parse("44444444-4444-4444-4444-444444444121"), CareerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111105"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222111"), RequiredLevel = "Intermediate", Priority = 1, Weight = 1.4m, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new RoleSkillRequirement { Id = Guid.Parse("44444444-4444-4444-4444-444444444122"), CareerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111105"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222112"), RequiredLevel = "Beginner", Priority = 2, Weight = 1.2m, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new RoleSkillRequirement { Id = Guid.Parse("44444444-4444-4444-4444-444444444123"), CareerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111105"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222104"), RequiredLevel = "Beginner", Priority = 3, Weight = 0.9m, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new RoleSkillRequirement { Id = Guid.Parse("44444444-4444-4444-4444-444444444124"), CareerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111106"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222113"), RequiredLevel = "Intermediate", Priority = 1, Weight = 1.4m, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new RoleSkillRequirement { Id = Guid.Parse("44444444-4444-4444-4444-444444444125"), CareerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111106"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222114"), RequiredLevel = "Intermediate", Priority = 1, Weight = 1.4m, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new RoleSkillRequirement { Id = Guid.Parse("44444444-4444-4444-4444-444444444126"), CareerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111106"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222105"), RequiredLevel = "Beginner", Priority = 3, Weight = 1.0m, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new RoleSkillRequirement { Id = Guid.Parse("44444444-4444-4444-4444-444444444127"), CareerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111106"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222112"), RequiredLevel = "Beginner", Priority = 3, Weight = 0.9m, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new RoleSkillRequirement { Id = Guid.Parse("44444444-4444-4444-4444-444444444128"), CareerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111107"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222115"), RequiredLevel = "Intermediate", Priority = 1, Weight = 1.4m, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new RoleSkillRequirement { Id = Guid.Parse("44444444-4444-4444-4444-444444444129"), CareerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111107"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222104"), RequiredLevel = "Intermediate", Priority = 2, Weight = 1.2m, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new RoleSkillRequirement { Id = Guid.Parse("44444444-4444-4444-4444-444444444130"), CareerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111107"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222109"), RequiredLevel = "Beginner", Priority = 2, Weight = 1.0m, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new RoleSkillRequirement { Id = Guid.Parse("44444444-4444-4444-4444-444444444131"), CareerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111107"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222111"), RequiredLevel = "Beginner", Priority = 3, Weight = 0.9m, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new RoleSkillRequirement { Id = Guid.Parse("44444444-4444-4444-4444-444444444132"), CareerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111108"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222112"), RequiredLevel = "Intermediate", Priority = 1, Weight = 1.5m, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new RoleSkillRequirement { Id = Guid.Parse("44444444-4444-4444-4444-444444444133"), CareerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111108"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222105"), RequiredLevel = "Intermediate", Priority = 1, Weight = 1.2m, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new RoleSkillRequirement { Id = Guid.Parse("44444444-4444-4444-4444-444444444134"), CareerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111108"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222111"), RequiredLevel = "Beginner", Priority = 2, Weight = 1.1m, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new RoleSkillRequirement { Id = Guid.Parse("44444444-4444-4444-4444-444444444135"), CareerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111108"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222113"), RequiredLevel = "Beginner", Priority = 3, Weight = 0.8m, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new RoleSkillRequirement { Id = Guid.Parse("44444444-4444-4444-4444-444444444136"), CareerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111109"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222116"), RequiredLevel = "Intermediate", Priority = 1, Weight = 1.5m, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new RoleSkillRequirement { Id = Guid.Parse("44444444-4444-4444-4444-444444444137"), CareerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111109"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222117"), RequiredLevel = "Intermediate", Priority = 1, Weight = 1.3m, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new RoleSkillRequirement { Id = Guid.Parse("44444444-4444-4444-4444-444444444138"), CareerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111109"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222101"), RequiredLevel = "Beginner", Priority = 2, Weight = 1.0m, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new RoleSkillRequirement { Id = Guid.Parse("44444444-4444-4444-4444-444444444139"), CareerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111109"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222107"), RequiredLevel = "Beginner", Priority = 3, Weight = 0.9m, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new RoleSkillRequirement { Id = Guid.Parse("44444444-4444-4444-4444-444444444140"), CareerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111109"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222118"), RequiredLevel = "Beginner", Priority = 4, Weight = 0.8m, CreatedAt = SeededAt, UpdatedAt = SeededAt });
        });

        modelBuilder.Entity<SkillGapReport>(entity =>
        {
            entity.ToTable("skill_gap_reports");
            entity.HasKey(report => report.Id);
            entity.HasIndex(report => new { report.UserId, report.CreatedAt }).IsDescending(false, true);
            entity.HasIndex(report => report.CareerRoleId);
            entity.Property(report => report.MatchScore).HasPrecision(5, 2);
            entity.Property(report => report.Summary).HasMaxLength(4000);
            entity.HasCheckConstraint("CK_skill_gap_reports_MatchScore",
                "\"MatchScore\" >= 0 AND \"MatchScore\" <= 100");
            entity.HasOne(report => report.User)
                .WithMany()
                .HasForeignKey(report => report.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(report => report.CareerRole)
                .WithMany()
                .HasForeignKey(report => report.CareerRoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SkillGapReportItem>(entity =>
        {
            entity.ToTable("skill_gap_report_items");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.SkillGapReportId, item.SkillId }).IsUnique();
            entity.HasIndex(item => item.SkillId);
            entity.HasIndex(item => item.Status);
            entity.HasIndex(item => item.Priority);
            entity.Property(item => item.CurrentLevel).HasMaxLength(30);
            entity.Property(item => item.RequiredLevel).HasMaxLength(30).IsRequired();
            entity.Property(item => item.Status).HasMaxLength(30).IsRequired();
            entity.Property(item => item.Recommendation).HasMaxLength(2000);
            entity.HasCheckConstraint("CK_skill_gap_report_items_Priority",
                "\"Priority\" >= 1 AND \"Priority\" <= 5");
            entity.HasOne(item => item.SkillGapReport)
                .WithMany()
                .HasForeignKey(item => item.SkillGapReportId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Skill)
                .WithMany()
                .HasForeignKey(item => item.SkillId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Roadmap>(entity =>
        {
            entity.ToTable("roadmaps");
            entity.HasKey(roadmap => roadmap.Id);
            entity.HasIndex(roadmap => new { roadmap.UserId, roadmap.Status });
            entity.HasIndex(roadmap => roadmap.CareerRoleId);
            entity.Property(roadmap => roadmap.Title).HasMaxLength(200).IsRequired();
            entity.Property(roadmap => roadmap.Description).HasMaxLength(2000);
            entity.Property(roadmap => roadmap.Status).HasMaxLength(30).HasDefaultValue("Draft").IsRequired();
            entity.Property(roadmap => roadmap.Progress).HasPrecision(5, 2).HasDefaultValue(0m);
            entity.HasCheckConstraint("CK_roadmaps_Progress",
                "\"Progress\" >= 0 AND \"Progress\" <= 100");
            entity.HasOne(roadmap => roadmap.User)
                .WithMany()
                .HasForeignKey(roadmap => roadmap.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(roadmap => roadmap.CareerRole)
                .WithMany()
                .HasForeignKey(roadmap => roadmap.CareerRoleId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(roadmap => roadmap.SkillGapReport)
                .WithMany()
                .HasForeignKey(roadmap => roadmap.SkillGapReportId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<RoadmapNode>(entity =>
        {
            entity.ToTable("roadmap_nodes");
            entity.HasKey(node => node.Id);
            entity.HasIndex(node => new { node.RoadmapId, node.OrderIndex }).IsUnique();
            entity.HasIndex(node => node.SkillId);
            entity.HasIndex(node => node.LearningResourceId);
            entity.HasIndex(node => node.PrerequisiteNodeId);
            entity.HasIndex(node => node.Status);
            entity.Property(node => node.Title).HasMaxLength(200).IsRequired();
            entity.Property(node => node.Description).HasMaxLength(2000);
            entity.Property(node => node.NodeType).HasMaxLength(30).IsRequired();
            entity.Property(node => node.Status).HasMaxLength(30).HasDefaultValue("NotStarted").IsRequired();
            entity.HasCheckConstraint("CK_roadmap_nodes_EstimatedHours",
                "\"EstimatedHours\" IS NULL OR \"EstimatedHours\" >= 0");
            entity.HasCheckConstraint("CK_roadmap_nodes_Priority",
                "\"Priority\" >= 1 AND \"Priority\" <= 5");
            entity.HasOne(node => node.Roadmap)
                .WithMany()
                .HasForeignKey(node => node.RoadmapId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(node => node.Skill)
                .WithMany()
                .HasForeignKey(node => node.SkillId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(node => node.LearningResource)
                .WithMany()
                .HasForeignKey(node => node.LearningResourceId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(node => node.PrerequisiteNode)
                .WithMany()
                .HasForeignKey(node => node.PrerequisiteNodeId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<LearningResource>(entity =>
        {
            entity.ToTable("learning_resources");
            entity.HasKey(resource => resource.Id);
            entity.HasIndex(resource => resource.SkillId);
            entity.HasIndex(resource => resource.IsActive);
            entity.Property(resource => resource.Title).HasMaxLength(200).IsRequired();
            entity.Property(resource => resource.Url).HasMaxLength(1024).IsRequired();
            entity.Property(resource => resource.StorageObjectName).HasMaxLength(1024);
            entity.Property(resource => resource.ContentType).HasMaxLength(100);
            entity.Property(resource => resource.ResourceType).HasMaxLength(50).IsRequired();
            entity.Property(resource => resource.Difficulty).HasMaxLength(50);
            entity.Property(resource => resource.IsActive).HasDefaultValue(true);
            entity.HasCheckConstraint("CK_learning_resources_EstimatedHours",
                "\"EstimatedHours\" IS NULL OR \"EstimatedHours\" >= 0");
            entity.HasOne(resource => resource.Skill)
                .WithMany()
                .HasForeignKey(resource => resource.SkillId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasData(
                new LearningResource { Id = Guid.Parse("33333333-3333-3333-3333-333333333101"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222101"), Title = "Microsoft Learn: Create web APIs with ASP.NET Core", Url = "https://learn.microsoft.com/en-us/aspnet/core/web-api/", ResourceType = "Documentation", Difficulty = "Beginner", EstimatedHours = 8, IsActive = true, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new LearningResource { Id = Guid.Parse("33333333-3333-3333-3333-333333333102"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222102"), Title = "Microsoft Learn: EF Core modeling", Url = "https://learn.microsoft.com/en-us/ef/core/modeling/", ResourceType = "Documentation", Difficulty = "Beginner", EstimatedHours = 8, IsActive = true, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new LearningResource { Id = Guid.Parse("33333333-3333-3333-3333-333333333103"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222103"), Title = "Microsoft Learn: Authentication and authorization in ASP.NET Core", Url = "https://learn.microsoft.com/en-us/aspnet/core/security/authentication/", ResourceType = "Documentation", Difficulty = "Intermediate", EstimatedHours = 10, IsActive = true, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new LearningResource { Id = Guid.Parse("33333333-3333-3333-3333-333333333104"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222104"), Title = "Microsoft Learn: Unit test C# code", Url = "https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-dotnet-test", ResourceType = "Documentation", Difficulty = "Beginner", EstimatedHours = 6, IsActive = true, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new LearningResource { Id = Guid.Parse("33333333-3333-3333-3333-333333333105"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222105"), Title = "Docker Docs: Get started", Url = "https://docs.docker.com/get-started/", ResourceType = "Documentation", Difficulty = "Beginner", EstimatedHours = 6, IsActive = true, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new LearningResource { Id = Guid.Parse("33333333-3333-3333-3333-333333333106"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222106"), Title = "React Docs: Learn React", Url = "https://react.dev/learn", ResourceType = "Documentation", Difficulty = "Beginner", EstimatedHours = 10, IsActive = true, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new LearningResource { Id = Guid.Parse("33333333-3333-3333-3333-333333333107"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222107"), Title = "TypeScript Handbook", Url = "https://www.typescriptlang.org/docs/handbook/intro.html", ResourceType = "Documentation", Difficulty = "Beginner", EstimatedHours = 8, IsActive = true, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new LearningResource { Id = Guid.Parse("33333333-3333-3333-3333-333333333108"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222108"), Title = "MDN: Responsive design", Url = "https://developer.mozilla.org/en-US/docs/Learn_web_development/Core/CSS_layout/Responsive_Design", ResourceType = "Documentation", Difficulty = "Beginner", EstimatedHours = 6, IsActive = true, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new LearningResource { Id = Guid.Parse("33333333-3333-3333-3333-333333333109"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222109"), Title = "MDN: Fetch API", Url = "https://developer.mozilla.org/en-US/docs/Web/API/Fetch_API/Using_Fetch", ResourceType = "Documentation", Difficulty = "Beginner", EstimatedHours = 5, IsActive = true, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new LearningResource { Id = Guid.Parse("33333333-3333-3333-3333-333333333110"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222110"), Title = "Android Developers: App architecture", Url = "https://developer.android.com/topic/architecture", ResourceType = "Documentation", Difficulty = "Beginner", EstimatedHours = 10, IsActive = true, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new LearningResource { Id = Guid.Parse("33333333-3333-3333-3333-333333333111"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222111"), Title = "GitHub Actions: Learn GitHub Actions", Url = "https://docs.github.com/en/actions/learn-github-actions", ResourceType = "Documentation", Difficulty = "Beginner", EstimatedHours = 6, IsActive = true, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new LearningResource { Id = Guid.Parse("33333333-3333-3333-3333-333333333112"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222112"), Title = "Google Cloud: Cloud Run quickstart", Url = "https://cloud.google.com/run/docs/quickstarts", ResourceType = "Documentation", Difficulty = "Beginner", EstimatedHours = 6, IsActive = true, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new LearningResource { Id = Guid.Parse("33333333-3333-3333-3333-333333333113"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222113"), Title = "PostgreSQL Tutorial", Url = "https://www.postgresql.org/docs/current/tutorial.html", ResourceType = "Documentation", Difficulty = "Beginner", EstimatedHours = 8, IsActive = true, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new LearningResource { Id = Guid.Parse("33333333-3333-3333-3333-333333333114"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222114"), Title = "Microsoft Learn: Data engineering on Microsoft Azure", Url = "https://learn.microsoft.com/en-us/training/paths/data-engineer-azure-databricks/", ResourceType = "Course", Difficulty = "Intermediate", EstimatedHours = 12, IsActive = true, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new LearningResource { Id = Guid.Parse("33333333-3333-3333-3333-333333333115"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222115"), Title = "Playwright Docs: Getting started", Url = "https://playwright.dev/docs/intro", ResourceType = "Documentation", Difficulty = "Beginner", EstimatedHours = 6, IsActive = true, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new LearningResource { Id = Guid.Parse("33333333-3333-3333-3333-333333333116"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222116"), Title = "Google AI for Developers: Gemini API quickstart", Url = "https://ai.google.dev/gemini-api/docs/quickstart", ResourceType = "Documentation", Difficulty = "Beginner", EstimatedHours = 6, IsActive = true, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new LearningResource { Id = Guid.Parse("33333333-3333-3333-3333-333333333117"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222117"), Title = "Google AI for Developers: Prompting strategies", Url = "https://ai.google.dev/gemini-api/docs/prompting-strategies", ResourceType = "Documentation", Difficulty = "Intermediate", EstimatedHours = 6, IsActive = true, CreatedAt = SeededAt, UpdatedAt = SeededAt },
                new LearningResource { Id = Guid.Parse("33333333-3333-3333-3333-333333333118"), SkillId = Guid.Parse("22222222-2222-2222-2222-222222222118"), Title = "GitHub Docs: About READMEs", Url = "https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/about-readmes", ResourceType = "Documentation", Difficulty = "Beginner", EstimatedHours = 4, IsActive = true, CreatedAt = SeededAt, UpdatedAt = SeededAt });
        });

        modelBuilder.Entity<MentorSession>(entity =>
        {
            entity.ToTable("mentor_sessions");
            entity.HasKey(session => session.Id);
            entity.HasIndex(session => new { session.UserId, session.CreatedAt }).IsDescending(false, true);
            entity.Property(session => session.Question).HasColumnType("text").IsRequired();
            entity.Property(session => session.Answer).HasColumnType("text").IsRequired();
            entity.Property(session => session.ContextJson).HasColumnType("jsonb");
            entity.Property(session => session.Model).HasMaxLength(100);
            entity.HasCheckConstraint("CK_mentor_sessions_TokensUsed",
                "\"TokensUsed\" IS NULL OR \"TokensUsed\" >= 0");
            entity.HasOne(session => session.User)
                .WithMany()
                .HasForeignKey(session => session.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GithubRepository>(entity =>
        {
            entity.ToTable("github_repositories");
            entity.HasKey(repository => repository.Id);
            entity.HasIndex(repository => new { repository.UserId, repository.RepoUrl }).IsUnique();
            entity.HasIndex(repository => repository.MainLanguage);
            entity.Property(repository => repository.RepoName).HasMaxLength(200).IsRequired();
            entity.Property(repository => repository.RepoUrl).HasMaxLength(1024).IsRequired();
            entity.Property(repository => repository.Description).HasMaxLength(2000);
            entity.Property(repository => repository.MainLanguage).HasMaxLength(100);
            entity.Property(repository => repository.ReadmeContent).HasColumnType("text");
            entity.Property(repository => repository.AiSummary).HasColumnType("text");
            entity.Property(repository => repository.TechStackJson).HasColumnType("jsonb");
            entity.Property(repository => repository.QualityScore).HasPrecision(5, 2);
            entity.HasCheckConstraint("CK_github_repositories_QualityScore",
                "\"QualityScore\" IS NULL OR (\"QualityScore\" >= 0 AND \"QualityScore\" <= 100)");
            entity.HasOne(repository => repository.User)
                .WithMany()
                .HasForeignKey(repository => repository.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GithubRepositorySkill>(entity =>
        {
            entity.ToTable("github_repository_skills");
            entity.HasKey(repositorySkill => repositorySkill.Id);
            entity.HasIndex(repositorySkill => new { repositorySkill.GithubRepositoryId, repositorySkill.SkillId }).IsUnique();
            entity.HasIndex(repositorySkill => repositorySkill.SkillId);
            entity.Property(repositorySkill => repositorySkill.ConfidenceScore).HasPrecision(5, 2);
            entity.Property(repositorySkill => repositorySkill.EvidenceText).HasMaxLength(2000);
            entity.HasCheckConstraint("CK_github_repository_skills_ConfidenceScore",
                "\"ConfidenceScore\" IS NULL OR (\"ConfidenceScore\" >= 0 AND \"ConfidenceScore\" <= 100)");
            entity.HasOne(repositorySkill => repositorySkill.GithubRepository)
                .WithMany()
                .HasForeignKey(repositorySkill => repositorySkill.GithubRepositoryId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(repositorySkill => repositorySkill.Skill)
                .WithMany()
                .HasForeignKey(repositorySkill => repositorySkill.SkillId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GithubConnection>(entity =>
        {
            entity.ToTable("github_connections");
            entity.HasKey(connection => connection.Id);
            entity.HasIndex(connection => connection.UserId).IsUnique();
            entity.HasIndex(connection => connection.GithubUsername);
            entity.Property(connection => connection.GithubUsername).HasMaxLength(100).IsRequired();
            entity.Property(connection => connection.AccessToken).HasColumnType("text").IsRequired();
            entity.Property(connection => connection.TokenType).HasMaxLength(50).IsRequired();
            entity.Property(connection => connection.Scope).HasMaxLength(500);
            entity.HasOne(connection => connection.User)
                .WithMany()
                .HasForeignKey(connection => connection.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GithubOAuthState>(entity =>
        {
            entity.ToTable("github_oauth_states");
            entity.HasKey(state => state.State);
            entity.HasIndex(state => state.UserId);
            entity.HasIndex(state => state.ExpiresAt);
            entity.Property(state => state.State).HasMaxLength(128).IsRequired();
            entity.Property(state => state.ReturnUrl).HasMaxLength(2048);
            entity.HasOne(state => state.User)
                .WithMany()
                .HasForeignKey(state => state.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Portfolio>(entity =>
        {
            entity.ToTable("portfolios");
            entity.HasKey(portfolio => portfolio.Id);
            entity.HasIndex(portfolio => portfolio.Slug).IsUnique();
            entity.HasIndex(portfolio => new { portfolio.UserId, portfolio.IsPublished });
            entity.Property(portfolio => portfolio.Slug).HasMaxLength(150).IsRequired();
            entity.Property(portfolio => portfolio.Title).HasMaxLength(200).IsRequired();
            entity.Property(portfolio => portfolio.Bio).HasMaxLength(2000);
            entity.Property(portfolio => portfolio.Theme).HasMaxLength(80).HasDefaultValue("Default");
            entity.Property(portfolio => portfolio.IsPublished).HasDefaultValue(false);
            entity.HasOne(portfolio => portfolio.User)
                .WithMany()
                .HasForeignKey(portfolio => portfolio.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PortfolioProject>(entity =>
        {
            entity.ToTable("portfolio_projects");
            entity.HasKey(project => project.Id);
            entity.HasIndex(project => new { project.PortfolioId, project.OrderIndex });
            entity.HasIndex(project => project.GithubRepositoryId);
            entity.Property(project => project.Title).HasMaxLength(200).IsRequired();
            entity.Property(project => project.Description).HasMaxLength(3000);
            entity.Property(project => project.TechStackJson).HasColumnType("jsonb");
            entity.Property(project => project.ImageUrl).HasMaxLength(1024);
            entity.Property(project => project.DemoUrl).HasMaxLength(1024);
            entity.Property(project => project.SourceUrl).HasMaxLength(1024);
            entity.Property(project => project.OrderIndex).HasDefaultValue(0);
            entity.HasOne(project => project.Portfolio)
                .WithMany()
                .HasForeignKey(project => project.PortfolioId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(project => project.GithubRepository)
                .WithMany()
                .HasForeignKey(project => project.GithubRepositoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SubscriptionPlan>(entity =>
        {
            entity.ToTable("subscription_plans");
            entity.HasKey(plan => plan.Id);
            entity.HasIndex(plan => plan.Name).IsUnique();
            entity.HasIndex(plan => plan.IsActive);
            entity.Property(plan => plan.Name).HasMaxLength(100).IsRequired();
            entity.Property(plan => plan.Description).HasMaxLength(2000);
            entity.Property(plan => plan.Price).HasPrecision(18, 2).HasDefaultValue(0m);
            entity.Property(plan => plan.Currency).HasMaxLength(10).IsRequired();
            entity.Property(plan => plan.BillingCycle).HasMaxLength(30).IsRequired();
            entity.Property(plan => plan.FeaturesJson).HasColumnType("jsonb");
            entity.Property(plan => plan.IsActive).HasDefaultValue(true);

            entity.HasData(
                new SubscriptionPlan
                {
                    Id = Guid.Parse("44444444-4444-4444-4444-444444444101"),
                    Name = "Free",
                    Description = "Free access with 2 mentor reviews.",
                    Price = 0m,
                    Currency = "VND",
                    BillingCycle = "Free",
                    FeaturesJson = """{"mentorReviewLimit":2,"features":["2 mentor reviews","Basic roadmap resources"]}""",
                    IsActive = true,
                    CreatedAt = SeededAt,
                    UpdatedAt = SeededAt
                },
                new SubscriptionPlan
                {
                    Id = Guid.Parse("44444444-4444-4444-4444-444444444102"),
                    Name = "Premium Monthly",
                    Description = "Monthly access with 5 mentor reviews.",
                    Price = 99000m,
                    Currency = "VND",
                    BillingCycle = "Monthly",
                    FeaturesJson = """{"mentorReviewLimit":5,"features":["5 mentor reviews per month","AI mentor chat","GitHub analysis","Roadmap resources"]}""",
                    IsActive = true,
                    CreatedAt = SeededAt,
                    UpdatedAt = SeededAt
                });
        });

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.ToTable("subscriptions");
            entity.HasKey(subscription => subscription.Id);
            entity.HasIndex(subscription => new { subscription.UserId, subscription.Status });
            entity.HasIndex(subscription => subscription.ExpiredAt);
            entity.HasIndex(subscription => subscription.ProviderSubscriptionId);
            entity.Property(subscription => subscription.Status).HasMaxLength(30).IsRequired();
            entity.Property(subscription => subscription.Provider).HasMaxLength(50);
            entity.Property(subscription => subscription.ProviderSubscriptionId).HasMaxLength(200);
            entity.HasOne(subscription => subscription.User)
                .WithMany()
                .HasForeignKey(subscription => subscription.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(subscription => subscription.Plan)
                .WithMany()
                .HasForeignKey(subscription => subscription.PlanId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.ToTable("payment_transactions");
            entity.HasKey(transaction => transaction.Id);
            entity.HasIndex(transaction => new { transaction.UserId, transaction.CreatedAt }).IsDescending(false, true);
            entity.HasIndex(transaction => transaction.SubscriptionId);
            entity.HasIndex(transaction => transaction.PlanId);
            entity.HasIndex(transaction => transaction.Status);
            entity.HasIndex(transaction => transaction.Provider);
            entity.HasIndex(transaction => transaction.ProviderTransactionId).IsUnique();
            entity.Property(transaction => transaction.Amount).HasPrecision(18, 2);
            entity.Property(transaction => transaction.Currency).HasMaxLength(10).IsRequired();
            entity.Property(transaction => transaction.Status).HasMaxLength(30).IsRequired();
            entity.Property(transaction => transaction.Provider).HasMaxLength(50).IsRequired();
            entity.Property(transaction => transaction.ProviderTransactionId).HasMaxLength(200);
            entity.Property(transaction => transaction.CheckoutUrl).HasMaxLength(2048);
            entity.HasOne(transaction => transaction.User)
                .WithMany()
                .HasForeignKey(transaction => transaction.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(transaction => transaction.Subscription)
                .WithMany()
                .HasForeignKey(transaction => transaction.SubscriptionId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(transaction => transaction.Plan)
                .WithMany()
                .HasForeignKey(transaction => transaction.PlanId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PaymentWebhookEvent>(entity =>
        {
            entity.ToTable("payment_webhook_events");
            entity.HasKey(webhookEvent => webhookEvent.Id);
            entity.HasIndex(webhookEvent => new { webhookEvent.Provider, webhookEvent.EventId }).IsUnique();
            entity.HasIndex(webhookEvent => webhookEvent.EventType);
            entity.Property(webhookEvent => webhookEvent.Provider).HasMaxLength(50).IsRequired();
            entity.Property(webhookEvent => webhookEvent.EventId).HasMaxLength(200).IsRequired();
            entity.Property(webhookEvent => webhookEvent.EventType).HasMaxLength(100).IsRequired();
            entity.Property(webhookEvent => webhookEvent.PayloadJson).HasColumnType("jsonb").IsRequired();
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.ToTable("invoices");
            entity.HasKey(invoice => invoice.Id);
            entity.HasIndex(invoice => invoice.UserId);
            entity.HasIndex(invoice => invoice.PaymentTransactionId).IsUnique();
            entity.HasIndex(invoice => invoice.InvoiceNumber).IsUnique();
            entity.Property(invoice => invoice.InvoiceNumber).HasMaxLength(100).IsRequired();
            entity.Property(invoice => invoice.Amount).HasPrecision(18, 2);
            entity.Property(invoice => invoice.Currency).HasMaxLength(10).IsRequired();
            entity.Property(invoice => invoice.PdfUrl).HasMaxLength(1024);
            entity.HasOne(invoice => invoice.User)
                .WithMany()
                .HasForeignKey(invoice => invoice.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(invoice => invoice.PaymentTransaction)
                .WithOne()
                .HasForeignKey<Invoice>(invoice => invoice.PaymentTransactionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Coupon>(entity =>
        {
            entity.ToTable("coupons");
            entity.HasKey(coupon => coupon.Id);
            entity.HasIndex(coupon => coupon.Code).IsUnique();
            entity.HasIndex(coupon => coupon.ExpiredAt);
            entity.HasIndex(coupon => coupon.IsActive);
            entity.Property(coupon => coupon.Code).HasMaxLength(80).IsRequired();
            entity.Property(coupon => coupon.DiscountType).HasMaxLength(30).IsRequired();
            entity.Property(coupon => coupon.DiscountValue).HasPrecision(18, 2);
            entity.Property(coupon => coupon.UsedCount).HasDefaultValue(0);
            entity.Property(coupon => coupon.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<MentorFeedback>(entity =>
        {
            entity.ToTable("mentor_feedbacks");
            entity.HasKey(feedback => feedback.Id);
            entity.HasIndex(feedback => feedback.MentorId);
            entity.HasIndex(feedback => feedback.StudentId);
            entity.HasIndex(feedback => feedback.PortfolioId);
            entity.HasIndex(feedback => feedback.GithubRepositoryId);
            entity.Property(feedback => feedback.Comment).HasColumnType("text").IsRequired();
            entity.HasCheckConstraint("CK_mentor_feedbacks_Rating",
                "\"Rating\" IS NULL OR (\"Rating\" >= 1 AND \"Rating\" <= 5)");
            entity.HasOne(feedback => feedback.Mentor)
                .WithMany()
                .HasForeignKey(feedback => feedback.MentorId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(feedback => feedback.Student)
                .WithMany()
                .HasForeignKey(feedback => feedback.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(feedback => feedback.Portfolio)
                .WithMany()
                .HasForeignKey(feedback => feedback.PortfolioId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(feedback => feedback.GithubRepository)
                .WithMany()
                .HasForeignKey(feedback => feedback.GithubRepositoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<CounselorFeedback>(entity =>
        {
            entity.ToTable("counselor_feedbacks");
            entity.HasKey(feedback => feedback.Id);
            entity.HasIndex(feedback => feedback.CounselorId);
            entity.HasIndex(feedback => feedback.StudentId);
            entity.HasIndex(feedback => feedback.RoadmapId);
            entity.HasIndex(feedback => feedback.SkillGapReportId);
            entity.Property(feedback => feedback.Comment).HasColumnType("text").IsRequired();
            entity.HasOne(feedback => feedback.Counselor)
                .WithMany()
                .HasForeignKey(feedback => feedback.CounselorId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(feedback => feedback.Student)
                .WithMany()
                .HasForeignKey(feedback => feedback.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(feedback => feedback.Roadmap)
                .WithMany()
                .HasForeignKey(feedback => feedback.RoadmapId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(feedback => feedback.SkillGapReport)
                .WithMany()
                .HasForeignKey(feedback => feedback.SkillGapReportId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
