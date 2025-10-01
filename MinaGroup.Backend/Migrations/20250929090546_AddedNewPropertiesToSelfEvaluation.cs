using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinaGroup.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddedNewPropertiesToSelfEvaluation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovedByUserId",
                table: "SelfEvaluations",
                type: "varchar(255)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SickReason",
                table: "SelfEvaluations",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_SelfEvaluations_ApprovedByUserId",
                table: "SelfEvaluations",
                column: "ApprovedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_SelfEvaluations_AspNetUsers_ApprovedByUserId",
                table: "SelfEvaluations",
                column: "ApprovedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SelfEvaluations_AspNetUsers_ApprovedByUserId",
                table: "SelfEvaluations");

            migrationBuilder.DropIndex(
                name: "IX_SelfEvaluations_ApprovedByUserId",
                table: "SelfEvaluations");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                table: "SelfEvaluations");

            migrationBuilder.DropColumn(
                name: "SickReason",
                table: "SelfEvaluations");
        }
    }
}
