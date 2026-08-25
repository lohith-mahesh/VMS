using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RRVMS.Api.Migrations
{
    /// <inheritdoc />
    public partial class WorkflowVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VisitorRequests_CurrentStatus",
                table: "VisitorRequests");

            migrationBuilder.RenameColumn(
                name: "Purpose",
                table: "VisitorRequests",
                newName: "SiteTimezone");

            migrationBuilder.RenameColumn(
                name: "CurrentStatus",
                table: "VisitorRequests",
                newName: "AreasToVisit");

            migrationBuilder.RenameColumn(
                name: "AccompanyingEmployeeId",
                table: "VisitorRequests",
                newName: "VisitorFormId");

            migrationBuilder.RenameColumn(
                name: "SecurityUserId",
                table: "VisitCheckOuts",
                newName: "ReceptionUserId");

            migrationBuilder.RenameColumn(
                name: "SecurityUserId",
                table: "VisitCheckIns",
                newName: "ReceptionUserId");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ApprovedAt",
                table: "VisitorRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DpsPerformedBy",
                table: "VisitorRequests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DpsRecordId",
                table: "VisitorRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EscortingHostId",
                table: "VisitorRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "MainHostChangedAt",
                table: "VisitorRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NumberOfVisitors",
                table: "VisitorRequests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "PersonnelChangeRequested",
                table: "VisitorRequests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PersonnelChangeRequestedAt",
                table: "VisitorRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PreviousMainHostId",
                table: "VisitorRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RejectedAt",
                table: "VisitorRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "VisitorRequests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "VisitorRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VisitorType",
                table: "VisitorRequests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AttendanceRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VisitorRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    VisitDayId = table.Column<Guid>(type: "uuid", nullable: true),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Completed = table.Column<bool>(type: "boolean", nullable: false),
                    MarkedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    MarkedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Comments = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_VisitDays_VisitDayId",
                        column: x => x.VisitDayId,
                        principalTable: "VisitDays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_VisitorRequests_VisitorRequestId",
                        column: x => x.VisitorRequestId,
                        principalTable: "VisitorRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VisitorRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommentType = table.Column<int>(type: "integer", nullable: false),
                    CommentText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comments_Users_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Comments_VisitorRequests_VisitorRequestId",
                        column: x => x.VisitorRequestId,
                        principalTable: "VisitorRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VisitorForms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VisitorRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    VisitorId = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    Citizenship = table.Column<string>(type: "text", nullable: false),
                    Country = table.Column<string>(type: "text", nullable: false),
                    Designation = table.Column<string>(type: "text", nullable: false),
                    CompanyName = table.Column<string>(type: "text", nullable: false),
                    OfficeCity = table.Column<string>(type: "text", nullable: false),
                    OfficeCountry = table.Column<string>(type: "text", nullable: false),
                    Telephone = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    IdType = table.Column<string>(type: "text", nullable: false),
                    IdLast4 = table.Column<string>(type: "text", nullable: false),
                    DeclaredAssets = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitorForms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisitorForms_VisitorRequests_VisitorRequestId",
                        column: x => x.VisitorRequestId,
                        principalTable: "VisitorRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VisitorForms_Visitors_VisitorId",
                        column: x => x.VisitorId,
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VisitorFormVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VisitorRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    VisitorFormId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    FullNameSnapshot = table.Column<string>(type: "text", nullable: false),
                    CitizenshipSnapshot = table.Column<string>(type: "text", nullable: false),
                    CountrySnapshot = table.Column<string>(type: "text", nullable: false),
                    CompanySnapshot = table.Column<string>(type: "text", nullable: false),
                    OfficeCitySnapshot = table.Column<string>(type: "text", nullable: false),
                    OfficeCountrySnapshot = table.Column<string>(type: "text", nullable: false),
                    DesignationSnapshot = table.Column<string>(type: "text", nullable: false),
                    PhoneSnapshot = table.Column<string>(type: "text", nullable: false),
                    EmailSnapshot = table.Column<string>(type: "text", nullable: false),
                    IdTypeSnapshot = table.Column<string>(type: "text", nullable: false),
                    IdLast4Snapshot = table.Column<string>(type: "text", nullable: false),
                    AssetsSnapshot = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitorFormVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdditionalInformationRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VisitorRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    VisitorFormId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestedFields = table.Column<string>(type: "text", nullable: false),
                    RequestComment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    RespondedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResponseSummary = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdditionalInformationRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdditionalInformationRequests_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdditionalInformationRequests_VisitorForms_VisitorFormId",
                        column: x => x.VisitorFormId,
                        principalTable: "VisitorForms",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AdditionalInformationRequests_VisitorRequests_VisitorReques~",
                        column: x => x.VisitorRequestId,
                        principalTable: "VisitorRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VisitorRequests_Status",
                table: "VisitorRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorRequests_UserId",
                table: "VisitorRequests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ECReviews_ReviewerId",
                table: "ECReviews",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_AdditionalInformationRequests_RequestedByUserId",
                table: "AdditionalInformationRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AdditionalInformationRequests_VisitorFormId",
                table: "AdditionalInformationRequests",
                column: "VisitorFormId");

            migrationBuilder.CreateIndex(
                name: "IX_AdditionalInformationRequests_VisitorRequestId",
                table: "AdditionalInformationRequests",
                column: "VisitorRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_VisitDayId",
                table: "AttendanceRecords",
                column: "VisitDayId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_VisitorRequestId_Category",
                table: "AttendanceRecords",
                columns: new[] { "VisitorRequestId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_AuthorUserId",
                table: "Comments",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_VisitorRequestId",
                table: "Comments",
                column: "VisitorRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorForms_VisitorId",
                table: "VisitorForms",
                column: "VisitorId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorForms_VisitorRequestId",
                table: "VisitorForms",
                column: "VisitorRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VisitorFormVersions_VisitorRequestId_Version",
                table: "VisitorFormVersions",
                columns: new[] { "VisitorRequestId", "Version" });

            migrationBuilder.Sql("""
                INSERT INTO "Users" ("Id", "EmployeeNumber", "FullName", "Email", "Role", "IsActive", "CreatedAt", "UpdatedAt")
                SELECT DISTINCT review."ReviewerId", 'legacy-' || review."ReviewerId"::text, 'Legacy Export Control Reviewer', 'legacy-' || review."ReviewerId"::text || '@rrvms.invalid', 1, TRUE, NOW(), NOW()
                FROM "ECReviews" review
                LEFT JOIN "Users" user_record ON user_record."Id" = review."ReviewerId"
                WHERE user_record."Id" IS NULL;
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_ECReviews_Users_ReviewerId",
                table: "ECReviews",
                column: "ReviewerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VisitorRequests_Users_UserId",
                table: "VisitorRequests",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ECReviews_Users_ReviewerId",
                table: "ECReviews");

            migrationBuilder.DropForeignKey(
                name: "FK_VisitorRequests_Users_UserId",
                table: "VisitorRequests");

            migrationBuilder.DropTable(
                name: "AdditionalInformationRequests");

            migrationBuilder.DropTable(
                name: "AttendanceRecords");

            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.DropTable(
                name: "VisitorFormVersions");

            migrationBuilder.DropTable(
                name: "VisitorForms");

            migrationBuilder.DropIndex(
                name: "IX_VisitorRequests_Status",
                table: "VisitorRequests");

            migrationBuilder.DropIndex(
                name: "IX_VisitorRequests_UserId",
                table: "VisitorRequests");

            migrationBuilder.DropIndex(
                name: "IX_ECReviews_ReviewerId",
                table: "ECReviews");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "VisitorRequests");

            migrationBuilder.DropColumn(
                name: "DpsPerformedBy",
                table: "VisitorRequests");

            migrationBuilder.DropColumn(
                name: "DpsRecordId",
                table: "VisitorRequests");

            migrationBuilder.DropColumn(
                name: "EscortingHostId",
                table: "VisitorRequests");

            migrationBuilder.DropColumn(
                name: "MainHostChangedAt",
                table: "VisitorRequests");

            migrationBuilder.DropColumn(
                name: "NumberOfVisitors",
                table: "VisitorRequests");

            migrationBuilder.DropColumn(
                name: "PersonnelChangeRequested",
                table: "VisitorRequests");

            migrationBuilder.DropColumn(
                name: "PersonnelChangeRequestedAt",
                table: "VisitorRequests");

            migrationBuilder.DropColumn(
                name: "PreviousMainHostId",
                table: "VisitorRequests");

            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "VisitorRequests");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "VisitorRequests");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "VisitorRequests");

            migrationBuilder.DropColumn(
                name: "VisitorType",
                table: "VisitorRequests");

            migrationBuilder.RenameColumn(
                name: "VisitorFormId",
                table: "VisitorRequests",
                newName: "AccompanyingEmployeeId");

            migrationBuilder.RenameColumn(
                name: "SiteTimezone",
                table: "VisitorRequests",
                newName: "Purpose");

            migrationBuilder.RenameColumn(
                name: "AreasToVisit",
                table: "VisitorRequests",
                newName: "CurrentStatus");

            migrationBuilder.RenameColumn(
                name: "ReceptionUserId",
                table: "VisitCheckOuts",
                newName: "SecurityUserId");

            migrationBuilder.RenameColumn(
                name: "ReceptionUserId",
                table: "VisitCheckIns",
                newName: "SecurityUserId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorRequests_CurrentStatus",
                table: "VisitorRequests",
                column: "CurrentStatus");
        }
    }
}
