using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinaGroup.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddedOffWorkPropertiesToSelfEvaluation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOffWork",
                table: "SelfEvaluations",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOffWork",
                table: "SelfEvaluations");
        }
    }
}
