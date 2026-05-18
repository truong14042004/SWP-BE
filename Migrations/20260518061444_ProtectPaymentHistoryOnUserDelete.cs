using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP_BE.Migrations
{
    /// <inheritdoc />
    public partial class ProtectPaymentHistoryOnUserDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_invoices_users_UserId",
                table: "invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_payment_transactions_users_UserId",
                table: "payment_transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_subscriptions_users_UserId",
                table: "subscriptions");

            migrationBuilder.AddForeignKey(
                name: "FK_invoices_users_UserId",
                table: "invoices",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_payment_transactions_users_UserId",
                table: "payment_transactions",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_subscriptions_users_UserId",
                table: "subscriptions",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_invoices_users_UserId",
                table: "invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_payment_transactions_users_UserId",
                table: "payment_transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_subscriptions_users_UserId",
                table: "subscriptions");

            migrationBuilder.AddForeignKey(
                name: "FK_invoices_users_UserId",
                table: "invoices",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_payment_transactions_users_UserId",
                table: "payment_transactions",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_subscriptions_users_UserId",
                table: "subscriptions",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
