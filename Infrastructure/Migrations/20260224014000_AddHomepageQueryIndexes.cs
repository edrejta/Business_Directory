using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessDirectory.Infrastructure.Migrations
{
    public partial class AddHomepageQueryIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Businesses_Status_CreatedAt",
                table: "Businesses",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_BusinessId_CreatedAt",
                table: "Comments",
                columns: new[] { "BusinessId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_BusinessId_IsActive_ExpiresAt_CreatedAt",
                table: "Promotions",
                columns: new[] { "BusinessId", "IsActive", "ExpiresAt", "CreatedAt" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Businesses_Status_CreatedAt",
                table: "Businesses");

            migrationBuilder.DropIndex(
                name: "IX_Comments_BusinessId_CreatedAt",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_Promotions_BusinessId_IsActive_ExpiresAt_CreatedAt",
                table: "Promotions");
        }
    }
}
