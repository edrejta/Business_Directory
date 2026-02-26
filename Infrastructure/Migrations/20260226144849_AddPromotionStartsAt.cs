using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessDirectory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPromotionStartsAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "StartsAt",
                table: "Promotions",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StartsAt",
                table: "Promotions");
        }
    }
}
