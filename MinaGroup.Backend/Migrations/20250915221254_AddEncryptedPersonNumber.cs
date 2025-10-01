using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinaGroup.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddEncryptedPersonNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PersonNumberCPR",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<string>(
                name: "EncryptedPersonNumber",
                table: "AspNetUsers",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PersonNumberHash",
                table: "AspNetUsers",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptedPersonNumber",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PersonNumberHash",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<string>(
                name: "PersonNumberCPR",
                table: "AspNetUsers",
                type: "varchar(11)",
                maxLength: 11,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
