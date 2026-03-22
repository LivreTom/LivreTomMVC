using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LivreTom.Migrations
{
    /// <inheritdoc />
    public partial class AddCancelReasonToTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancelReason",
                table: "SupportTickets",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancelReason",
                table: "SupportTickets");
        }
    }
}
