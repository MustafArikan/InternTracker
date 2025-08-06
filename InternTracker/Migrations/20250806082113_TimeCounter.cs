using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternTracker.Migrations
{
    /// <inheritdoc />
    public partial class TimeCounter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TaskId",
                table: "WorkSessions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkSessions_TaskId",
                table: "WorkSessions",
                column: "TaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkSessions_TaskItems_TaskId",
                table: "WorkSessions",
                column: "TaskId",
                principalTable: "TaskItems",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkSessions_TaskItems_TaskId",
                table: "WorkSessions");

            migrationBuilder.DropIndex(
                name: "IX_WorkSessions_TaskId",
                table: "WorkSessions");

            migrationBuilder.DropColumn(
                name: "TaskId",
                table: "WorkSessions");
        }
    }
}
