using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP_BE.Migrations
{
    /// <inheritdoc />
    public partial class AlterMentorSessionContextJsonToText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Convert ContextJson from jsonb to text. Idempotent: only runs if the column is still jsonb.
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_name = 'mentor_sessions'
                          AND column_name = 'ContextJson'
                          AND data_type = 'jsonb'
                    ) THEN
                        ALTER TABLE mentor_sessions
                        ALTER COLUMN ""ContextJson"" TYPE text
                        USING ""ContextJson""::text;
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE mentor_sessions
                ALTER COLUMN ""ContextJson"" TYPE jsonb
                USING ""ContextJson""::jsonb;
            ");
        }
    }
}
