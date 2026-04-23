using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LivreTom.Migrations
{
    /// <inheritdoc />
    public partial class AddPlaceholderToStepQuestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "StepQuestions"
                ADD COLUMN IF NOT EXISTS "Placeholder" text;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "StepQuestions"
                DROP COLUMN IF EXISTS "Placeholder";
                """);
        }
    }
}
