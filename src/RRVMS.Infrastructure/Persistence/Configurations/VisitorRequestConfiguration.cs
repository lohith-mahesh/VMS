using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RRVMS.Domain.Entities;

namespace RRVMS.Infrastructure.Persistence.Configurations;

public sealed class VisitorRequestConfiguration : IEntityTypeConfiguration<VisitorRequest>
{
    public void Configure(EntityTypeBuilder<VisitorRequest> builder)
    {
        builder.ToTable("VisitorRequests", table =>
        {
            table.HasCheckConstraint("CK_VisitorRequests_VisitWindow", "[VisitEnd] > [VisitStart]");
        });
        builder.HasKey(request => request.Id);
        builder.Property(request => request.RequestNumber).HasMaxLength(32).IsRequired();
        builder.HasIndex(request => request.RequestNumber).IsUnique();
        builder.Property(request => request.RequesterObjectId).HasMaxLength(64).IsRequired();
        builder.Property(request => request.MainHostObjectId).HasMaxLength(64).IsRequired();
        builder.Property(request => request.MainHostName).HasMaxLength(200).IsRequired();
        builder.Property(request => request.HostDepartment).HasMaxLength(200).IsRequired();
        builder.Property(request => request.EscortingHostObjectId).HasMaxLength(64).IsRequired();
        builder.Property(request => request.EscortingHostName).HasMaxLength(200).IsRequired();
        builder.Property(request => request.SiteCode).HasMaxLength(50).IsRequired();
        builder.Property(request => request.Purpose).HasMaxLength(2000).IsRequired();
        builder.Property(request => request.AreasToVisit).HasMaxLength(1000).IsRequired();
        builder.Property(request => request.CancellationReason).HasMaxLength(2000).IsRequired();
        builder.Property(request => request.VisitorType).HasConversion<string>().HasMaxLength(20);
        builder.Property(request => request.ContractorType).HasConversion<string>().HasMaxLength(40);
        builder.Property(request => request.PurposeType).HasConversion<string>().HasMaxLength(30);
        builder.Property(request => request.Status).HasConversion<string>().HasMaxLength(40);
        builder.Property(request => request.RowVersion).IsRowVersion();
        builder.HasIndex(request => new { request.RequesterObjectId, request.UpdatedAt });
        builder.HasIndex(request => new { request.Status, request.VisitStart });
        builder.HasIndex(request => request.RetainUntil);

        builder.HasMany(request => request.Visitors)
            .WithOne()
            .HasForeignKey(visitor => visitor.RequestId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(request => request.VisitDays)
            .WithOne()
            .HasForeignKey(day => day.RequestId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(request => request.AuditEvents)
            .WithOne()
            .HasForeignKey(audit => audit.RequestId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(request => request.InformationRequests)
            .WithOne()
            .HasForeignKey(item => item.RequestId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(request => request.ScheduleChanges)
            .WithOne()
            .HasForeignKey(change => change.RequestId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(request => request.ScreeningReviews)
            .WithOne()
            .HasForeignKey(review => review.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        SetFieldAccess(builder, nameof(VisitorRequest.Visitors));
        SetFieldAccess(builder, nameof(VisitorRequest.VisitDays));
        SetFieldAccess(builder, nameof(VisitorRequest.AuditEvents));
        SetFieldAccess(builder, nameof(VisitorRequest.InformationRequests));
        SetFieldAccess(builder, nameof(VisitorRequest.ScheduleChanges));
        SetFieldAccess(builder, nameof(VisitorRequest.ScreeningReviews));
    }

    private static void SetFieldAccess(EntityTypeBuilder<VisitorRequest> builder, string navigation) =>
        builder.Navigation(navigation).UsePropertyAccessMode(PropertyAccessMode.Field);
}
