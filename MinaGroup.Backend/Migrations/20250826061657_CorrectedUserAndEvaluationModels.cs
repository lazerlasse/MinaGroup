using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinaGroup.Backend.Migrations
{
    /// <inheritdoc />
    public partial class CorrectedUserAndEvaluationModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<TimeSpan>(
                name: "TotalHours",
                table: "SelfEvaluations",
                type: "time(6)",
                nullable: true,
                oldClrType: typeof(TimeSpan),
                oldType: "time(6)");

            migrationBuilder.AlterColumn<bool>(
                name: "HadDiscomfort",
                table: "SelfEvaluations",
                type: "tinyint(1)",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)");

            migrationBuilder.AlterColumn<bool>(
                name: "HadBreak",
                table: "SelfEvaluations",
                type: "tinyint(1)",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "DepartureTime",
                table: "SelfEvaluations",
                type: "time(6)",
                nullable: true,
                oldClrType: typeof(TimeSpan),
                oldType: "time(6)");

            migrationBuilder.AlterColumn<string>(
                name: "Collaboration",
                table: "SelfEvaluations",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Assistance",
                table: "SelfEvaluations",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "ArrivalTime",
                table: "SelfEvaluations",
                type: "time(6)",
                nullable: true,
                oldClrType: typeof(TimeSpan),
                oldType: "time(6)");

            migrationBuilder.AlterColumn<string>(
                name: "ArrivalStatus",
                table: "SelfEvaluations",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Aid",
                table: "SelfEvaluations",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "JobEndDate",
                table: "AspNetUsers",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "JobStartDate",
                table: "AspNetUsers",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JobEndDate",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "JobStartDate",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "TotalHours",
                table: "SelfEvaluations",
                type: "time(6)",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0),
                oldClrType: typeof(TimeSpan),
                oldType: "time(6)",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "HadDiscomfort",
                table: "SelfEvaluations",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "HadBreak",
                table: "SelfEvaluations",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)",
                oldNullable: true);

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "DepartureTime",
                table: "SelfEvaluations",
                type: "time(6)",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0),
                oldClrType: typeof(TimeSpan),
                oldType: "time(6)",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "SelfEvaluations",
                keyColumn: "Collaboration",
                keyValue: null,
                column: "Collaboration",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "Collaboration",
                table: "SelfEvaluations",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "SelfEvaluations",
                keyColumn: "Assistance",
                keyValue: null,
                column: "Assistance",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "Assistance",
                table: "SelfEvaluations",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "ArrivalTime",
                table: "SelfEvaluations",
                type: "time(6)",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0),
                oldClrType: typeof(TimeSpan),
                oldType: "time(6)",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "SelfEvaluations",
                keyColumn: "ArrivalStatus",
                keyValue: null,
                column: "ArrivalStatus",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "ArrivalStatus",
                table: "SelfEvaluations",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "SelfEvaluations",
                keyColumn: "Aid",
                keyValue: null,
                column: "Aid",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "Aid",
                table: "SelfEvaluations",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
