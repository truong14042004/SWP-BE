using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP_BE.Migrations
{
    /// <inheritdoc />
    public partial class BusinessConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Dọn dữ liệu hiện có TRƯỚC khi thêm ràng buộc (nếu không migration sẽ vỡ) ──

            // 1. Hồ sơ talent trùng: giữ bản phân tích mới nhất của mỗi sinh viên.
            migrationBuilder.Sql("""
                DELETE FROM student_talent_profiles p
                USING student_talent_profiles newer
                WHERE p."StudentId" = newer."StudentId"
                  AND (p."AnalyzedAt" < newer."AnalyzedAt"
                       OR (p."AnalyzedAt" = newer."AnalyzedAt" AND p."Id" < newer."Id"));
                """);

            // 2. Trạng thái node ngoài bộ giá trị hợp lệ (dữ liệu legacy) -> NotStarted.
            migrationBuilder.Sql("""
                UPDATE roadmap_nodes
                SET "Status" = 'NotStarted'
                WHERE "Status" NOT IN ('NotStarted','InProgress','Completed','NeedReview','Verified');
                """);

            // 3. Tài nguyên auto trùng URL: tắt các bản cũ, giữ bản tạo mới nhất.
            migrationBuilder.Sql("""
                UPDATE learning_resources r
                SET "IsActive" = FALSE
                FROM learning_resources newer
                WHERE r."IsActive" AND newer."IsActive"
                  AND r."Url" = newer."Url"
                  AND ( r."Url" LIKE 'https://www.youtube.com/results?search_query=%'
                        OR r."Url" LIKE 'https://www.google.com/search?q=%' )
                  AND (r."CreatedAt" < newer."CreatedAt"
                       OR (r."CreatedAt" = newer."CreatedAt" AND r."Id" < newer."Id"));
                """);

            // 4. Cấp độ legacy "Verified" (trạng thái bị trộn vào thang cấp độ) -> Advanced,
            //    khớp SkillLevels.LevelRank coi "verified" tương đương Advanced.
            migrationBuilder.Sql("""
                UPDATE user_skills SET "Level" = 'Advanced' WHERE "Level" = 'Verified';
                UPDATE user_skills SET "VerifiedLevel" = 'Advanced' WHERE "VerifiedLevel" = 'Verified';
                """);

            migrationBuilder.DropIndex(
                name: "IX_student_talent_profiles_StudentId",
                table: "student_talent_profiles");

            migrationBuilder.CreateIndex(
                name: "IX_student_talent_profiles_StudentId",
                table: "student_talent_profiles",
                column: "StudentId",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_roadmap_nodes_Status",
                table: "roadmap_nodes",
                sql: "\"Status\" IN ('NotStarted','InProgress','Completed','NeedReview','Verified')");

            migrationBuilder.CreateIndex(
                name: "IX_learning_resources_auto_url",
                table: "learning_resources",
                column: "Url",
                unique: true,
                filter: "\"IsActive\" AND (\"Url\" LIKE 'https://www.youtube.com/results?search_query=%' OR \"Url\" LIKE 'https://www.google.com/search?q=%')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_student_talent_profiles_StudentId",
                table: "student_talent_profiles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_roadmap_nodes_Status",
                table: "roadmap_nodes");

            migrationBuilder.DropIndex(
                name: "IX_learning_resources_auto_url",
                table: "learning_resources");

            migrationBuilder.CreateIndex(
                name: "IX_student_talent_profiles_StudentId",
                table: "student_talent_profiles",
                column: "StudentId");
        }
    }
}
