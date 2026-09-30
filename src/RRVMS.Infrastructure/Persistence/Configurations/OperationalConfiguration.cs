using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RRVMS.Domain.Entities;

namespace RRVMS.Infrastructure.Persistence.Configurations;

public sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("AuditEvents");
        builder.HasKey(audit => audit.Id);
        builder.Property(audit => audit.Action).HasMaxLength(100).IsRequired();
        builder.Property(audit => audit.ActorId).HasMaxLength(64).IsRequired();
        builder.Property(audit => audit.ActorRole).HasMaxLength(40).IsRequired();
        builder.Property(audit => audit.BeforeJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(audit => audit.AfterJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(audit => audit.Details).HasMaxLength(4000).IsRequired();
        builder.Property(audit => audit.CorrelationId).HasMaxLength(100).IsRequired();
        builder.HasIndex(audit => new { audit.RequestId, audit.OccurredAt });
    }
}

public sealed class DocumentAccessEventConfiguration : IEntityTypeConfiguration<DocumentAccessEvent>
{
    public void Configure(EntityTypeBuilder<DocumentAccessEvent> builder)
    {
        builder.ToTable("DocumentAccessEvents");
        builder.HasKey(access => access.Id);
        builder.Property(access => access.ActorId).HasMaxLength(64).IsRequired();
        builder.Property(access => access.Action).HasMaxLength(50).IsRequired();
        builder.Property(access => access.CorrelationId).HasMaxLength(100).IsRequired();
        builder.HasIndex(access => new { access.DocumentId, access.OccurredAt });
        builder.HasOne<VisitorRequest>().WithMany().HasForeignKey(access => access.RequestId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.MessageType).HasMaxLength(200).IsRequired();
        builder.Property(message => message.PayloadJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(message => message.LastError).HasMaxLength(4000).IsRequired();
        builder.HasIndex(message => new { message.ProcessedAt, message.OccurredAt });
    }
}

public sealed class ReferenceOptionConfiguration : IEntityTypeConfiguration<ReferenceOption>
{
    public void Configure(EntityTypeBuilder<ReferenceOption> builder)
    {
        builder.ToTable("ReferenceOptions");
        builder.HasKey(option => option.Id);
        builder.Property(option => option.Category).HasMaxLength(100).IsRequired();
        builder.Property(option => option.Code).HasMaxLength(100).IsRequired();
        builder.Property(option => option.DisplayName).HasMaxLength(200).IsRequired();
        builder.HasIndex(option => new { option.Category, option.Code }).IsUnique();
    }
}

public sealed class RequestSequenceConfiguration : IEntityTypeConfiguration<RequestSequence>
{
    public void Configure(EntityTypeBuilder<RequestSequence> builder)
    {
        builder.ToTable("RequestSequences");
        builder.HasKey(sequence => new { sequence.Year, sequence.Month });
    }
}

public sealed class ReportExportEventConfiguration : IEntityTypeConfiguration<ReportExportEvent>
{
    public void Configure(EntityTypeBuilder<ReportExportEvent> builder)
    {
        builder.ToTable("ReportExportEvents");
        builder.HasKey(exportEvent => exportEvent.Id);
        builder.Property(exportEvent => exportEvent.ActorId).HasMaxLength(64).IsRequired();
        builder.Property(exportEvent => exportEvent.ActorRole).HasMaxLength(40).IsRequired();
        builder.Property(exportEvent => exportEvent.Format).HasMaxLength(20).IsRequired();
        builder.Property(exportEvent => exportEvent.FiltersJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(exportEvent => exportEvent.CorrelationId).HasMaxLength(100).IsRequired();
        builder.HasIndex(exportEvent => exportEvent.ExportedAt);
    }
}
