using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RRVMS.Api.Models;

namespace RRVMS.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(RrvmsDbContext db, IWebHostEnvironment environment, CancellationToken cancellationToken = default)
    {
        var demoEnabled = string.Equals(Environment.GetEnvironmentVariable("DEMO_DATA_ENABLED"), "true", StringComparison.OrdinalIgnoreCase);
        if (!environment.IsDevelopment() && !demoEnabled)
        {
            return;
        }

        var existingDemoReq = await db.VisitorRequests
            .Include(r => r.Visitor)
            .FirstOrDefaultAsync(r => r.RequestNumber == "RRVMS-2026-000001", cancellationToken);

        if (existingDemoReq != null && existingDemoReq.Visitor != null && existingDemoReq.Visitor.FullName == "Adam Gilchrist")
        {
            // Already seeded with Adam Gilchrist demo data
            return;
        }

        // If old dummy RRVMS-2026-000001 exists with another visitor name, re-number it to avoid primary key/unique constraint conflict
        if (existingDemoReq != null)
        {
            existingDemoReq.RequestNumber = $"RRVMS-2026-LEGACY-{existingDemoReq.Id.ToString()[..6]}";
            await db.SaveChangesAsync(cancellationToken);
        }

        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.DateTime);

        // 1. SEED USERS
        var hostUser = new User
        {
            Id = StableGuid("prototype-host-requester"),
            EmployeeNumber = "RR-01001",
            FullName = "Alex Morgan",
            Email = "alex.morgan@rolls-royce.com",
            Role = UserRole.HOST_REQUESTER,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        var ecUser = new User
        {
            Id = StableGuid("prototype-export-control"),
            EmployeeNumber = "RR-02002",
            FullName = "Priya Shah",
            Email = "priya.shah@rolls-royce.com",
            Role = UserRole.EXPORT_CONTROL,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        var receptionUser = new User
        {
            Id = StableGuid("prototype-reception"),
            EmployeeNumber = "RR-03003",
            FullName = "Michael Brown",
            Email = "michael.brown@rolls-royce.com",
            Role = UserRole.RECEPTION,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        var escortUser = new User
        {
            Id = StableGuid("prototype-escort-host"),
            EmployeeNumber = "RR-04004",
            FullName = "Sarah Jenkins",
            Email = "sarah.jenkins@rolls-royce.com",
            Role = UserRole.HOST_REQUESTER,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        foreach (var user in new[] { hostUser, ecUser, receptionUser, escortUser })
        {
            if (!await db.Users.AnyAsync(u => u.Id == user.Id, cancellationToken))
            {
                db.Users.Add(user);
            }
        }
        await db.SaveChangesAsync(cancellationToken);

        // 2. SEED SHARED/PRIMARY VISITOR RECORD (Adam Gilchrist)
        var visitorId = StableGuid("demo-visitor-adam-gilchrist");
        var visitor = await db.Visitors.FirstOrDefaultAsync(v => v.Id == visitorId, cancellationToken);
        if (visitor == null)
        {
            visitor = new Visitor
            {
                Id = visitorId,
                FullName = "Adam Gilchrist",
                CompanyName = "Demo Aerospace Engineering Ltd.",
                Citizenship = "Australian",
                Nationality = "Australian",
                Country = "Australia",
                Designation = "Senior Technical Consultant",
                Email = "adam.gilchrist.demo@example.com",
                Phone = "+61 400 000 000",
                IdType = "Passport",
                IdLast4 = "4821",
                VisitorType = VisitorType.External,
                CreatedAt = now.AddDays(-30),
                UpdatedAt = now
            };
            db.Visitors.Add(visitor);
            await db.SaveChangesAsync(cancellationToken);
        }

        // 3. SEED PREVIOUS COMPLETED DEMO REQUEST (RRVMS-2026-000000)
        var prevRequestId = StableGuid("demo-request-000000");
        if (!await db.VisitorRequests.AnyAsync(r => r.Id == prevRequestId || r.RequestNumber == "RRVMS-2026-000000", cancellationToken))
        {
            var prevRequest = new VisitorRequest
            {
                Id = prevRequestId,
                RequestNumber = "RRVMS-2026-000000",
                Status = RequestStatus.VISIT_PROCESS_COMPLETED,
                VisitorId = visitor.Id,
                RequesterId = hostUser.Id,
                MainHostId = hostUser.Id,
                EscortingHostId = escortUser.Id,
                VisitorType = VisitorType.External,
                VisitingCompany = "Demo Aerospace Engineering Ltd.",
                VisitingSite = "Rolls-Royce Demo Facility",
                VisitPurposeType = "Technical",
                Purpose = "Initial technical consultation on engine design specifications",
                AreasToVisit = "Engine Research Area",
                SiteTimezone = "Europe/London",
                NumberOfVisitors = 1,
                CreatedAt = now.AddDays(-20),
                UpdatedAt = now.AddDays(-14),
                SubmittedAt = now.AddDays(-19),
                ApprovedAt = now.AddDays(-15)
            };
            db.VisitorRequests.Add(prevRequest);

            var prevVisitDay = new VisitDay
            {
                Id = StableGuid("demo-visitday-000000"),
                VisitorRequestId = prevRequestId,
                VisitDate = today.AddDays(-14),
                ExpectedArrivalTime = new TimeOnly(9, 0),
                ExpectedDepartureTime = new TimeOnly(17, 0),
                Status = VisitDayStatus.COMPLETED,
                ActualArrivalTime = now.AddDays(-14).AddHours(-8),
                ActualDepartureTime = now.AddDays(-14).AddHours(-1),
                CreatedAt = now.AddDays(-20),
                UpdatedAt = now.AddDays(-14)
            };
            db.VisitDays.Add(prevVisitDay);

            var prevEcReview = new ECReview
            {
                Id = StableGuid("demo-ecreview-000000"),
                VisitorRequestId = prevRequestId,
                ReviewerId = ecUser.Id,
                Status = EcReviewStatus.Approved,
                Decision = EcDecision.Approve,
                Comments = "Approved previous technical visit.",
                ReviewedAt = now.AddDays(-15),
                CreatedAt = now.AddDays(-15)
            };
            db.ECReviews.Add(prevEcReview);

            var prevDps = new DPSRecord
            {
                Id = StableGuid("demo-dps-000000"),
                VisitorRequestId = prevRequestId,
                PerformedByUserId = ecUser.Id,
                PerformedByType = DpsPerformedByType.EXPORT_CONTROL,
                Status = DpsStatus.Completed,
                Result = DpsResult.Clear,
                Notes = "Screening clear. DEMO DATA.",
                PerformedAt = now.AddDays(-16)
            };
            db.DPSRecords.Add(prevDps);
        }

        // 4. SEED CURRENT ACTIVE DEMO REQUEST (RRVMS-2026-000001)
        var currentRequestId = StableGuid("demo-request-000001");
        var currentRequest = new VisitorRequest
        {
            Id = currentRequestId,
            RequestNumber = "RRVMS-2026-000001",
            Status = RequestStatus.EC_REVIEW,
            VisitorId = visitor.Id,
            RequesterId = hostUser.Id,
            MainHostId = hostUser.Id,
            EscortingHostId = escortUser.Id,
            VisitorType = VisitorType.External,
            VisitingCompany = "Demo Aerospace Engineering Ltd.",
            VisitingSite = "Rolls-Royce Demo Facility",
            VisitPurposeType = "Technical",
            Purpose = "Advanced propulsion systems design review & technical consultation",
            AreasToVisit = "Engine Research Area",
            SiteTimezone = "Europe/London",
            NumberOfVisitors = 1,
            CreatedAt = now.AddHours(-6),
            UpdatedAt = now,
            SubmittedAt = now.AddHours(-5),
            DpsPerformedBy = DpsPerformedByType.EXPORT_CONTROL
        };
        db.VisitorRequests.Add(currentRequest);

        // Assets
        var asset1 = new Asset
        {
            Id = StableGuid("demo-asset-lap-001"),
            VisitorRequestId = currentRequestId,
            VisitorId = visitor.Id,
            AssetType = "Laptop",
            Description = "Dell Precision Workstation",
            SerialNumber = "DEMO-LAP-001",
            IsDeclared = true,
            IsVerified = false,
            VerificationStatus = AssetVerificationStatus.NotVerified,
            CreatedAt = now.AddHours(-6),
            UpdatedAt = now.AddHours(-6)
        };

        var asset2 = new Asset
        {
            Id = StableGuid("demo-asset-drv-001"),
            VisitorRequestId = currentRequestId,
            VisitorId = visitor.Id,
            AssetType = "External Drive",
            Description = "Encrypted SSD 1TB",
            SerialNumber = "DEMO-DRV-001",
            IsDeclared = true,
            IsVerified = false,
            VerificationStatus = AssetVerificationStatus.NotVerified,
            CreatedAt = now.AddHours(-6),
            UpdatedAt = now.AddHours(-6)
        };

        db.Assets.AddRange(asset1, asset2);

        // Visit Day (Today / Development-friendly current date)
        var visitDay = new VisitDay
        {
            Id = StableGuid("demo-visitday-000001"),
            VisitorRequestId = currentRequestId,
            VisitDate = today,
            ExpectedArrivalTime = new TimeOnly(9, 30),
            ExpectedDepartureTime = new TimeOnly(16, 30),
            Status = VisitDayStatus.UPCOMING,
            CreatedAt = now.AddHours(-6),
            UpdatedAt = now
        };
        db.VisitDays.Add(visitDay);

        // Visitor Form
        var visitorFormId = StableGuid("demo-form-000001");
        var visitorForm = new VisitorForm
        {
            Id = visitorFormId,
            VisitorRequestId = currentRequestId,
            VisitorId = visitor.Id,
            FullName = "Adam Gilchrist",
            Citizenship = "Australian",
            Nationality = "Australian",
            Country = "Australia",
            Designation = "Senior Technical Consultant",
            CompanyName = "Demo Aerospace Engineering Ltd.",
            OfficeCity = "Sydney",
            OfficeCountry = "Australia",
            Telephone = "+61 400 000 000",
            Email = "adam.gilchrist.demo@example.com",
            IdType = "Passport",
            IdLast4 = "4821",
            DeclaredAssets = "Laptop (DEMO-LAP-001), External Drive (DEMO-DRV-001)",
            Status = "SUBMITTED",
            SubmittedAt = now.AddHours(-5),
            CreatedAt = now.AddHours(-6),
            UpdatedAt = now.AddHours(-5)
        };
        db.VisitorForms.Add(visitorForm);
        currentRequest.VisitorFormId = visitorFormId;

        // Visitor Form Version 1
        var formVersion1 = new VisitorFormVersion
        {
            Id = StableGuid("demo-version-1"),
            VisitorRequestId = currentRequestId,
            VisitorFormId = visitorFormId,
            Version = 1,
            FullNameSnapshot = "Adam Gilchrist",
            CitizenshipSnapshot = "Australian",
            NationalitySnapshot = "Australian",
            CountrySnapshot = "Australia",
            CompanySnapshot = "Demo Aerospace Engineering Ltd.",
            OfficeCitySnapshot = "Sydney",
            OfficeCountrySnapshot = "Australia",
            DesignationSnapshot = "Senior Technical Consultant",
            PhoneSnapshot = "+61 400 000 000",
            EmailSnapshot = "adam.gilchrist.demo@example.com",
            IdTypeSnapshot = "Passport",
            IdLast4Snapshot = "4821",
            AssetsSnapshot = "[{\"AssetType\":\"Laptop\",\"Description\":\"Dell Precision Workstation\",\"SerialNumber\":\"DEMO-LAP-001\"},{\"AssetType\":\"External Drive\",\"Description\":\"Encrypted SSD 1TB\",\"SerialNumber\":\"DEMO-DRV-001\"}]",
            CreatedAt = now.AddHours(-5)
        };
        db.VisitorFormVersions.Add(formVersion1);

        // DPS Record
        var dpsRecord = new DPSRecord
        {
            Id = StableGuid("demo-dps-000001"),
            VisitorRequestId = currentRequestId,
            PerformedByUserId = ecUser.Id,
            PerformedByType = DpsPerformedByType.EXPORT_CONTROL,
            Status = DpsStatus.InProgress,
            Result = DpsResult.Flagged,
            Notes = "Demo screening result requiring EC review. DEMO DATA.",
            PerformedAt = now.AddHours(-2)
        };
        db.DPSRecords.Add(dpsRecord);
        currentRequest.DpsRecordId = dpsRecord.Id;

        // EC Review
        var ecReview = new ECReview
        {
            Id = StableGuid("demo-ecreview-000001"),
            VisitorRequestId = currentRequestId,
            ReviewerId = ecUser.Id,
            Status = EcReviewStatus.Pending,
            Comments = "Pending Export Control decision",
            CreatedAt = now.AddHours(-2)
        };
        db.ECReviews.Add(ecReview);

        // Comment
        var comment = new Comment
        {
            Id = StableGuid("demo-comment-000001"),
            VisitorRequestId = currentRequestId,
            AuthorUserId = ecUser.Id,
            CommentType = CommentType.EC_REQUEST,
            CommentText = "Demo case: DPS result requires EC review. Additional information may be requested before approval.",
            CreatedAt = now.AddHours(-2)
        };
        db.Comments.Add(comment);

        // Information Request History (#1 Resolved sample)
        var infoRequest = new AdditionalInformationRequest
        {
            Id = StableGuid("demo-inforeq-000001"),
            VisitorRequestId = currentRequestId,
            RequestedByUserId = ecUser.Id,
            VisitorFormId = visitorFormId,
            RequestedFields = "Full Legal Name",
            RequestComment = "Please provide the visitor's full legal name.",
            Status = "RESOLVED",
            ResponseSummary = "Adam Gilchrist",
            RespondedAt = now.AddHours(-3),
            CreatedAt = now.AddHours(-4),
            UpdatedAt = now.AddHours(-3)
        };
        db.AdditionalInformationRequests.Add(infoRequest);

        // Audit Logs
        db.AuditLogs.Add(new AuditLog
        {
            Id = StableGuid("demo-audit-001"),
            Action = "REQUEST_CREATED",
            EntityType = nameof(VisitorRequest),
            EntityId = currentRequestId,
            PerformedByUserId = hostUser.Id,
            Details = "Created demo request RRVMS-2026-000001",
            CreatedAt = now.AddHours(-6)
        });

        db.AuditLogs.Add(new AuditLog
        {
            Id = StableGuid("demo-audit-002"),
            Action = "VISITOR_FORM_SUBMITTED",
            EntityType = nameof(VisitorRequest),
            EntityId = currentRequestId,
            PerformedByUserId = hostUser.Id,
            Details = "Visitor form submitted by Adam Gilchrist",
            CreatedAt = now.AddHours(-5)
        });

        db.AuditLogs.Add(new AuditLog
        {
            Id = StableGuid("demo-audit-003"),
            Action = "DPS_FLAGGED",
            EntityType = nameof(VisitorRequest),
            EntityId = currentRequestId,
            PerformedByUserId = ecUser.Id,
            Details = "DPS Result: FLAGGED. Demo screening result requiring EC review.",
            CreatedAt = now.AddHours(-2)
        });

        // Notifications
        db.Notifications.Add(new Notification
        {
            Id = StableGuid("demo-notification-001"),
            UserId = ecUser.Id,
            Type = "EC_REVIEW_REQUIRED",
            Message = "Request RRVMS-2026-000001 is pending Export Control review.",
            IsRead = false,
            CreatedAt = now.AddHours(-2)
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    private static Guid StableGuid(string value) => new(MD5.HashData(Encoding.UTF8.GetBytes(value)));
}
