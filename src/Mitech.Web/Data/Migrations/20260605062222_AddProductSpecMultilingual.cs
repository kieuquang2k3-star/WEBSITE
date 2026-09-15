using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mitech.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductSpecMultilingual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SpecDimensionEn",
                table: "Products",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SpecDimensionVi",
                table: "Products",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SpecMaterialEn",
                table: "Products",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SpecMaterialVi",
                table: "Products",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SpecNameEn",
                table: "Products",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SpecNameVi",
                table: "Products",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            // Backfill EN/VI spec data for existing seeded products (matched by SortOrder)
            migrationBuilder.Sql("UPDATE Products SET SpecNameEn='Body',        SpecNameVi='Thân',                SpecMaterialEn='Aluminum 6000 series', SpecMaterialVi='Nhôm hợp kim 6000', SpecDimensionEn='Total length 70mm',  SpecDimensionVi='Chiều dài 70mm'         WHERE SortOrder=1");
            migrationBuilder.Sql("UPDATE Products SET SpecNameEn='Body',        SpecNameVi='Thân',                SpecMaterialEn='Electromagnetic SUS',   SpecMaterialVi='Thép không gỉ điện từ', SpecDimensionEn='Outer dia. φ32',     SpecDimensionVi='Đường kính ngoài φ32'  WHERE SortOrder=2");
            migrationBuilder.Sql("UPDATE Products SET SpecNameEn='Valve ASSY (press-fit)', SpecNameVi='Cụm van (ép nguội)', SpecMaterialEn='Aluminum 6000 series', SpecMaterialVi='Nhôm hợp kim 6000', SpecDimensionEn='Outer dia. φ13', SpecDimensionVi='Đường kính ngoài φ13' WHERE SortOrder=3");
            migrationBuilder.Sql("UPDATE Products SET SpecNameEn='Shaft',       SpecNameVi='Trục',                SpecMaterialEn='Iron',                 SpecMaterialVi='Vật liệu sắt',          SpecDimensionEn='Outer dia. φ18',     SpecDimensionVi='Đường kính ngoài φ18'  WHERE SortOrder=4");
            migrationBuilder.Sql("UPDATE Products SET SpecNameEn='Shaft',       SpecNameVi='Trục',                SpecMaterialEn='Steel',                SpecMaterialVi='Thép',                  SpecDimensionEn='Outer dia. φ20',     SpecDimensionVi='Đường kính ngoài φ20'  WHERE SortOrder=5");
            migrationBuilder.Sql("UPDATE Products SET SpecNameEn='Valve',       SpecNameVi='Van',                 SpecMaterialEn='Aluminum 6000 series', SpecMaterialVi='Nhôm hợp kim 6000',     SpecDimensionEn='Outer dia. φ14',     SpecDimensionVi='Đường kính ngoài φ14'  WHERE SortOrder=6");
            migrationBuilder.Sql("UPDATE Products SET SpecNameEn='Apex Seal',   SpecNameVi='Con dấu đỉnh',        SpecMaterialEn='Special iron',         SpecMaterialVi='Thép đặc biệt',         SpecDimensionEn='Total length 85mm',  SpecDimensionVi='Chiều dài 85mm'         WHERE SortOrder=7");
            migrationBuilder.Sql("UPDATE Products SET SpecNameEn='Pin',         SpecNameVi='Chốt',                SpecMaterialEn='SUS304',               SpecMaterialVi='SUS304',                SpecDimensionEn='Outer dia. φ4',      SpecDimensionVi='Đường kính ngoài φ4'   WHERE SortOrder=8");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SpecDimensionEn",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SpecDimensionVi",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SpecMaterialEn",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SpecMaterialVi",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SpecNameEn",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SpecNameVi",
                table: "Products");
        }
    }
}
