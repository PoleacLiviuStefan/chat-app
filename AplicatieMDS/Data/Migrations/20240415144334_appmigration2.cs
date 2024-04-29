using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AplicatieMDS.Data.Migrations
{
    public partial class appmigration2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdditionalInfoJson",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdditionalInfoJson",
                table: "AspNetUsers");
        }
    }
}
