using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessDirectory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PendingUserModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Businesses_Users_OwnerId",
                table: "Businesses");

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Businesses_BusinessId",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_Comments_BusinessId",
                table: "Comments");

            migrationBuilder.RenameColumn(
                name: "OwnerId",
                table: "Businesses",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Businesses_OwnerId",
                table: "Businesses",
                newName: "IX_Businesses_UserId");

            migrationBuilder.AddColumn<int>(
                name: "BusinessId1",
                table: "Comments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Businesses",
                type: "int",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier")
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_BusinessId1",
                table: "Comments",
                column: "BusinessId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Businesses_Users_UserId",
                table: "Businesses",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Businesses_BusinessId1",
                table: "Comments",
                column: "BusinessId1",
                principalTable: "Businesses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Businesses_Users_UserId",
                table: "Businesses");

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Businesses_BusinessId1",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_Comments_BusinessId1",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "BusinessId1",
                table: "Comments");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Businesses",
                newName: "OwnerId");

            migrationBuilder.RenameIndex(
                name: "IX_Businesses_UserId",
                table: "Businesses",
                newName: "IX_Businesses_OwnerId");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Businesses",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_BusinessId",
                table: "Comments",
                column: "BusinessId");

            migrationBuilder.AddForeignKey(
                name: "FK_Businesses_Users_OwnerId",
                table: "Businesses",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Businesses_BusinessId",
                table: "Comments",
                column: "BusinessId",
                principalTable: "Businesses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
