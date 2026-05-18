CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260512052555_InitialCreate') THEN
    CREATE TABLE users (
        "Id" uuid NOT NULL,
        "Email" character varying(256) NOT NULL,
        "FullName" character varying(200) NOT NULL,
        "AvatarUrl" character varying(1024),
        "GoogleSubject" character varying(128),
        "Role" character varying(50) NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_users" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260512052555_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_users_Email" ON users ("Email");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260512052555_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_users_GoogleSubject" ON users ("GoogleSubject");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260512052555_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260512052555_InitialCreate', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260513145811_AddPasswordLogin') THEN
    ALTER TABLE users ADD "PasswordHash" character varying(512);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260513145811_AddPasswordLogin') THEN
    ALTER TABLE users ADD "Username" character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260513145811_AddPasswordLogin') THEN
    CREATE UNIQUE INDEX "IX_users_Username" ON users ("Username");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260513145811_AddPasswordLogin') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260513145811_AddPasswordLogin', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260513152421_AddEmailOtpVerification') THEN
    ALTER TABLE users ADD "EmailVerificationOtpExpiresAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260513152421_AddEmailOtpVerification') THEN
    ALTER TABLE users ADD "EmailVerificationOtpHash" character varying(512);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260513152421_AddEmailOtpVerification') THEN
    ALTER TABLE users ADD "EmailVerifiedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260513152421_AddEmailOtpVerification') THEN
    ALTER TABLE users ADD "IsEmailVerified" boolean NOT NULL DEFAULT TRUE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260513152421_AddEmailOtpVerification') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260513152421_AddEmailOtpVerification', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    ALTER TABLE users ALTER COLUMN "Role" SET DEFAULT 'Student';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    ALTER TABLE users ALTER COLUMN "IsActive" SET DEFAULT TRUE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE TABLE career_roles (
        "Id" uuid NOT NULL,
        "Name" character varying(150) NOT NULL,
        "Description" character varying(2000),
        "Level" character varying(50),
        "IsActive" boolean NOT NULL DEFAULT TRUE,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_career_roles" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE TABLE coupons (
        "Id" uuid NOT NULL,
        "Code" character varying(80) NOT NULL,
        "DiscountType" character varying(30) NOT NULL,
        "DiscountValue" numeric(18,2) NOT NULL,
        "MaxUsage" integer,
        "UsedCount" integer NOT NULL DEFAULT 0,
        "ExpiredAt" timestamp with time zone,
        "IsActive" boolean NOT NULL DEFAULT TRUE,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_coupons" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE TABLE github_repositories (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "RepoName" character varying(200) NOT NULL,
        "RepoUrl" character varying(1024) NOT NULL,
        "Description" character varying(2000),
        "MainLanguage" character varying(100),
        "ReadmeContent" text,
        "AiSummary" text,
        "TechStackJson" jsonb,
        "QualityScore" numeric(5,2),
        "LastSyncedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_github_repositories" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_github_repositories_QualityScore" CHECK ("QualityScore" IS NULL OR ("QualityScore" >= 0 AND "QualityScore" <= 100)),
        CONSTRAINT "FK_github_repositories_users_UserId" FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE TABLE mentor_sessions (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "Question" text NOT NULL,
        "Answer" text NOT NULL,
        "ContextJson" jsonb,
        "Model" character varying(100),
        "TokensUsed" integer,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_mentor_sessions" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_mentor_sessions_TokensUsed" CHECK ("TokensUsed" IS NULL OR "TokensUsed" >= 0),
        CONSTRAINT "FK_mentor_sessions_users_UserId" FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE TABLE payment_webhook_events (
        "Id" uuid NOT NULL,
        "Provider" character varying(50) NOT NULL,
        "EventId" character varying(200) NOT NULL,
        "EventType" character varying(100) NOT NULL,
        "PayloadJson" jsonb NOT NULL,
        "ProcessedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_payment_webhook_events" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE TABLE portfolios (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "Slug" character varying(150) NOT NULL,
        "Title" character varying(200) NOT NULL,
        "Bio" character varying(2000),
        "Theme" character varying(80) DEFAULT 'Default',
        "IsPublished" boolean NOT NULL DEFAULT FALSE,
        "PublishedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_portfolios" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_portfolios_users_UserId" FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE TABLE skills (
        "Id" uuid NOT NULL,
        "Name" character varying(150) NOT NULL,
        "Category" character varying(80) NOT NULL,
        "Description" character varying(1000),
        "IsActive" boolean NOT NULL DEFAULT TRUE,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_skills" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE TABLE subscription_plans (
        "Id" uuid NOT NULL,
        "Name" character varying(100) NOT NULL,
        "Description" character varying(2000),
        "Price" numeric(18,2) NOT NULL DEFAULT 0.0,
        "Currency" character varying(10) NOT NULL,
        "BillingCycle" character varying(30) NOT NULL,
        "FeaturesJson" jsonb,
        "IsActive" boolean NOT NULL DEFAULT TRUE,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_subscription_plans" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE TABLE skill_gap_reports (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "CareerRoleId" uuid NOT NULL,
        "MatchScore" numeric(5,2) NOT NULL,
        "Summary" character varying(4000),
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_skill_gap_reports" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_skill_gap_reports_MatchScore" CHECK ("MatchScore" >= 0 AND "MatchScore" <= 100),
        CONSTRAINT "FK_skill_gap_reports_career_roles_CareerRoleId" FOREIGN KEY ("CareerRoleId") REFERENCES career_roles ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_skill_gap_reports_users_UserId" FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE TABLE student_profiles (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "School" character varying(200),
        "Major" character varying(200),
        "Year" integer,
        "Gpa" numeric(4,2),
        "TargetRoleId" uuid,
        "GithubUsername" character varying(100),
        "CareerGoal" character varying(1000),
        "PreferredLearningHoursPerWeek" integer,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_student_profiles" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_student_profiles_Gpa" CHECK ("Gpa" IS NULL OR "Gpa" >= 0),
        CONSTRAINT "CK_student_profiles_PreferredLearningHoursPerWeek" CHECK ("PreferredLearningHoursPerWeek" IS NULL OR "PreferredLearningHoursPerWeek" >= 0),
        CONSTRAINT "CK_student_profiles_Year" CHECK ("Year" IS NULL OR ("Year" >= 1 AND "Year" <= 8)),
        CONSTRAINT "FK_student_profiles_career_roles_TargetRoleId" FOREIGN KEY ("TargetRoleId") REFERENCES career_roles ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_student_profiles_users_UserId" FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE TABLE mentor_feedbacks (
        "Id" uuid NOT NULL,
        "MentorId" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "PortfolioId" uuid,
        "GithubRepositoryId" uuid,
        "Comment" text NOT NULL,
        "Rating" integer,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_mentor_feedbacks" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_mentor_feedbacks_Rating" CHECK ("Rating" IS NULL OR ("Rating" >= 1 AND "Rating" <= 5)),
        CONSTRAINT "FK_mentor_feedbacks_github_repositories_GithubRepositoryId" FOREIGN KEY ("GithubRepositoryId") REFERENCES github_repositories ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_mentor_feedbacks_portfolios_PortfolioId" FOREIGN KEY ("PortfolioId") REFERENCES portfolios ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_mentor_feedbacks_users_MentorId" FOREIGN KEY ("MentorId") REFERENCES users ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_mentor_feedbacks_users_StudentId" FOREIGN KEY ("StudentId") REFERENCES users ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE TABLE portfolio_projects (
        "Id" uuid NOT NULL,
        "PortfolioId" uuid NOT NULL,
        "GithubRepositoryId" uuid,
        "Title" character varying(200) NOT NULL,
        "Description" character varying(3000),
        "TechStackJson" jsonb,
        "DemoUrl" character varying(1024),
        "SourceUrl" character varying(1024),
        "OrderIndex" integer NOT NULL DEFAULT 0,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_portfolio_projects" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_portfolio_projects_github_repositories_GithubRepositoryId" FOREIGN KEY ("GithubRepositoryId") REFERENCES github_repositories ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_portfolio_projects_portfolios_PortfolioId" FOREIGN KEY ("PortfolioId") REFERENCES portfolios ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE TABLE github_repository_skills (
        "Id" uuid NOT NULL,
        "GithubRepositoryId" uuid NOT NULL,
        "SkillId" uuid NOT NULL,
        "ConfidenceScore" numeric(5,2),
        "EvidenceText" character varying(2000),
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_github_repository_skills" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_github_repository_skills_ConfidenceScore" CHECK ("ConfidenceScore" IS NULL OR ("ConfidenceScore" >= 0 AND "ConfidenceScore" <= 100)),
        CONSTRAINT "FK_github_repository_skills_github_repositories_GithubReposito~" FOREIGN KEY ("GithubRepositoryId") REFERENCES github_repositories ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_github_repository_skills_skills_SkillId" FOREIGN KEY ("SkillId") REFERENCES skills ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE TABLE learning_resources (
        "Id" uuid NOT NULL,
        "SkillId" uuid,
        "Title" character varying(200) NOT NULL,
        "Url" character varying(1024) NOT NULL,
        "ResourceType" character varying(50) NOT NULL,
        "Difficulty" character varying(50),
        "EstimatedHours" integer,
        "IsActive" boolean NOT NULL DEFAULT TRUE,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_learning_resources" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_learning_resources_EstimatedHours" CHECK ("EstimatedHours" IS NULL OR "EstimatedHours" >= 0),
        CONSTRAINT "FK_learning_resources_skills_SkillId" FOREIGN KEY ("SkillId") REFERENCES skills ("Id") ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE TABLE role_skill_requirements (
        "Id" uuid NOT NULL,
        "CareerRoleId" uuid NOT NULL,
        "SkillId" uuid NOT NULL,
        "RequiredLevel" character varying(30) NOT NULL,
        "Priority" integer NOT NULL,
        "Weight" numeric(5,2) NOT NULL DEFAULT 1.0,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_role_skill_requirements" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_role_skill_requirements_Priority" CHECK ("Priority" >= 1 AND "Priority" <= 5),
        CONSTRAINT "CK_role_skill_requirements_Weight" CHECK ("Weight" > 0),
        CONSTRAINT "FK_role_skill_requirements_career_roles_CareerRoleId" FOREIGN KEY ("CareerRoleId") REFERENCES career_roles ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_role_skill_requirements_skills_SkillId" FOREIGN KEY ("SkillId") REFERENCES skills ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE TABLE user_skills (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "SkillId" uuid NOT NULL,
        "Level" character varying(30) NOT NULL,
        "EvidenceUrl" character varying(1024),
        "EvidenceType" character varying(50),
        "IsVerified" boolean NOT NULL DEFAULT FALSE,
        "VerifiedByUserId" uuid,
        "VerifiedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_user_skills" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_user_skills_skills_SkillId" FOREIGN KEY ("SkillId") REFERENCES skills ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_user_skills_users_UserId" FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_user_skills_users_VerifiedByUserId" FOREIGN KEY ("VerifiedByUserId") REFERENCES users ("Id") ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE TABLE subscriptions (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "PlanId" uuid NOT NULL,
        "Status" character varying(30) NOT NULL,
        "StartedAt" timestamp with time zone,
        "ExpiredAt" timestamp with time zone,
        "CancelledAt" timestamp with time zone,
        "Provider" character varying(50),
        "ProviderSubscriptionId" character varying(200),
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_subscriptions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_subscriptions_subscription_plans_PlanId" FOREIGN KEY ("PlanId") REFERENCES subscription_plans ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_subscriptions_users_UserId" FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE TABLE roadmaps (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "CareerRoleId" uuid NOT NULL,
        "SkillGapReportId" uuid,
        "Title" character varying(200) NOT NULL,
        "Description" character varying(2000),
        "Status" character varying(30) NOT NULL DEFAULT 'Draft',
        "Progress" numeric(5,2) NOT NULL DEFAULT 0.0,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_roadmaps" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_roadmaps_Progress" CHECK ("Progress" >= 0 AND "Progress" <= 100),
        CONSTRAINT "FK_roadmaps_career_roles_CareerRoleId" FOREIGN KEY ("CareerRoleId") REFERENCES career_roles ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_roadmaps_skill_gap_reports_SkillGapReportId" FOREIGN KEY ("SkillGapReportId") REFERENCES skill_gap_reports ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_roadmaps_users_UserId" FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE TABLE skill_gap_report_items (
        "Id" uuid NOT NULL,
        "SkillGapReportId" uuid NOT NULL,
        "SkillId" uuid NOT NULL,
        "CurrentLevel" character varying(30),
        "RequiredLevel" character varying(30) NOT NULL,
        "Status" character varying(30) NOT NULL,
        "Priority" integer NOT NULL,
        "Recommendation" character varying(2000),
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_skill_gap_report_items" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_skill_gap_report_items_Priority" CHECK ("Priority" >= 1 AND "Priority" <= 5),
        CONSTRAINT "FK_skill_gap_report_items_skill_gap_reports_SkillGapReportId" FOREIGN KEY ("SkillGapReportId") REFERENCES skill_gap_reports ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_skill_gap_report_items_skills_SkillId" FOREIGN KEY ("SkillId") REFERENCES skills ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE TABLE payment_transactions (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "SubscriptionId" uuid,
        "PlanId" uuid NOT NULL,
        "Amount" numeric(18,2) NOT NULL,
        "Currency" character varying(10) NOT NULL,
        "Status" character varying(30) NOT NULL,
        "Provider" character varying(50) NOT NULL,
        "ProviderTransactionId" character varying(200),
        "CheckoutUrl" character varying(2048),
        "PaidAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_payment_transactions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_payment_transactions_subscription_plans_PlanId" FOREIGN KEY ("PlanId") REFERENCES subscription_plans ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_payment_transactions_subscriptions_SubscriptionId" FOREIGN KEY ("SubscriptionId") REFERENCES subscriptions ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_payment_transactions_users_UserId" FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE TABLE counselor_feedbacks (
        "Id" uuid NOT NULL,
        "CounselorId" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "RoadmapId" uuid,
        "SkillGapReportId" uuid,
        "Comment" text NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_counselor_feedbacks" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_counselor_feedbacks_roadmaps_RoadmapId" FOREIGN KEY ("RoadmapId") REFERENCES roadmaps ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_counselor_feedbacks_skill_gap_reports_SkillGapReportId" FOREIGN KEY ("SkillGapReportId") REFERENCES skill_gap_reports ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_counselor_feedbacks_users_CounselorId" FOREIGN KEY ("CounselorId") REFERENCES users ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_counselor_feedbacks_users_StudentId" FOREIGN KEY ("StudentId") REFERENCES users ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE TABLE roadmap_nodes (
        "Id" uuid NOT NULL,
        "RoadmapId" uuid NOT NULL,
        "SkillId" uuid,
        "LearningResourceId" uuid,
        "Title" character varying(200) NOT NULL,
        "Description" character varying(2000),
        "NodeType" character varying(30) NOT NULL,
        "Status" character varying(30) NOT NULL DEFAULT 'NotStarted',
        "OrderIndex" integer NOT NULL,
        "EstimatedHours" integer,
        "Priority" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_roadmap_nodes" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_roadmap_nodes_EstimatedHours" CHECK ("EstimatedHours" IS NULL OR "EstimatedHours" >= 0),
        CONSTRAINT "CK_roadmap_nodes_Priority" CHECK ("Priority" >= 1 AND "Priority" <= 5),
        CONSTRAINT "FK_roadmap_nodes_learning_resources_LearningResourceId" FOREIGN KEY ("LearningResourceId") REFERENCES learning_resources ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_roadmap_nodes_roadmaps_RoadmapId" FOREIGN KEY ("RoadmapId") REFERENCES roadmaps ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_roadmap_nodes_skills_SkillId" FOREIGN KEY ("SkillId") REFERENCES skills ("Id") ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE TABLE invoices (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "PaymentTransactionId" uuid NOT NULL,
        "InvoiceNumber" character varying(100) NOT NULL,
        "Amount" numeric(18,2) NOT NULL,
        "Currency" character varying(10) NOT NULL,
        "IssuedAt" timestamp with time zone NOT NULL,
        "PdfUrl" character varying(1024),
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_invoices" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_invoices_payment_transactions_PaymentTransactionId" FOREIGN KEY ("PaymentTransactionId") REFERENCES payment_transactions ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_invoices_users_UserId" FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_users_IsActive" ON users ("IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_users_Role" ON users ("Role");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_career_roles_IsActive" ON career_roles ("IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE UNIQUE INDEX "IX_career_roles_Name" ON career_roles ("Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_counselor_feedbacks_CounselorId" ON counselor_feedbacks ("CounselorId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_counselor_feedbacks_RoadmapId" ON counselor_feedbacks ("RoadmapId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_counselor_feedbacks_SkillGapReportId" ON counselor_feedbacks ("SkillGapReportId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_counselor_feedbacks_StudentId" ON counselor_feedbacks ("StudentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE UNIQUE INDEX "IX_coupons_Code" ON coupons ("Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_coupons_ExpiredAt" ON coupons ("ExpiredAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_coupons_IsActive" ON coupons ("IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_github_repositories_MainLanguage" ON github_repositories ("MainLanguage");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE UNIQUE INDEX "IX_github_repositories_UserId_RepoUrl" ON github_repositories ("UserId", "RepoUrl");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE UNIQUE INDEX "IX_github_repository_skills_GithubRepositoryId_SkillId" ON github_repository_skills ("GithubRepositoryId", "SkillId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_github_repository_skills_SkillId" ON github_repository_skills ("SkillId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE UNIQUE INDEX "IX_invoices_InvoiceNumber" ON invoices ("InvoiceNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE UNIQUE INDEX "IX_invoices_PaymentTransactionId" ON invoices ("PaymentTransactionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_invoices_UserId" ON invoices ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_learning_resources_IsActive" ON learning_resources ("IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_learning_resources_SkillId" ON learning_resources ("SkillId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_mentor_feedbacks_GithubRepositoryId" ON mentor_feedbacks ("GithubRepositoryId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_mentor_feedbacks_MentorId" ON mentor_feedbacks ("MentorId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_mentor_feedbacks_PortfolioId" ON mentor_feedbacks ("PortfolioId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_mentor_feedbacks_StudentId" ON mentor_feedbacks ("StudentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_mentor_sessions_UserId_CreatedAt" ON mentor_sessions ("UserId", "CreatedAt" DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_payment_transactions_PlanId" ON payment_transactions ("PlanId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_payment_transactions_Provider" ON payment_transactions ("Provider");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE UNIQUE INDEX "IX_payment_transactions_ProviderTransactionId" ON payment_transactions ("ProviderTransactionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_payment_transactions_Status" ON payment_transactions ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_payment_transactions_SubscriptionId" ON payment_transactions ("SubscriptionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_payment_transactions_UserId_CreatedAt" ON payment_transactions ("UserId", "CreatedAt" DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_payment_webhook_events_EventType" ON payment_webhook_events ("EventType");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE UNIQUE INDEX "IX_payment_webhook_events_Provider_EventId" ON payment_webhook_events ("Provider", "EventId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_portfolio_projects_GithubRepositoryId" ON portfolio_projects ("GithubRepositoryId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_portfolio_projects_PortfolioId_OrderIndex" ON portfolio_projects ("PortfolioId", "OrderIndex");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE UNIQUE INDEX "IX_portfolios_Slug" ON portfolios ("Slug");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_portfolios_UserId_IsPublished" ON portfolios ("UserId", "IsPublished");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_roadmap_nodes_LearningResourceId" ON roadmap_nodes ("LearningResourceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE UNIQUE INDEX "IX_roadmap_nodes_RoadmapId_OrderIndex" ON roadmap_nodes ("RoadmapId", "OrderIndex");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_roadmap_nodes_SkillId" ON roadmap_nodes ("SkillId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_roadmap_nodes_Status" ON roadmap_nodes ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_roadmaps_CareerRoleId" ON roadmaps ("CareerRoleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_roadmaps_SkillGapReportId" ON roadmaps ("SkillGapReportId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_roadmaps_UserId_Status" ON roadmaps ("UserId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE UNIQUE INDEX "IX_role_skill_requirements_CareerRoleId_SkillId" ON role_skill_requirements ("CareerRoleId", "SkillId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_role_skill_requirements_Priority" ON role_skill_requirements ("Priority");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_role_skill_requirements_SkillId" ON role_skill_requirements ("SkillId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_skill_gap_report_items_Priority" ON skill_gap_report_items ("Priority");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE UNIQUE INDEX "IX_skill_gap_report_items_SkillGapReportId_SkillId" ON skill_gap_report_items ("SkillGapReportId", "SkillId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_skill_gap_report_items_SkillId" ON skill_gap_report_items ("SkillId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_skill_gap_report_items_Status" ON skill_gap_report_items ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_skill_gap_reports_CareerRoleId" ON skill_gap_reports ("CareerRoleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_skill_gap_reports_UserId_CreatedAt" ON skill_gap_reports ("UserId", "CreatedAt" DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_skills_Category" ON skills ("Category");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_skills_IsActive" ON skills ("IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE UNIQUE INDEX "IX_skills_Name_Category" ON skills ("Name", "Category");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_student_profiles_GithubUsername" ON student_profiles ("GithubUsername");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_student_profiles_TargetRoleId" ON student_profiles ("TargetRoleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE UNIQUE INDEX "IX_student_profiles_UserId" ON student_profiles ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_subscription_plans_IsActive" ON subscription_plans ("IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE UNIQUE INDEX "IX_subscription_plans_Name" ON subscription_plans ("Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_subscriptions_ExpiredAt" ON subscriptions ("ExpiredAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_subscriptions_PlanId" ON subscriptions ("PlanId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_subscriptions_ProviderSubscriptionId" ON subscriptions ("ProviderSubscriptionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_subscriptions_UserId_Status" ON subscriptions ("UserId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_user_skills_IsVerified" ON user_skills ("IsVerified");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_user_skills_Level" ON user_skills ("Level");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_user_skills_SkillId" ON user_skills ("SkillId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE UNIQUE INDEX "IX_user_skills_UserId_SkillId" ON user_skills ("UserId", "SkillId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    CREATE INDEX "IX_user_skills_VerifiedByUserId" ON user_skills ("VerifiedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515143511_AddCareerPlatformSchema') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260515143511_AddCareerPlatformSchema', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515144108_SeedCareerRoles') THEN
    INSERT INTO career_roles ("Id", "CreatedAt", "Description", "IsActive", "Level", "Name", "UpdatedAt")
    VALUES ('11111111-1111-1111-1111-111111111101', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Builds server-side APIs, databases, authentication, and backend services.', TRUE, 'Fresher', 'Backend Developer', TIMESTAMPTZ '2026-05-15T00:00:00+00:00');
    INSERT INTO career_roles ("Id", "CreatedAt", "Description", "IsActive", "Level", "Name", "UpdatedAt")
    VALUES ('11111111-1111-1111-1111-111111111102', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Builds user interfaces, client-side application logic, and responsive web experiences.', TRUE, 'Fresher', 'Frontend Developer', TIMESTAMPTZ '2026-05-15T00:00:00+00:00');
    INSERT INTO career_roles ("Id", "CreatedAt", "Description", "IsActive", "Level", "Name", "UpdatedAt")
    VALUES ('11111111-1111-1111-1111-111111111103', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Builds both frontend interfaces and backend APIs for complete web applications.', TRUE, 'Fresher', 'Fullstack Developer', TIMESTAMPTZ '2026-05-15T00:00:00+00:00');
    INSERT INTO career_roles ("Id", "CreatedAt", "Description", "IsActive", "Level", "Name", "UpdatedAt")
    VALUES ('11111111-1111-1111-1111-111111111104', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Builds mobile applications and integrates them with backend services.', TRUE, 'Fresher', 'Mobile Developer', TIMESTAMPTZ '2026-05-15T00:00:00+00:00');
    INSERT INTO career_roles ("Id", "CreatedAt", "Description", "IsActive", "Level", "Name", "UpdatedAt")
    VALUES ('11111111-1111-1111-1111-111111111105', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Works on CI/CD, deployment automation, infrastructure, monitoring, and reliability.', TRUE, 'Fresher', 'DevOps Engineer', TIMESTAMPTZ '2026-05-15T00:00:00+00:00');
    INSERT INTO career_roles ("Id", "CreatedAt", "Description", "IsActive", "Level", "Name", "UpdatedAt")
    VALUES ('11111111-1111-1111-1111-111111111106', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Builds data pipelines, data storage, processing jobs, and analytics infrastructure.', TRUE, 'Fresher', 'Data Engineer', TIMESTAMPTZ '2026-05-15T00:00:00+00:00');
    INSERT INTO career_roles ("Id", "CreatedAt", "Description", "IsActive", "Level", "Name", "UpdatedAt")
    VALUES ('11111111-1111-1111-1111-111111111107', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Designs automated tests, test frameworks, and quality assurance workflows.', TRUE, 'Fresher', 'QA Automation Engineer', TIMESTAMPTZ '2026-05-15T00:00:00+00:00');
    INSERT INTO career_roles ("Id", "CreatedAt", "Description", "IsActive", "Level", "Name", "UpdatedAt")
    VALUES ('11111111-1111-1111-1111-111111111108', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Builds and operates cloud infrastructure, deployment environments, and managed services.', TRUE, 'Fresher', 'Cloud Engineer', TIMESTAMPTZ '2026-05-15T00:00:00+00:00');
    INSERT INTO career_roles ("Id", "CreatedAt", "Description", "IsActive", "Level", "Name", "UpdatedAt")
    VALUES ('11111111-1111-1111-1111-111111111109', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Builds software applications that integrate AI APIs, prompts, agents, and AI-assisted workflows.', TRUE, 'Fresher', 'AI Application Developer', TIMESTAMPTZ '2026-05-15T00:00:00+00:00');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515144108_SeedCareerRoles') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260515144108_SeedCareerRoles', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515144341_AddUserRoleConstraint') THEN
    UPDATE users
    SET "Role" = 'Student'
    WHERE "Role" = 'User' OR "Role" IS NULL OR "Role" = '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515144341_AddUserRoleConstraint') THEN
    ALTER TABLE users ADD CONSTRAINT "CK_users_Role" CHECK ("Role" IN ('Student', 'Admin', 'AcademicCounselor', 'IndustryMentor'));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515144341_AddUserRoleConstraint') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260515144341_AddUserRoleConstraint', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515173354_AddRoadmapPrerequisitesAndLearningSeeds') THEN
    ALTER TABLE roadmap_nodes ADD "PrerequisiteNodeId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515173354_AddRoadmapPrerequisitesAndLearningSeeds') THEN
    INSERT INTO skills ("Id", "Category", "CreatedAt", "Description", "IsActive", "Name", "UpdatedAt")
    VALUES ('22222222-2222-2222-2222-222222222101', 'Backend', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Designing HTTP APIs with resources, validation, authentication, pagination, and error contracts.', TRUE, 'REST API Design', TIMESTAMPTZ '2026-05-15T00:00:00+00:00');
    INSERT INTO skills ("Id", "Category", "CreatedAt", "Description", "IsActive", "Name", "UpdatedAt")
    VALUES ('22222222-2222-2222-2222-222222222102', 'Backend', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Modeling relational data, constraints, indexes, migrations, and query patterns.', TRUE, 'Database Design', TIMESTAMPTZ '2026-05-15T00:00:00+00:00');
    INSERT INTO skills ("Id", "Category", "CreatedAt", "Description", "IsActive", "Name", "UpdatedAt")
    VALUES ('22222222-2222-2222-2222-222222222103', 'Backend', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Implementing secure login, JWT, role-based access, and protected API flows.', TRUE, 'Authentication and Authorization', TIMESTAMPTZ '2026-05-15T00:00:00+00:00');
    INSERT INTO skills ("Id", "Category", "CreatedAt", "Description", "IsActive", "Name", "UpdatedAt")
    VALUES ('22222222-2222-2222-2222-222222222104', 'Engineering', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Writing focused automated tests for service logic, controllers, and integration boundaries.', TRUE, 'Unit Testing', TIMESTAMPTZ '2026-05-15T00:00:00+00:00');
    INSERT INTO skills ("Id", "Category", "CreatedAt", "Description", "IsActive", "Name", "UpdatedAt")
    VALUES ('22222222-2222-2222-2222-222222222105', 'DevOps', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Containerizing applications and composing local development services.', TRUE, 'Docker', TIMESTAMPTZ '2026-05-15T00:00:00+00:00');
    INSERT INTO skills ("Id", "Category", "CreatedAt", "Description", "IsActive", "Name", "UpdatedAt")
    VALUES ('22222222-2222-2222-2222-222222222106', 'Frontend', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Building component-based user interfaces and stateful client flows.', TRUE, 'React', TIMESTAMPTZ '2026-05-15T00:00:00+00:00');
    INSERT INTO skills ("Id", "Category", "CreatedAt", "Description", "IsActive", "Name", "UpdatedAt")
    VALUES ('22222222-2222-2222-2222-222222222107', 'Frontend', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Using static typing, interfaces, and type-safe application boundaries.', TRUE, 'TypeScript', TIMESTAMPTZ '2026-05-15T00:00:00+00:00');
    INSERT INTO skills ("Id", "Category", "CreatedAt", "Description", "IsActive", "Name", "UpdatedAt")
    VALUES ('22222222-2222-2222-2222-222222222108', 'Frontend', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Creating accessible layouts that work well across mobile and desktop screens.', TRUE, 'Responsive UI', TIMESTAMPTZ '2026-05-15T00:00:00+00:00');
    INSERT INTO skills ("Id", "Category", "CreatedAt", "Description", "IsActive", "Name", "UpdatedAt")
    VALUES ('22222222-2222-2222-2222-222222222109', 'Frontend', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Connecting frontend applications to authenticated APIs with loading and error states.', TRUE, 'API Integration', TIMESTAMPTZ '2026-05-15T00:00:00+00:00');
    INSERT INTO skills ("Id", "Category", "CreatedAt", "Description", "IsActive", "Name", "UpdatedAt")
    VALUES ('22222222-2222-2222-2222-222222222110', 'Mobile', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Designing and implementing mobile-first application screens and navigation.', TRUE, 'Mobile UI', TIMESTAMPTZ '2026-05-15T00:00:00+00:00');
    INSERT INTO skills ("Id", "Category", "CreatedAt", "Description", "IsActive", "Name", "UpdatedAt")
    VALUES ('22222222-2222-2222-2222-222222222111', 'DevOps', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Automating build, test, migration, and deployment workflows.', TRUE, 'CI/CD', TIMESTAMPTZ '2026-05-15T00:00:00+00:00');
    INSERT INTO skills ("Id", "Category", "CreatedAt", "Description", "IsActive", "Name", "UpdatedAt")
    VALUES ('22222222-2222-2222-2222-222222222112', 'Cloud', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Deploying applications to cloud infrastructure and managed platform services.', TRUE, 'Cloud Deployment', TIMESTAMPTZ '2026-05-15T00:00:00+00:00');
    INSERT INTO skills ("Id", "Category", "CreatedAt", "Description", "IsActive", "Name", "UpdatedAt")
    VALUES ('22222222-2222-2222-2222-222222222113', 'Data', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Writing relational queries, joins, aggregations, and data validation checks.', TRUE, 'SQL', TIMESTAMPTZ '2026-05-15T00:00:00+00:00');
    INSERT INTO skills ("Id", "Category", "CreatedAt", "Description", "IsActive", "Name", "UpdatedAt")
    VALUES ('22222222-2222-2222-2222-222222222114', 'Data', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Building reproducible data ingestion, transformation, and quality workflows.', TRUE, 'Data Pipeline', TIMESTAMPTZ '2026-05-15T00:00:00+00:00');
    INSERT INTO skills ("Id", "Category", "CreatedAt", "Description", "IsActive", "Name", "UpdatedAt")
    VALUES ('22222222-2222-2222-2222-222222222115', 'QA', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Automating API, UI, and regression tests with repeatable reporting.', TRUE, 'Test Automation', TIMESTAMPTZ '2026-05-15T00:00:00+00:00');
    INSERT INTO skills ("Id", "Category", "CreatedAt", "Description", "IsActive", "Name", "UpdatedAt")
    VALUES ('22222222-2222-2222-2222-222222222116', 'AI', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Calling AI providers with prompts, structured outputs, retries, and result persistence.', TRUE, 'AI API Integration', TIMESTAMPTZ '2026-05-15T00:00:00+00:00');
    INSERT INTO skills ("Id", "Category", "CreatedAt", "Description", "IsActive", "Name", "UpdatedAt")
    VALUES ('22222222-2222-2222-2222-222222222117', 'AI', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Designing prompts, evaluation criteria, and guardrails for AI-assisted features.', TRUE, 'Prompt Engineering', TIMESTAMPTZ '2026-05-15T00:00:00+00:00');
    INSERT INTO skills ("Id", "Category", "CreatedAt", "Description", "IsActive", "Name", "UpdatedAt")
    VALUES ('22222222-2222-2222-2222-222222222118', 'Career', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Presenting repositories with README, screenshots, demo links, and clear project evidence.', TRUE, 'GitHub Portfolio', TIMESTAMPTZ '2026-05-15T00:00:00+00:00');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515173354_AddRoadmapPrerequisitesAndLearningSeeds') THEN
    INSERT INTO learning_resources ("Id", "CreatedAt", "Difficulty", "EstimatedHours", "IsActive", "ResourceType", "SkillId", "Title", "UpdatedAt", "Url")
    VALUES ('33333333-3333-3333-3333-333333333101', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Beginner', 8, TRUE, 'Documentation', '22222222-2222-2222-2222-222222222101', 'Microsoft Learn: Create web APIs with ASP.NET Core', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'https://learn.microsoft.com/en-us/aspnet/core/web-api/');
    INSERT INTO learning_resources ("Id", "CreatedAt", "Difficulty", "EstimatedHours", "IsActive", "ResourceType", "SkillId", "Title", "UpdatedAt", "Url")
    VALUES ('33333333-3333-3333-3333-333333333102', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Beginner', 8, TRUE, 'Documentation', '22222222-2222-2222-2222-222222222102', 'Microsoft Learn: EF Core modeling', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'https://learn.microsoft.com/en-us/ef/core/modeling/');
    INSERT INTO learning_resources ("Id", "CreatedAt", "Difficulty", "EstimatedHours", "IsActive", "ResourceType", "SkillId", "Title", "UpdatedAt", "Url")
    VALUES ('33333333-3333-3333-3333-333333333103', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Intermediate', 10, TRUE, 'Documentation', '22222222-2222-2222-2222-222222222103', 'Microsoft Learn: Authentication and authorization in ASP.NET Core', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'https://learn.microsoft.com/en-us/aspnet/core/security/authentication/');
    INSERT INTO learning_resources ("Id", "CreatedAt", "Difficulty", "EstimatedHours", "IsActive", "ResourceType", "SkillId", "Title", "UpdatedAt", "Url")
    VALUES ('33333333-3333-3333-3333-333333333104', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Beginner', 6, TRUE, 'Documentation', '22222222-2222-2222-2222-222222222104', 'Microsoft Learn: Unit test C# code', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-dotnet-test');
    INSERT INTO learning_resources ("Id", "CreatedAt", "Difficulty", "EstimatedHours", "IsActive", "ResourceType", "SkillId", "Title", "UpdatedAt", "Url")
    VALUES ('33333333-3333-3333-3333-333333333105', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Beginner', 6, TRUE, 'Documentation', '22222222-2222-2222-2222-222222222105', 'Docker Docs: Get started', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'https://docs.docker.com/get-started/');
    INSERT INTO learning_resources ("Id", "CreatedAt", "Difficulty", "EstimatedHours", "IsActive", "ResourceType", "SkillId", "Title", "UpdatedAt", "Url")
    VALUES ('33333333-3333-3333-3333-333333333106', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Beginner', 10, TRUE, 'Documentation', '22222222-2222-2222-2222-222222222106', 'React Docs: Learn React', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'https://react.dev/learn');
    INSERT INTO learning_resources ("Id", "CreatedAt", "Difficulty", "EstimatedHours", "IsActive", "ResourceType", "SkillId", "Title", "UpdatedAt", "Url")
    VALUES ('33333333-3333-3333-3333-333333333107', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Beginner', 8, TRUE, 'Documentation', '22222222-2222-2222-2222-222222222107', 'TypeScript Handbook', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'https://www.typescriptlang.org/docs/handbook/intro.html');
    INSERT INTO learning_resources ("Id", "CreatedAt", "Difficulty", "EstimatedHours", "IsActive", "ResourceType", "SkillId", "Title", "UpdatedAt", "Url")
    VALUES ('33333333-3333-3333-3333-333333333108', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Beginner', 6, TRUE, 'Documentation', '22222222-2222-2222-2222-222222222108', 'MDN: Responsive design', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'https://developer.mozilla.org/en-US/docs/Learn_web_development/Core/CSS_layout/Responsive_Design');
    INSERT INTO learning_resources ("Id", "CreatedAt", "Difficulty", "EstimatedHours", "IsActive", "ResourceType", "SkillId", "Title", "UpdatedAt", "Url")
    VALUES ('33333333-3333-3333-3333-333333333109', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Beginner', 5, TRUE, 'Documentation', '22222222-2222-2222-2222-222222222109', 'MDN: Fetch API', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'https://developer.mozilla.org/en-US/docs/Web/API/Fetch_API/Using_Fetch');
    INSERT INTO learning_resources ("Id", "CreatedAt", "Difficulty", "EstimatedHours", "IsActive", "ResourceType", "SkillId", "Title", "UpdatedAt", "Url")
    VALUES ('33333333-3333-3333-3333-333333333110', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Beginner', 10, TRUE, 'Documentation', '22222222-2222-2222-2222-222222222110', 'Android Developers: App architecture', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'https://developer.android.com/topic/architecture');
    INSERT INTO learning_resources ("Id", "CreatedAt", "Difficulty", "EstimatedHours", "IsActive", "ResourceType", "SkillId", "Title", "UpdatedAt", "Url")
    VALUES ('33333333-3333-3333-3333-333333333111', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Beginner', 6, TRUE, 'Documentation', '22222222-2222-2222-2222-222222222111', 'GitHub Actions: Learn GitHub Actions', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'https://docs.github.com/en/actions/learn-github-actions');
    INSERT INTO learning_resources ("Id", "CreatedAt", "Difficulty", "EstimatedHours", "IsActive", "ResourceType", "SkillId", "Title", "UpdatedAt", "Url")
    VALUES ('33333333-3333-3333-3333-333333333112', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Beginner', 6, TRUE, 'Documentation', '22222222-2222-2222-2222-222222222112', 'Google Cloud: Cloud Run quickstart', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'https://cloud.google.com/run/docs/quickstarts');
    INSERT INTO learning_resources ("Id", "CreatedAt", "Difficulty", "EstimatedHours", "IsActive", "ResourceType", "SkillId", "Title", "UpdatedAt", "Url")
    VALUES ('33333333-3333-3333-3333-333333333113', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Beginner', 8, TRUE, 'Documentation', '22222222-2222-2222-2222-222222222113', 'PostgreSQL Tutorial', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'https://www.postgresql.org/docs/current/tutorial.html');
    INSERT INTO learning_resources ("Id", "CreatedAt", "Difficulty", "EstimatedHours", "IsActive", "ResourceType", "SkillId", "Title", "UpdatedAt", "Url")
    VALUES ('33333333-3333-3333-3333-333333333114', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Intermediate', 12, TRUE, 'Course', '22222222-2222-2222-2222-222222222114', 'Microsoft Learn: Data engineering on Microsoft Azure', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'https://learn.microsoft.com/en-us/training/paths/data-engineer-azure-databricks/');
    INSERT INTO learning_resources ("Id", "CreatedAt", "Difficulty", "EstimatedHours", "IsActive", "ResourceType", "SkillId", "Title", "UpdatedAt", "Url")
    VALUES ('33333333-3333-3333-3333-333333333115', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Beginner', 6, TRUE, 'Documentation', '22222222-2222-2222-2222-222222222115', 'Playwright Docs: Getting started', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'https://playwright.dev/docs/intro');
    INSERT INTO learning_resources ("Id", "CreatedAt", "Difficulty", "EstimatedHours", "IsActive", "ResourceType", "SkillId", "Title", "UpdatedAt", "Url")
    VALUES ('33333333-3333-3333-3333-333333333116', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Beginner', 6, TRUE, 'Documentation', '22222222-2222-2222-2222-222222222116', 'Google AI for Developers: Gemini API quickstart', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'https://ai.google.dev/gemini-api/docs/quickstart');
    INSERT INTO learning_resources ("Id", "CreatedAt", "Difficulty", "EstimatedHours", "IsActive", "ResourceType", "SkillId", "Title", "UpdatedAt", "Url")
    VALUES ('33333333-3333-3333-3333-333333333117', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Intermediate', 6, TRUE, 'Documentation', '22222222-2222-2222-2222-222222222117', 'Google AI for Developers: Prompting strategies', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'https://ai.google.dev/gemini-api/docs/prompting-strategies');
    INSERT INTO learning_resources ("Id", "CreatedAt", "Difficulty", "EstimatedHours", "IsActive", "ResourceType", "SkillId", "Title", "UpdatedAt", "Url")
    VALUES ('33333333-3333-3333-3333-333333333118', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Beginner', 4, TRUE, 'Documentation', '22222222-2222-2222-2222-222222222118', 'GitHub Docs: About READMEs', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/about-readmes');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515173354_AddRoadmapPrerequisitesAndLearningSeeds') THEN
    INSERT INTO role_skill_requirements ("Id", "CareerRoleId", "CreatedAt", "Priority", "RequiredLevel", "SkillId", "UpdatedAt", "Weight")
    VALUES ('44444444-4444-4444-4444-444444444101', '11111111-1111-1111-1111-111111111101', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1, 'Intermediate', '22222222-2222-2222-2222-222222222101', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1.4);
    INSERT INTO role_skill_requirements ("Id", "CareerRoleId", "CreatedAt", "Priority", "RequiredLevel", "SkillId", "UpdatedAt", "Weight")
    VALUES ('44444444-4444-4444-4444-444444444102', '11111111-1111-1111-1111-111111111101', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1, 'Intermediate', '22222222-2222-2222-2222-222222222102', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1.3);
    INSERT INTO role_skill_requirements ("Id", "CareerRoleId", "CreatedAt", "Priority", "RequiredLevel", "SkillId", "UpdatedAt", "Weight")
    VALUES ('44444444-4444-4444-4444-444444444103', '11111111-1111-1111-1111-111111111101', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 2, 'Beginner', '22222222-2222-2222-2222-222222222103', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1.2);
    INSERT INTO role_skill_requirements ("Id", "CareerRoleId", "CreatedAt", "Priority", "RequiredLevel", "SkillId", "UpdatedAt", "Weight")
    VALUES ('44444444-4444-4444-4444-444444444104', '11111111-1111-1111-1111-111111111101', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 3, 'Beginner', '22222222-2222-2222-2222-222222222104', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1.0);
    INSERT INTO role_skill_requirements ("Id", "CareerRoleId", "CreatedAt", "Priority", "RequiredLevel", "SkillId", "UpdatedAt", "Weight")
    VALUES ('44444444-4444-4444-4444-444444444105', '11111111-1111-1111-1111-111111111101', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 4, 'Beginner', '22222222-2222-2222-2222-222222222105', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 0.8);
    INSERT INTO role_skill_requirements ("Id", "CareerRoleId", "CreatedAt", "Priority", "RequiredLevel", "SkillId", "UpdatedAt", "Weight")
    VALUES ('44444444-4444-4444-4444-444444444106', '11111111-1111-1111-1111-111111111102', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1, 'Intermediate', '22222222-2222-2222-2222-222222222106', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1.4);
    INSERT INTO role_skill_requirements ("Id", "CareerRoleId", "CreatedAt", "Priority", "RequiredLevel", "SkillId", "UpdatedAt", "Weight")
    VALUES ('44444444-4444-4444-4444-444444444107', '11111111-1111-1111-1111-111111111102', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1, 'Intermediate', '22222222-2222-2222-2222-222222222107', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1.3);
    INSERT INTO role_skill_requirements ("Id", "CareerRoleId", "CreatedAt", "Priority", "RequiredLevel", "SkillId", "UpdatedAt", "Weight")
    VALUES ('44444444-4444-4444-4444-444444444108', '11111111-1111-1111-1111-111111111102', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 2, 'Beginner', '22222222-2222-2222-2222-222222222108', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1.1);
    INSERT INTO role_skill_requirements ("Id", "CareerRoleId", "CreatedAt", "Priority", "RequiredLevel", "SkillId", "UpdatedAt", "Weight")
    VALUES ('44444444-4444-4444-4444-444444444109', '11111111-1111-1111-1111-111111111102', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 2, 'Beginner', '22222222-2222-2222-2222-222222222109', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1.0);
    INSERT INTO role_skill_requirements ("Id", "CareerRoleId", "CreatedAt", "Priority", "RequiredLevel", "SkillId", "UpdatedAt", "Weight")
    VALUES ('44444444-4444-4444-4444-444444444110', '11111111-1111-1111-1111-111111111102', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 4, 'Beginner', '22222222-2222-2222-2222-222222222118', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 0.8);
    INSERT INTO role_skill_requirements ("Id", "CareerRoleId", "CreatedAt", "Priority", "RequiredLevel", "SkillId", "UpdatedAt", "Weight")
    VALUES ('44444444-4444-4444-4444-444444444111', '11111111-1111-1111-1111-111111111103', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1, 'Intermediate', '22222222-2222-2222-2222-222222222101', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1.3);
    INSERT INTO role_skill_requirements ("Id", "CareerRoleId", "CreatedAt", "Priority", "RequiredLevel", "SkillId", "UpdatedAt", "Weight")
    VALUES ('44444444-4444-4444-4444-444444444112', '11111111-1111-1111-1111-111111111103', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1, 'Intermediate', '22222222-2222-2222-2222-222222222106', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1.3);
    INSERT INTO role_skill_requirements ("Id", "CareerRoleId", "CreatedAt", "Priority", "RequiredLevel", "SkillId", "UpdatedAt", "Weight")
    VALUES ('44444444-4444-4444-4444-444444444113', '11111111-1111-1111-1111-111111111103', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 2, 'Intermediate', '22222222-2222-2222-2222-222222222107', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1.2);
    INSERT INTO role_skill_requirements ("Id", "CareerRoleId", "CreatedAt", "Priority", "RequiredLevel", "SkillId", "UpdatedAt", "Weight")
    VALUES ('44444444-4444-4444-4444-444444444114', '11111111-1111-1111-1111-111111111103', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 2, 'Beginner', '22222222-2222-2222-2222-222222222102', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1.1);
    INSERT INTO role_skill_requirements ("Id", "CareerRoleId", "CreatedAt", "Priority", "RequiredLevel", "SkillId", "UpdatedAt", "Weight")
    VALUES ('44444444-4444-4444-4444-444444444115', '11111111-1111-1111-1111-111111111103', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 3, 'Beginner', '22222222-2222-2222-2222-222222222103', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1.0);
    INSERT INTO role_skill_requirements ("Id", "CareerRoleId", "CreatedAt", "Priority", "RequiredLevel", "SkillId", "UpdatedAt", "Weight")
    VALUES ('44444444-4444-4444-4444-444444444116', '11111111-1111-1111-1111-111111111104', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1, 'Intermediate', '22222222-2222-2222-2222-222222222110', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1.4);
    INSERT INTO role_skill_requirements ("Id", "CareerRoleId", "CreatedAt", "Priority", "RequiredLevel", "SkillId", "UpdatedAt", "Weight")
    VALUES ('44444444-4444-4444-4444-444444444117', '11111111-1111-1111-1111-111111111104', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 2, 'Beginner', '22222222-2222-2222-2222-222222222109', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1.2);
    INSERT INTO role_skill_requirements ("Id", "CareerRoleId", "CreatedAt", "Priority", "RequiredLevel", "SkillId", "UpdatedAt", "Weight")
    VALUES ('44444444-4444-4444-4444-444444444118', '11111111-1111-1111-1111-111111111104', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 3, 'Beginner', '22222222-2222-2222-2222-222222222107', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1.0);
    INSERT INTO role_skill_requirements ("Id", "CareerRoleId", "CreatedAt", "Priority", "RequiredLevel", "SkillId", "UpdatedAt", "Weight")
    VALUES ('44444444-4444-4444-4444-444444444119', '11111111-1111-1111-1111-111111111104', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 4, 'Beginner', '22222222-2222-2222-2222-222222222118', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 0.8);
    INSERT INTO role_skill_requirements ("Id", "CareerRoleId", "CreatedAt", "Priority", "RequiredLevel", "SkillId", "UpdatedAt", "Weight")
    VALUES ('44444444-4444-4444-4444-444444444120', '11111111-1111-1111-1111-111111111105', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1, 'Intermediate', '22222222-2222-2222-2222-222222222105', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1.4);
    INSERT INTO role_skill_requirements ("Id", "CareerRoleId", "CreatedAt", "Priority", "RequiredLevel", "SkillId", "UpdatedAt", "Weight")
    VALUES ('44444444-4444-4444-4444-444444444121', '11111111-1111-1111-1111-111111111105', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1, 'Intermediate', '22222222-2222-2222-2222-222222222111', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1.4);
    INSERT INTO role_skill_requirements ("Id", "CareerRoleId", "CreatedAt", "Priority", "RequiredLevel", "SkillId", "UpdatedAt", "Weight")
    VALUES ('44444444-4444-4444-4444-444444444122', '11111111-1111-1111-1111-111111111105', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 2, 'Beginner', '22222222-2222-2222-2222-222222222112', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1.2);
    INSERT INTO role_skill_requirements ("Id", "CareerRoleId", "CreatedAt", "Priority", "RequiredLevel", "SkillId", "UpdatedAt", "Weight")
    VALUES ('44444444-4444-4444-4444-444444444123', '11111111-1111-1111-1111-111111111105', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 3, 'Beginner', '22222222-2222-2222-2222-222222222104', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 0.9);
    INSERT INTO role_skill_requirements ("Id", "CareerRoleId", "CreatedAt", "Priority", "RequiredLevel", "SkillId", "UpdatedAt", "Weight")
    VALUES ('44444444-4444-4444-4444-444444444124', '11111111-1111-1111-1111-111111111106', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1, 'Intermediate', '22222222-2222-2222-2222-222222222113', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1.4);
    INSERT INTO role_skill_requirements ("Id", "CareerRoleId", "CreatedAt", "Priority", "RequiredLevel", "SkillId", "UpdatedAt", "Weight")
    VALUES ('44444444-4444-4444-4444-444444444125', '11111111-1111-1111-1111-111111111106', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1, 'Intermediate', '22222222-2222-2222-2222-222222222114', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1.4);
    INSERT INTO role_skill_requirements ("Id", "CareerRoleId", "CreatedAt", "Priority", "RequiredLevel", "SkillId", "UpdatedAt", "Weight")
    VALUES ('44444444-4444-4444-4444-444444444126', '11111111-1111-1111-1111-111111111106', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 3, 'Beginner', '22222222-2222-2222-2222-222222222105', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1.0);
    INSERT INTO role_skill_requirements ("Id", "CareerRoleId", "CreatedAt", "Priority", "RequiredLevel", "SkillId", "UpdatedAt", "Weight")
    VALUES ('44444444-4444-4444-4444-444444444127', '11111111-1111-1111-1111-111111111106', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 3, 'Beginner', '22222222-2222-2222-2222-222222222112', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 0.9);
    INSERT INTO role_skill_requirements ("Id", "CareerRoleId", "CreatedAt", "Priority", "RequiredLevel", "SkillId", "UpdatedAt", "Weight")
    VALUES ('44444444-4444-4444-4444-444444444128', '11111111-1111-1111-1111-111111111107', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1, 'Intermediate', '22222222-2222-2222-2222-222222222115', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1.4);
    INSERT INTO role_skill_requirements ("Id", "CareerRoleId", "CreatedAt", "Priority", "RequiredLevel", "SkillId", "UpdatedAt", "Weight")
    VALUES ('44444444-4444-4444-4444-444444444129', '11111111-1111-1111-1111-111111111107', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 2, 'Intermediate', '22222222-2222-2222-2222-222222222104', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1.2);
    INSERT INTO role_skill_requirements ("Id", "CareerRoleId", "CreatedAt", "Priority", "RequiredLevel", "SkillId", "UpdatedAt", "Weight")
    VALUES ('44444444-4444-4444-4444-444444444130', '11111111-1111-1111-1111-111111111107', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 2, 'Beginner', '22222222-2222-2222-2222-222222222109', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1.0);
    INSERT INTO role_skill_requirements ("Id", "CareerRoleId", "CreatedAt", "Priority", "RequiredLevel", "SkillId", "UpdatedAt", "Weight")
    VALUES ('44444444-4444-4444-4444-444444444131', '11111111-1111-1111-1111-111111111107', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 3, 'Beginner', '22222222-2222-2222-2222-222222222111', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 0.9);
    INSERT INTO role_skill_requirements ("Id", "CareerRoleId", "CreatedAt", "Priority", "RequiredLevel", "SkillId", "UpdatedAt", "Weight")
    VALUES ('44444444-4444-4444-4444-444444444132', '11111111-1111-1111-1111-111111111108', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1, 'Intermediate', '22222222-2222-2222-2222-222222222112', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1.5);
    INSERT INTO role_skill_requirements ("Id", "CareerRoleId", "CreatedAt", "Priority", "RequiredLevel", "SkillId", "UpdatedAt", "Weight")
    VALUES ('44444444-4444-4444-4444-444444444133', '11111111-1111-1111-1111-111111111108', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1, 'Intermediate', '22222222-2222-2222-2222-222222222105', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1.2);
    INSERT INTO role_skill_requirements ("Id", "CareerRoleId", "CreatedAt", "Priority", "RequiredLevel", "SkillId", "UpdatedAt", "Weight")
    VALUES ('44444444-4444-4444-4444-444444444134', '11111111-1111-1111-1111-111111111108', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 2, 'Beginner', '22222222-2222-2222-2222-222222222111', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1.1);
    INSERT INTO role_skill_requirements ("Id", "CareerRoleId", "CreatedAt", "Priority", "RequiredLevel", "SkillId", "UpdatedAt", "Weight")
    VALUES ('44444444-4444-4444-4444-444444444135', '11111111-1111-1111-1111-111111111108', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 3, 'Beginner', '22222222-2222-2222-2222-222222222113', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 0.8);
    INSERT INTO role_skill_requirements ("Id", "CareerRoleId", "CreatedAt", "Priority", "RequiredLevel", "SkillId", "UpdatedAt", "Weight")
    VALUES ('44444444-4444-4444-4444-444444444136', '11111111-1111-1111-1111-111111111109', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1, 'Intermediate', '22222222-2222-2222-2222-222222222116', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1.5);
    INSERT INTO role_skill_requirements ("Id", "CareerRoleId", "CreatedAt", "Priority", "RequiredLevel", "SkillId", "UpdatedAt", "Weight")
    VALUES ('44444444-4444-4444-4444-444444444137', '11111111-1111-1111-1111-111111111109', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1, 'Intermediate', '22222222-2222-2222-2222-222222222117', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1.3);
    INSERT INTO role_skill_requirements ("Id", "CareerRoleId", "CreatedAt", "Priority", "RequiredLevel", "SkillId", "UpdatedAt", "Weight")
    VALUES ('44444444-4444-4444-4444-444444444138', '11111111-1111-1111-1111-111111111109', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 2, 'Beginner', '22222222-2222-2222-2222-222222222101', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 1.0);
    INSERT INTO role_skill_requirements ("Id", "CareerRoleId", "CreatedAt", "Priority", "RequiredLevel", "SkillId", "UpdatedAt", "Weight")
    VALUES ('44444444-4444-4444-4444-444444444139', '11111111-1111-1111-1111-111111111109', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 3, 'Beginner', '22222222-2222-2222-2222-222222222107', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 0.9);
    INSERT INTO role_skill_requirements ("Id", "CareerRoleId", "CreatedAt", "Priority", "RequiredLevel", "SkillId", "UpdatedAt", "Weight")
    VALUES ('44444444-4444-4444-4444-444444444140', '11111111-1111-1111-1111-111111111109', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 4, 'Beginner', '22222222-2222-2222-2222-222222222118', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 0.8);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515173354_AddRoadmapPrerequisitesAndLearningSeeds') THEN
    CREATE INDEX "IX_roadmap_nodes_PrerequisiteNodeId" ON roadmap_nodes ("PrerequisiteNodeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515173354_AddRoadmapPrerequisitesAndLearningSeeds') THEN
    ALTER TABLE roadmap_nodes ADD CONSTRAINT "FK_roadmap_nodes_roadmap_nodes_PrerequisiteNodeId" FOREIGN KEY ("PrerequisiteNodeId") REFERENCES roadmap_nodes ("Id") ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515173354_AddRoadmapPrerequisitesAndLearningSeeds') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260515173354_AddRoadmapPrerequisitesAndLearningSeeds', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516045943_AddGithubOAuthConnections') THEN
    CREATE TABLE github_connections (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "GithubUserId" bigint,
        "GithubUsername" character varying(100) NOT NULL,
        "AccessToken" text NOT NULL,
        "TokenType" character varying(50) NOT NULL,
        "Scope" character varying(500),
        "ConnectedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_github_connections" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_github_connections_users_UserId" FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516045943_AddGithubOAuthConnections') THEN
    CREATE TABLE github_oauth_states (
        "State" character varying(128) NOT NULL,
        "UserId" uuid NOT NULL,
        "ReturnUrl" character varying(2048),
        "ExpiresAt" timestamp with time zone NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_github_oauth_states" PRIMARY KEY ("State"),
        CONSTRAINT "FK_github_oauth_states_users_UserId" FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516045943_AddGithubOAuthConnections') THEN
    CREATE INDEX "IX_github_connections_GithubUsername" ON github_connections ("GithubUsername");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516045943_AddGithubOAuthConnections') THEN
    CREATE UNIQUE INDEX "IX_github_connections_UserId" ON github_connections ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516045943_AddGithubOAuthConnections') THEN
    CREATE INDEX "IX_github_oauth_states_ExpiresAt" ON github_oauth_states ("ExpiresAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516045943_AddGithubOAuthConnections') THEN
    CREATE INDEX "IX_github_oauth_states_UserId" ON github_oauth_states ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516045943_AddGithubOAuthConnections') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260516045943_AddGithubOAuthConnections', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516054712_AddPortfolioProjectImageUrl') THEN
    ALTER TABLE portfolio_projects ADD "ImageUrl" character varying(1024);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516054712_AddPortfolioProjectImageUrl') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260516054712_AddPortfolioProjectImageUrl', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260518031004_AddLearningResourceFiles') THEN
    ALTER TABLE learning_resources ADD "ContentType" character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260518031004_AddLearningResourceFiles') THEN
    ALTER TABLE learning_resources ADD "FileSize" bigint;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260518031004_AddLearningResourceFiles') THEN
    ALTER TABLE learning_resources ADD "StorageObjectName" character varying(1024);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260518031004_AddLearningResourceFiles') THEN
    UPDATE learning_resources SET "ContentType" = NULL, "FileSize" = NULL, "StorageObjectName" = NULL
    WHERE "Id" = '33333333-3333-3333-3333-333333333101';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260518031004_AddLearningResourceFiles') THEN
    UPDATE learning_resources SET "ContentType" = NULL, "FileSize" = NULL, "StorageObjectName" = NULL
    WHERE "Id" = '33333333-3333-3333-3333-333333333102';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260518031004_AddLearningResourceFiles') THEN
    UPDATE learning_resources SET "ContentType" = NULL, "FileSize" = NULL, "StorageObjectName" = NULL
    WHERE "Id" = '33333333-3333-3333-3333-333333333103';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260518031004_AddLearningResourceFiles') THEN
    UPDATE learning_resources SET "ContentType" = NULL, "FileSize" = NULL, "StorageObjectName" = NULL
    WHERE "Id" = '33333333-3333-3333-3333-333333333104';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260518031004_AddLearningResourceFiles') THEN
    UPDATE learning_resources SET "ContentType" = NULL, "FileSize" = NULL, "StorageObjectName" = NULL
    WHERE "Id" = '33333333-3333-3333-3333-333333333105';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260518031004_AddLearningResourceFiles') THEN
    UPDATE learning_resources SET "ContentType" = NULL, "FileSize" = NULL, "StorageObjectName" = NULL
    WHERE "Id" = '33333333-3333-3333-3333-333333333106';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260518031004_AddLearningResourceFiles') THEN
    UPDATE learning_resources SET "ContentType" = NULL, "FileSize" = NULL, "StorageObjectName" = NULL
    WHERE "Id" = '33333333-3333-3333-3333-333333333107';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260518031004_AddLearningResourceFiles') THEN
    UPDATE learning_resources SET "ContentType" = NULL, "FileSize" = NULL, "StorageObjectName" = NULL
    WHERE "Id" = '33333333-3333-3333-3333-333333333108';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260518031004_AddLearningResourceFiles') THEN
    UPDATE learning_resources SET "ContentType" = NULL, "FileSize" = NULL, "StorageObjectName" = NULL
    WHERE "Id" = '33333333-3333-3333-3333-333333333109';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260518031004_AddLearningResourceFiles') THEN
    UPDATE learning_resources SET "ContentType" = NULL, "FileSize" = NULL, "StorageObjectName" = NULL
    WHERE "Id" = '33333333-3333-3333-3333-333333333110';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260518031004_AddLearningResourceFiles') THEN
    UPDATE learning_resources SET "ContentType" = NULL, "FileSize" = NULL, "StorageObjectName" = NULL
    WHERE "Id" = '33333333-3333-3333-3333-333333333111';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260518031004_AddLearningResourceFiles') THEN
    UPDATE learning_resources SET "ContentType" = NULL, "FileSize" = NULL, "StorageObjectName" = NULL
    WHERE "Id" = '33333333-3333-3333-3333-333333333112';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260518031004_AddLearningResourceFiles') THEN
    UPDATE learning_resources SET "ContentType" = NULL, "FileSize" = NULL, "StorageObjectName" = NULL
    WHERE "Id" = '33333333-3333-3333-3333-333333333113';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260518031004_AddLearningResourceFiles') THEN
    UPDATE learning_resources SET "ContentType" = NULL, "FileSize" = NULL, "StorageObjectName" = NULL
    WHERE "Id" = '33333333-3333-3333-3333-333333333114';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260518031004_AddLearningResourceFiles') THEN
    UPDATE learning_resources SET "ContentType" = NULL, "FileSize" = NULL, "StorageObjectName" = NULL
    WHERE "Id" = '33333333-3333-3333-3333-333333333115';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260518031004_AddLearningResourceFiles') THEN
    UPDATE learning_resources SET "ContentType" = NULL, "FileSize" = NULL, "StorageObjectName" = NULL
    WHERE "Id" = '33333333-3333-3333-3333-333333333116';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260518031004_AddLearningResourceFiles') THEN
    UPDATE learning_resources SET "ContentType" = NULL, "FileSize" = NULL, "StorageObjectName" = NULL
    WHERE "Id" = '33333333-3333-3333-3333-333333333117';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260518031004_AddLearningResourceFiles') THEN
    UPDATE learning_resources SET "ContentType" = NULL, "FileSize" = NULL, "StorageObjectName" = NULL
    WHERE "Id" = '33333333-3333-3333-3333-333333333118';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260518031004_AddLearningResourceFiles') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260518031004_AddLearningResourceFiles', '8.0.11');
    END IF;
END $EF$;
COMMIT;

