using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mitech.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddJobPositions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JobPositions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TitleVi = table.Column<string>(type: "TEXT", nullable: false),
                    TitleJa = table.Column<string>(type: "TEXT", nullable: false),
                    TitleEn = table.Column<string>(type: "TEXT", nullable: false),
                    ShortDescVi = table.Column<string>(type: "TEXT", nullable: false),
                    ShortDescJa = table.Column<string>(type: "TEXT", nullable: false),
                    ShortDescEn = table.Column<string>(type: "TEXT", nullable: false),
                    DetailVi = table.Column<string>(type: "TEXT", nullable: false),
                    DetailJa = table.Column<string>(type: "TEXT", nullable: false),
                    DetailEn = table.Column<string>(type: "TEXT", nullable: false),
                    SalaryVi = table.Column<string>(type: "TEXT", nullable: false),
                    SalaryJa = table.Column<string>(type: "TEXT", nullable: false),
                    SalaryEn = table.Column<string>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobPositions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobPositions_Slug",
                table: "JobPositions",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobPositions_SortOrder",
                table: "JobPositions",
                column: "SortOrder");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobPositions");
        }
    }
}
