using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LivreTom.Migrations
{
    /// <inheritdoc />
    public partial class AddDownloadConfirmedToMusicOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DownloadConfirmed",
                table: "MusicOrders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DownloadConfirmedAt",
                table: "MusicOrders",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DownloadConfirmed",
                table: "MusicOrders");

            migrationBuilder.DropColumn(
                name: "DownloadConfirmedAt",
                table: "MusicOrders");
        }
    }
}
