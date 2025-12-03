using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinaGroup.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskOptionOrganization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "TaskOptions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Town",
                keyValue: null,
                column: "Town",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "Town",
                table: "Organizations",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "OrganizationAdress",
                keyValue: null,
                column: "OrganizationAdress",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "OrganizationAdress",
                table: "Organizations",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_TaskOptions_OrganizationId",
                table: "TaskOptions",
                column: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskOptions_Organizations_OrganizationId",
                table: "TaskOptions",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskOptions_Organizations_OrganizationId",
                table: "TaskOptions");

            migrationBuilder.DropIndex(
                name: "IX_TaskOptions_OrganizationId",
                table: "TaskOptions");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "TaskOptions");

            migrationBuilder.AlterColumn<string>(
                name: "Town",
                table: "Organizations",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "OrganizationAdress",
                table: "Organizations",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
