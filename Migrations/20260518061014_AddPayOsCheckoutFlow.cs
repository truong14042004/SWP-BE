using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP_BE.Migrations
{
    /// <inheritdoc />
    public partial class AddPayOsCheckoutFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "subscription_plans",
                columns: new[] { "Id", "BillingCycle", "CreatedAt", "Currency", "Description", "FeaturesJson", "IsActive", "Name", "UpdatedAt" },
                values: new object[] { new Guid("44444444-4444-4444-4444-444444444101"), "Free", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "VND", "Free access with 2 mentor reviews.", "{\"mentorReviewLimit\":2,\"features\":[\"2 mentor reviews\",\"Basic roadmap resources\"]}", true, "Free", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.InsertData(
                table: "subscription_plans",
                columns: new[] { "Id", "BillingCycle", "CreatedAt", "Currency", "Description", "FeaturesJson", "IsActive", "Name", "Price", "UpdatedAt" },
                values: new object[] { new Guid("44444444-4444-4444-4444-444444444102"), "Monthly", new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "VND", "Monthly access with 5 mentor reviews.", "{\"mentorReviewLimit\":5,\"features\":[\"5 mentor reviews per month\",\"AI mentor chat\",\"GitHub analysis\",\"Roadmap resources\"]}", true, "Premium Monthly", 99000m, new DateTimeOffset(new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "subscription_plans",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444101"));

            migrationBuilder.DeleteData(
                table: "subscription_plans",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444102"));
        }
    }
}
