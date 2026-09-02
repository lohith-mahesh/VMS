using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RRVMS.Api.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdLast4",
                table: "Visitors");

            migrationBuilder.DropColumn(
                name: "Nationality",
                table: "Visitors");

            migrationBuilder.DropColumn(
                name: "IdLast4Snapshot",
                table: "VisitorFormVersions");

            migrationBuilder.DropColumn(
                name: "IdLast4",
                table: "VisitorForms");

            migrationBuilder.RenameColumn(
                name: "NationalitySnapshot",
                table: "VisitorFormVersions",
                newName: "OtherIdTypeSnapshot");

            migrationBuilder.RenameColumn(
                name: "Nationality",
                table: "VisitorForms",
                newName: "OtherIdType");

            migrationBuilder.AddColumn<string>(
                name: "BadgeColor",
                table: "VisitorRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VisitingCompanyAddressCountry",
                table: "VisitorRequests",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_Comments_AuthorUserId\" ON \"Comments\" (\"AuthorUserId\");");

            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_AdditionalInformationRequests_RequestedByUserId\" ON \"AdditionalInformationRequests\" (\"RequestedByUserId\");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Comments_AuthorUserId",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_AdditionalInformationRequests_RequestedByUserId",
                table: "AdditionalInformationRequests");

            migrationBuilder.DropColumn(
                name: "BadgeColor",
                table: "VisitorRequests");

            migrationBuilder.DropColumn(
                name: "VisitingCompanyAddressCountry",
                table: "VisitorRequests");

            migrationBuilder.RenameColumn(
                name: "OtherIdTypeSnapshot",
                table: "VisitorFormVersions",
                newName: "NationalitySnapshot");

            migrationBuilder.RenameColumn(
                name: "OtherIdType",
                table: "VisitorForms",
                newName: "Nationality");

            migrationBuilder.AddColumn<string>(
                name: "IdLast4",
                table: "Visitors",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Nationality",
                table: "Visitors",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IdLast4Snapshot",
                table: "VisitorFormVersions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IdLast4",
                table: "VisitorForms",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
