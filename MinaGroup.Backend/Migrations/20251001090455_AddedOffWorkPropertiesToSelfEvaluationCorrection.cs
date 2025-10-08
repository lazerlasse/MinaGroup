using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinaGroup.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddedOffWorkPropertiesToSelfEvaluationCorrection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OffWorkReason",
                table: "SelfEvaluations",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OffWorkReason",
                table: "SelfEvaluations");
        }
    }
}
