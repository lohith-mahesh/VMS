using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RRVMS.Api.Migrations
{
    /// <inheritdoc />
    public partial class BatchVisitorForms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VisitorForms_VisitorRequestId",
                table: "VisitorForms");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorForms_VisitorRequestId",
                table: "VisitorForms",
                column: "VisitorRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VisitorForms_VisitorRequestId",
                table: "VisitorForms");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorForms_VisitorRequestId",
                table: "VisitorForms",
                column: "VisitorRequestId",
                unique: true);
        }
    }
}
