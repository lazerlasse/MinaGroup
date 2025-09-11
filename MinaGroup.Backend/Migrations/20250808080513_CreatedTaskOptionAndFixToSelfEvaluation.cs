using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinaGroup.Backend.Migrations
{
    /// <inheritdoc />
    public partial class CreatedTaskOptionAndFixToSelfEvaluation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Activities",
                table: "SelfEvaluations");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "SelfEvaluations",
                newName: "LastUpdated");

            migrationBuilder.RenameColumn(
                name: "BreakInfo",
                table: "SelfEvaluations",
                newName: "CommentFromLeader");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "TotalHours",
                table: "SelfEvaluations",
                type: "time(6)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "DepartureTime",
                table: "SelfEvaluations",
                type: "time(6)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "ArrivalTime",
                table: "SelfEvaluations",
                type: "time(6)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "BreakDuration",
                table: "SelfEvaluations",
                type: "time(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EvaluationDate",
                table: "SelfEvaluations",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsSick",
                table: "SelfEvaluations",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "TaskOptions",
                columns: table => new
                {
                    TaskOptionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TaskName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskOptions", x => x.TaskOptionId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SelfEvaluationTasks",
                columns: table => new
                {
                    SelectedTaskTaskOptionId = table.Column<int>(type: "int", nullable: false),
                    SelfEvaluationId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SelfEvaluationTasks", x => new { x.SelectedTaskTaskOptionId, x.SelfEvaluationId });
                    table.ForeignKey(
                        name: "FK_SelfEvaluationTasks_SelfEvaluations_SelfEvaluationId",
                        column: x => x.SelfEvaluationId,
                        principalTable: "SelfEvaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SelfEvaluationTasks_TaskOptions_SelectedTaskTaskOptionId",
                        column: x => x.SelectedTaskTaskOptionId,
                        principalTable: "TaskOptions",
                        principalColumn: "TaskOptionId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_SelfEvaluationTasks_SelfEvaluationId",
                table: "SelfEvaluationTasks",
                column: "SelfEvaluationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SelfEvaluationTasks");

            migrationBuilder.DropTable(
                name: "TaskOptions");

            migrationBuilder.DropColumn(
                name: "BreakDuration",
                table: "SelfEvaluations");

            migrationBuilder.DropColumn(
                name: "EvaluationDate",
                table: "SelfEvaluations");

            migrationBuilder.DropColumn(
                name: "IsSick",
                table: "SelfEvaluations");

            migrationBuilder.RenameColumn(
                name: "LastUpdated",
                table: "SelfEvaluations",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "CommentFromLeader",
                table: "SelfEvaluations",
                newName: "BreakInfo");

            migrationBuilder.AlterColumn<string>(
                name: "TotalHours",
                table: "SelfEvaluations",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "time(6)")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "DepartureTime",
                table: "SelfEvaluations",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "time(6)")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "ArrivalTime",
                table: "SelfEvaluations",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "time(6)")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Activities",
                table: "SelfEvaluations",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
