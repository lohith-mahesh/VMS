using Microsoft.EntityFrameworkCore;
using RRVMS.Domain.Entities;

namespace RRVMS.Infrastructure.Persistence;

public sealed class RrvmsDbContext(DbContextOptions<RrvmsDbContext> options) : DbContext(options)
{
    public DbSet<VisitorRequest> VisitorRequests => Set<VisitorRequest>();
    public DbSet<Visitor> Visitors => Set<Visitor>();
    public DbSet<VisitDay> VisitDays => Set<VisitDay>();
    public DbSet<DeclaredAsset> DeclaredAssets => Set<DeclaredAsset>();
    public DbSet<DpsDocument> DpsDocuments => Set<DpsDocument>();
    public DbSet<ReceptionRecord> ReceptionRecords => Set<ReceptionRecord>();
    public DbSet<VisitorVersion> VisitorVersions => Set<VisitorVersion>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<InformationRequest> InformationRequests => Set<InformationRequest>();
    public DbSet<ScheduleChange> ScheduleChanges => Set<ScheduleChange>();
    public DbSet<ScreeningReview> ScreeningReviews => Set<ScreeningReview>();
    public DbSet<DocumentAccessEvent> DocumentAccessEvents => Set<DocumentAccessEvent>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ReferenceOption> ReferenceOptions => Set<ReferenceOption>();
    public DbSet<ReportExportEvent> ReportExportEvents => Set<ReportExportEvent>();
    public DbSet<RequestSequence> RequestSequences => Set<RequestSequence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RrvmsDbContext).Assembly);
}
