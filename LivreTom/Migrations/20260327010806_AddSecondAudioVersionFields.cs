using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LivreTom.Migrations
{
    /// <inheritdoc />
    public partial class AddSecondAudioVersionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "MusicOrders"
                ADD COLUMN IF NOT EXISTS "AudioUrlV2" text;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "MusicOrders"
                ADD COLUMN IF NOT EXISTS "SunoSongIdV2" text;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "MusicOrders"
                DROP COLUMN IF EXISTS "AudioUrlV2";
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "MusicOrders"
                DROP COLUMN IF EXISTS "SunoSongIdV2";
                """);
        }
    }
}
