using Microsoft.EntityFrameworkCore;
using RRVMS.Api.Models;

namespace RRVMS.Api.Data;

public sealed class RrvmsDbContext(DbContextOptions<RrvmsDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Visitor> Visitors => Set<Visitor>();
    public DbSet<VisitorRequest> VisitorRequests => Set<VisitorRequest>();
    public DbSet<VisitDay> VisitDays => Set<VisitDay>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<DPSRecord> DPSRecords => Set<DPSRecord>();
    public DbSet<ECReview> ECReviews => Set<ECReview>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Badge> Badges => Set<Badge>();
    public DbSet<VisitCheckIn> VisitCheckIns => Set<VisitCheckIn>();
    public DbSet<VisitCheckOut> VisitCheckOuts => Set<VisitCheckOut>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(user => user.Email).IsUnique();
        modelBuilder.Entity<Visitor>().HasIndex(visitor => visitor.FullName);
        modelBuilder.Entity<Visitor>().HasIndex(visitor => visitor.CompanyName);
        modelBuilder.Entity<VisitorRequest>().HasIndex(request => request.RequestNumber).IsUnique();
        modelBuilder.Entity<VisitorRequest>().HasIndex(request => request.CurrentStatus);
        modelBuilder.Entity<VisitorRequest>().HasIndex(request => request.MainHostId);
        modelBuilder.Entity<VisitorRequest>().HasIndex(request => request.VisitorId);
        modelBuilder.Entity<VisitDay>().HasIndex(day => day.VisitDate);
        modelBuilder.Entity<VisitDay>().HasIndex(day => day.Status);
        modelBuilder.Entity<ECReview>().HasIndex(review => review.Status);
        modelBuilder.Entity<Notification>().HasIndex(notification => new { notification.UserId, notification.IsRead });
        modelBuilder.Entity<AuditLog>().HasIndex(log => new { log.EntityType, log.EntityId });

        modelBuilder.Entity<VisitorRequest>().HasOne(request => request.Visitor).WithMany(visitor => visitor.Requests).HasForeignKey(request => request.VisitorId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<VisitorRequest>().HasMany(request => request.VisitDays).WithOne(day => day.VisitorRequest).HasForeignKey(day => day.VisitorRequestId);
        modelBuilder.Entity<VisitorRequest>().HasMany(request => request.Assets).WithOne(asset => asset.VisitorRequest).HasForeignKey(asset => asset.VisitorRequestId);
        modelBuilder.Entity<VisitorRequest>().HasMany(request => request.DpsRecords).WithOne(record => record.VisitorRequest).HasForeignKey(record => record.VisitorRequestId);
        modelBuilder.Entity<VisitorRequest>().HasMany(request => request.EcReviews).WithOne(review => review.VisitorRequest).HasForeignKey(review => review.VisitorRequestId);
        modelBuilder.Entity<VisitorRequest>().HasMany(request => request.Documents).WithOne(document => document.VisitorRequest).HasForeignKey(document => document.VisitorRequestId);
        modelBuilder.Entity<AuditLog>().Property(log => log.Details).HasMaxLength(4000);
    }
}
