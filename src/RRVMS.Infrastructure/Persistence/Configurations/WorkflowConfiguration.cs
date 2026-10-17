using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RRVMS.Domain.Entities;

namespace RRVMS.Infrastructure.Persistence.Configurations;

public sealed class VisitDayConfiguration : IEntityTypeConfiguration<VisitDay>
{
    public void Configure(EntityTypeBuilder<VisitDay> builder)
    {
        builder.ToTable("VisitDays");
        builder.HasKey(day => day.Id);
        builder.HasIndex(day => new { day.RequestId, day.Date }).IsUnique();
    }
}

public sealed class ReceptionRecordConfiguration : IEntityTypeConfiguration<ReceptionRecord>
{
    public void Configure(EntityTypeBuilder<ReceptionRecord> builder)
    {
        builder.ToTable("ReceptionRecords");
        builder.HasKey(record => record.Id);
        builder.Property(record => record.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(record => record.IdentityStatus).HasConversion<string>().HasMaxLength(30);
        builder.Property(record => record.AssetsStatus).HasConversion<string>().HasMaxLength(30);
        builder.Property(record => record.ArrivalStatus).HasConversion<string>().HasMaxLength(30);
        builder.Property(record => record.BadgeId).HasMaxLength(100).IsRequired();
        builder.Property(record => record.BadgeType).HasMaxLength(100).IsRequired();
        builder.Property(record => record.VerificationRemarks).HasMaxLength(2000).IsRequired();
        builder.HasIndex(record => new { record.VisitorId, record.VisitDayId }).IsUnique();
        builder.HasIndex(record => record.BadgeId).HasFilter("[Status] = 'CheckedIn' AND [BadgeId] <> ''").IsUnique();
        builder.HasOne<VisitDay>().WithMany().HasForeignKey(record => record.VisitDayId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class InformationRequestConfiguration : IEntityTypeConfiguration<InformationRequest>
{
    public void Configure(EntityTypeBuilder<InformationRequest> builder)
    {
        builder.ToTable("InformationRequests");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.FieldsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(item => item.Instructions).HasMaxLength(2000).IsRequired();
        builder.Property(item => item.OriginalValuesJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(item => item.ChangesJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(item => item.CreatedBy).HasMaxLength(64).IsRequired();
        builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(30);
        builder.HasOne<Visitor>().WithMany().HasForeignKey(item => item.VisitorId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class ScreeningReviewConfiguration : IEntityTypeConfiguration<ScreeningReview>
{
    public void Configure(EntityTypeBuilder<ScreeningReview> builder)
    {
        builder.ToTable("ScreeningReviews");
        builder.HasKey(review => review.Id);
        builder.Property(review => review.Decision).HasConversion<string>().HasMaxLength(30);
        builder.Property(review => review.Classification).HasConversion<string>().HasMaxLength(30);
        builder.Property(review => review.Comments).HasMaxLength(2000).IsRequired();
        builder.Property(review => review.ReviewerId).HasMaxLength(64).IsRequired();
        builder.HasOne<Visitor>().WithMany().HasForeignKey(review => review.VisitorId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class ScheduleChangeConfiguration : IEntityTypeConfiguration<ScheduleChange>
{
    public void Configure(EntityTypeBuilder<ScheduleChange> builder)
    {
        builder.ToTable("ScheduleChanges");
        builder.HasKey(change => change.Id);
        builder.Property(change => change.Reason).HasMaxLength(2000).IsRequired();
        builder.Property(change => change.ChangedBy).HasMaxLength(64).IsRequired();
    }
}
