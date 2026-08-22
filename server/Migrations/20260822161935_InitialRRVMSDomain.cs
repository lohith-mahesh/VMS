using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RRVMS.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialRRVMSDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    EntityType = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    PerformedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Details = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Badges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BadgeNumber = table.Column<string>(type: "text", nullable: false),
                    VisitorId = table.Column<Guid>(type: "uuid", nullable: false),
                    VisitDayId = table.Column<Guid>(type: "uuid", nullable: false),
                    IssuedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReturnedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Badges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeNumber = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VisitCheckIns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VisitDayId = table.Column<Guid>(type: "uuid", nullable: false),
                    BadgeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SecurityUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PhysicalIdVerified = table.Column<bool>(type: "boolean", nullable: false),
                    AssetsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    CheckedInAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitCheckIns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VisitCheckOuts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VisitDayId = table.Column<Guid>(type: "uuid", nullable: false),
                    BadgeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SecurityUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BadgeReturned = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: false),
                    CheckedOutAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitCheckOuts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Visitors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    CompanyName = table.Column<string>(type: "text", nullable: false),
                    Citizenship = table.Column<string>(type: "text", nullable: false),
                    Country = table.Column<string>(type: "text", nullable: false),
                    Designation = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: false),
                    IdType = table.Column<string>(type: "text", nullable: false),
                    IdLast4 = table.Column<string>(type: "text", nullable: false),
                    VisitorType = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Visitors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VisitorRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestNumber = table.Column<string>(type: "text", nullable: false),
                    VisitorId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequesterId = table.Column<Guid>(type: "uuid", nullable: false),
                    MainHostId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccompanyingEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Purpose = table.Column<string>(type: "text", nullable: false),
                    VisitingCompany = table.Column<string>(type: "text", nullable: false),
                    VisitingSite = table.Column<string>(type: "text", nullable: false),
                    VisitPurposeType = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    CurrentStatus = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitorRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisitorRequests_Visitors_VisitorId",
                        column: x => x.VisitorId,
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VisitorRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetType = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    SerialNumber = table.Column<string>(type: "text", nullable: false),
                    IsDeclared = table.Column<bool>(type: "boolean", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    VerificationStatus = table.Column<int>(type: "integer", nullable: false),
                    DetectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assets_VisitorRequests_VisitorRequestId",
                        column: x => x.VisitorRequestId,
                        principalTable: "VisitorRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VisitorRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    ECReviewId = table.Column<Guid>(type: "uuid", nullable: true),
                    DocumentType = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    StorageReference = table.Column<string>(type: "text", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documents_VisitorRequests_VisitorRequestId",
                        column: x => x.VisitorRequestId,
                        principalTable: "VisitorRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DPSRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VisitorRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    PerformedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    PerformedByType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Result = table.Column<int>(type: "integer", nullable: false),
                    OCRReference = table.Column<string>(type: "text", nullable: true),
                    ReportDocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    PerformedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DPSRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DPSRecords_VisitorRequests_VisitorRequestId",
                        column: x => x.VisitorRequestId,
                        principalTable: "VisitorRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ECReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VisitorRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Decision = table.Column<int>(type: "integer", nullable: false),
                    Comments = table.Column<string>(type: "text", nullable: false),
                    RequestedDocuments = table.Column<string>(type: "text", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ECReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ECReviews_VisitorRequests_VisitorRequestId",
                        column: x => x.VisitorRequestId,
                        principalTable: "VisitorRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VisitDays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VisitorRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    VisitDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpectedArrivalTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    ExpectedDepartureTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ActualArrivalTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ActualDepartureTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NoShowMarkedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisitDays_VisitorRequests_VisitorRequestId",
                        column: x => x.VisitorRequestId,
                        principalTable: "VisitorRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assets_VisitorRequestId",
                table: "Assets",
                column: "VisitorRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_VisitorRequestId",
                table: "Documents",
                column: "VisitorRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_DPSRecords_VisitorRequestId",
                table: "DPSRecords",
                column: "VisitorRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ECReviews_Status",
                table: "ECReviews",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ECReviews_VisitorRequestId",
                table: "ECReviews",
                column: "VisitorRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VisitDays_Status",
                table: "VisitDays",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_VisitDays_VisitDate",
                table: "VisitDays",
                column: "VisitDate");

            migrationBuilder.CreateIndex(
                name: "IX_VisitDays_VisitorRequestId",
                table: "VisitDays",
                column: "VisitorRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorRequests_CurrentStatus",
                table: "VisitorRequests",
                column: "CurrentStatus");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorRequests_MainHostId",
                table: "VisitorRequests",
                column: "MainHostId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorRequests_RequestNumber",
                table: "VisitorRequests",
                column: "RequestNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VisitorRequests_VisitorId",
                table: "VisitorRequests",
                column: "VisitorId");

            migrationBuilder.CreateIndex(
                name: "IX_Visitors_CompanyName",
                table: "Visitors",
                column: "CompanyName");

            migrationBuilder.CreateIndex(
                name: "IX_Visitors_FullName",
                table: "Visitors",
                column: "FullName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Assets");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "Badges");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "DPSRecords");

            migrationBuilder.DropTable(
                name: "ECReviews");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "VisitCheckIns");

            migrationBuilder.DropTable(
                name: "VisitCheckOuts");

            migrationBuilder.DropTable(
                name: "VisitDays");

            migrationBuilder.DropTable(
                name: "VisitorRequests");

            migrationBuilder.DropTable(
                name: "Visitors");
        }
    }
}
