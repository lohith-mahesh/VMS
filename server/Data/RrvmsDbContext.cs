using Microsoft.EntityFrameworkCore;
using RRVMS.Api.Models;

namespace RRVMS.Api.Data;

public sealed class RrvmsDbContext(DbContextOptions<RrvmsDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Visitor> Visitors => Set<Visitor>();
    public DbSet<VisitorRequest> VisitorRequests => Set<VisitorRequest>();
    public DbSet<VisitorForm> VisitorForms => Set<VisitorForm>();
    public DbSet<VisitorFormVersion> VisitorFormVersions => Set<VisitorFormVersion>();
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
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<AdditionalInformationRequest> AdditionalInformationRequests => Set<AdditionalInformationRequest>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // INDICES
        modelBuilder.Entity<User>().HasIndex(user => user.Email).IsUnique();
        modelBuilder.Entity<Visitor>().HasIndex(visitor => visitor.FullName);
        modelBuilder.Entity<Visitor>().HasIndex(visitor => visitor.CompanyName);
        modelBuilder.Entity<Visitor>().HasIndex(visitor => visitor.VisitorRequestId);
        modelBuilder.Entity<VisitorRequest>().HasIndex(request => request.RequestNumber).IsUnique();
        modelBuilder.Entity<VisitorRequest>().HasIndex(request => request.Status);
        modelBuilder.Entity<VisitorRequest>().HasIndex(request => request.MainHostId);
        modelBuilder.Entity<VisitorRequest>().HasIndex(request => request.VisitorId);
        modelBuilder.Entity<VisitorForm>().HasIndex(form => form.VisitorRequestId);
        modelBuilder.Entity<VisitorFormVersion>().HasIndex(version => new { version.VisitorRequestId, version.Version });
        modelBuilder.Entity<VisitDay>().HasIndex(day => day.VisitDate);
        modelBuilder.Entity<VisitDay>().HasIndex(day => day.Status);
        modelBuilder.Entity<ECReview>().HasIndex(review => review.Status);
        modelBuilder.Entity<Comment>().HasIndex(comment => comment.VisitorRequestId);
        modelBuilder.Entity<AdditionalInformationRequest>().HasIndex(req => req.VisitorRequestId);
        modelBuilder.Entity<Notification>().HasIndex(notification => new { notification.UserId, notification.IsRead });
        modelBuilder.Entity<AuditLog>().HasIndex(log => new { log.EntityType, log.EntityId });
        modelBuilder.Entity<AttendanceRecord>().HasIndex(record => new { record.VisitorRequestId, record.Category });

        // RELATIONSHIPS
        modelBuilder.Entity<VisitorRequest>()
            .HasOne(request => request.Visitor)
            .WithMany(visitor => visitor.Requests)
            .HasForeignKey(request => request.VisitorId)
            .OnDelete(DeleteBehavior.Restrict);
            
        modelBuilder.Entity<VisitorRequest>()
            .HasMany(request => request.VisitorForms)
            .WithOne(form => form.VisitorRequest)
            .HasForeignKey(form => form.VisitorRequestId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<VisitorRequest>()
            .HasMany(request => request.VisitDays)
            .WithOne(day => day.VisitorRequest)
            .HasForeignKey(day => day.VisitorRequestId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<VisitorRequest>()
            .HasMany(request => request.Assets)
            .WithOne(asset => asset.VisitorRequest)
            .HasForeignKey(asset => asset.VisitorRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Asset>()
            .HasOne(asset => asset.Visitor)
            .WithMany(visitor => visitor.Assets)
            .HasForeignKey(asset => asset.VisitorId)
            .OnDelete(DeleteBehavior.SetNull);
            
        modelBuilder.Entity<VisitorRequest>()
            .HasMany(request => request.DpsRecords)
            .WithOne(record => record.VisitorRequest)
            .HasForeignKey(record => record.VisitorRequestId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<VisitorRequest>()
            .HasMany(request => request.EcReviews)
            .WithOne(review => review.VisitorRequest)
            .HasForeignKey(review => review.VisitorRequestId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<VisitorRequest>()
            .HasMany(request => request.Documents)
            .WithOne(document => document.VisitorRequest)
            .HasForeignKey(document => document.VisitorRequestId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<VisitorRequest>()
            .HasMany(request => request.Comments)
            .WithOne(comment => comment.VisitorRequest)
            .HasForeignKey(comment => comment.VisitorRequestId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<VisitorRequest>()
            .HasMany(request => request.InformationRequests)
            .WithOne(infoReq => infoReq.VisitorRequest)
            .HasForeignKey(infoReq => infoReq.VisitorRequestId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<VisitorRequest>()
            .HasMany(request => request.AttendanceRecords)
            .WithOne(record => record.VisitorRequest)
            .HasForeignKey(record => record.VisitorRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Comment>()
            .HasOne(comment => comment.Author)
            .WithMany(user => user.Comments)
            .HasForeignKey(comment => comment.AuthorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ECReview>()
            .HasOne(review => review.Reviewer)
            .WithMany(user => user.Reviews)
            .HasForeignKey(review => review.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<VisitorForm>()
            .HasOne(form => form.Visitor)
            .WithMany(visitor => visitor.Forms)
            .HasForeignKey(form => form.VisitorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AdditionalInformationRequest>()
            .HasOne(req => req.RequestedBy)
            .WithMany()
            .HasForeignKey(req => req.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AttendanceRecord>()
            .HasOne(record => record.VisitDay)
            .WithMany()
            .HasForeignKey(record => record.VisitDayId)
            .OnDelete(DeleteBehavior.SetNull);

        // COLUMN CONFIGURATION
        modelBuilder.Entity<AuditLog>().Property(log => log.Details).HasMaxLength(4000);
        modelBuilder.Entity<Comment>().Property(comment => comment.CommentText).HasMaxLength(2000);
        modelBuilder.Entity<AdditionalInformationRequest>().Property(req => req.RequestComment).HasMaxLength(2000);
    }
}
