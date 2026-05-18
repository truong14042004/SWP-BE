using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP_BE.Migrations
{
    /// <inheritdoc />
    public partial class AddLearningResourceFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "learning_resources",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FileSize",
                table: "learning_resources",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StorageObjectName",
                table: "learning_resources",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333101"),
                columns: new[] { "ContentType", "FileSize", "StorageObjectName" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333102"),
                columns: new[] { "ContentType", "FileSize", "StorageObjectName" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333103"),
                columns: new[] { "ContentType", "FileSize", "StorageObjectName" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333104"),
                columns: new[] { "ContentType", "FileSize", "StorageObjectName" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333105"),
                columns: new[] { "ContentType", "FileSize", "StorageObjectName" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333106"),
                columns: new[] { "ContentType", "FileSize", "StorageObjectName" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333107"),
                columns: new[] { "ContentType", "FileSize", "StorageObjectName" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333108"),
                columns: new[] { "ContentType", "FileSize", "StorageObjectName" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333109"),
                columns: new[] { "ContentType", "FileSize", "StorageObjectName" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333110"),
                columns: new[] { "ContentType", "FileSize", "StorageObjectName" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333111"),
                columns: new[] { "ContentType", "FileSize", "StorageObjectName" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333112"),
                columns: new[] { "ContentType", "FileSize", "StorageObjectName" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333113"),
                columns: new[] { "ContentType", "FileSize", "StorageObjectName" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333114"),
                columns: new[] { "ContentType", "FileSize", "StorageObjectName" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333115"),
                columns: new[] { "ContentType", "FileSize", "StorageObjectName" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333116"),
                columns: new[] { "ContentType", "FileSize", "StorageObjectName" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333117"),
                columns: new[] { "ContentType", "FileSize", "StorageObjectName" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "learning_resources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333118"),
                columns: new[] { "ContentType", "FileSize", "StorageObjectName" },
                values: new object[] { null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "learning_resources");

            migrationBuilder.DropColumn(
                name: "FileSize",
                table: "learning_resources");

            migrationBuilder.DropColumn(
                name: "StorageObjectName",
                table: "learning_resources");
        }
    }
}
