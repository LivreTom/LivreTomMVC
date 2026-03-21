using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LivreTom.Migrations
{
    /// <inheritdoc />
    public partial class AddSunoSongIdAndTitleToMusicOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataProtectionKeys");

            migrationBuilder.RenameColumn(
                name: "ResultUrl",
                table: "MusicOrders",
                newName: "Title");

            migrationBuilder.AddColumn<string>(
                name: "AudioUrl",
                table: "MusicOrders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoverImageUrl",
                table: "MusicOrders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Lyrics",
                table: "MusicOrders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SunoSongId",
                table: "MusicOrders",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AudioUrl",
                table: "MusicOrders");

            migrationBuilder.DropColumn(
                name: "CoverImageUrl",
                table: "MusicOrders");

            migrationBuilder.DropColumn(
                name: "Lyrics",
                table: "MusicOrders");

            migrationBuilder.DropColumn(
                name: "SunoSongId",
                table: "MusicOrders");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "MusicOrders",
                newName: "ResultUrl");

            migrationBuilder.CreateTable(
                name: "DataProtectionKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FriendlyName = table.Column<string>(type: "text", nullable: true),
                    Xml = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProtectionKeys", x => x.Id);
                });
        }
    }
}
