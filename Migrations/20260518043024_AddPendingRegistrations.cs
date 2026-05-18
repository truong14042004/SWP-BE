using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP_BE.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingRegistrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pending_registrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    EmailVerificationOtpHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    EmailVerificationOtpExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pending_registrations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pending_registrations_Email",
                table: "pending_registrations",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pending_registrations_EmailVerificationOtpExpiresAt",
                table: "pending_registrations",
                column: "EmailVerificationOtpExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_pending_registrations_Username",
                table: "pending_registrations",
                column: "Username",
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO pending_registrations (
                    "Id",
                    "Username",
                    "Email",
                    "FullName",
                    "PasswordHash",
                    "EmailVerificationOtpHash",
                    "EmailVerificationOtpExpiresAt",
                    "CreatedAt",
                    "UpdatedAt"
                )
                SELECT
                    "Id",
                    "Username",
                    "Email",
                    "FullName",
                    "PasswordHash",
                    "EmailVerificationOtpHash",
                    "EmailVerificationOtpExpiresAt",
                    "CreatedAt",
                    "UpdatedAt"
                FROM users
                WHERE "IsEmailVerified" = FALSE
                  AND "EmailVerificationOtpHash" IS NOT NULL
                  AND "EmailVerificationOtpExpiresAt" IS NOT NULL
                  AND "PasswordHash" IS NOT NULL
                  AND "Username" IS NOT NULL;

                DELETE FROM users
                WHERE "IsEmailVerified" = FALSE
                  AND "EmailVerificationOtpHash" IS NOT NULL
                  AND "EmailVerificationOtpExpiresAt" IS NOT NULL
                  AND "PasswordHash" IS NOT NULL
                  AND "Username" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO users (
                    "Id",
                    "Username",
                    "Email",
                    "FullName",
                    "PasswordHash",
                    "IsEmailVerified",
                    "EmailVerificationOtpHash",
                    "EmailVerificationOtpExpiresAt",
                    "Role",
                    "IsActive",
                    "CreatedAt",
                    "UpdatedAt"
                )
                SELECT
                    "Id",
                    "Username",
                    "Email",
                    "FullName",
                    "PasswordHash",
                    FALSE,
                    "EmailVerificationOtpHash",
                    "EmailVerificationOtpExpiresAt",
                    'Student',
                    FALSE,
                    "CreatedAt",
                    "UpdatedAt"
                FROM pending_registrations
                ON CONFLICT DO NOTHING;
                """);

            migrationBuilder.DropTable(
                name: "pending_registrations");
        }
    }
}
