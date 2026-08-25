using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RRVMS.Api.Migrations
{
    /// <inheritdoc />
    public partial class VisitorFormSubmissionState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Nationality",
                table: "Visitors",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "VisitorRequestId",
                table: "Visitors",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Purpose",
                table: "VisitorRequests",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NationalitySnapshot",
                table: "VisitorFormVersions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Nationality",
                table: "VisitorForms",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "VisitorId",
                table: "Assets",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Visitors_VisitorRequestId",
                table: "Visitors",
                column: "VisitorRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_VisitorId",
                table: "Assets",
                column: "VisitorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assets_Visitors_VisitorId",
                table: "Assets",
                column: "VisitorId",
                principalTable: "Visitors",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assets_Visitors_VisitorId",
                table: "Assets");

            migrationBuilder.DropIndex(
                name: "IX_Visitors_VisitorRequestId",
                table: "Visitors");

            migrationBuilder.DropIndex(
                name: "IX_Assets_VisitorId",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "Nationality",
                table: "Visitors");

            migrationBuilder.DropColumn(
                name: "VisitorRequestId",
                table: "Visitors");

            migrationBuilder.DropColumn(
                name: "Purpose",
                table: "VisitorRequests");

            migrationBuilder.DropColumn(
                name: "NationalitySnapshot",
                table: "VisitorFormVersions");

            migrationBuilder.DropColumn(
                name: "Nationality",
                table: "VisitorForms");

            migrationBuilder.DropColumn(
                name: "VisitorId",
                table: "Assets");
        }
    }
}
