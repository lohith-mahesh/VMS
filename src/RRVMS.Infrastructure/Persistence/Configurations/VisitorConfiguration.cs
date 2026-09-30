using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RRVMS.Domain.Entities;

namespace RRVMS.Infrastructure.Persistence.Configurations;

public sealed class VisitorConfiguration : IEntityTypeConfiguration<Visitor>
{
    public void Configure(EntityTypeBuilder<Visitor> builder)
    {
        builder.ToTable("Visitors");
        builder.HasKey(visitor => visitor.Id);
        builder.HasIndex(visitor => new { visitor.RequestId, visitor.Sequence }).IsUnique();
        builder.Property(visitor => visitor.DetailsStatus).HasConversion<string>().HasMaxLength(30);
        builder.Property(visitor => visitor.ScreeningDecision).HasConversion<string>().HasMaxLength(30);
        builder.Property(visitor => visitor.Classification).HasConversion<string>().HasMaxLength(30);
        ConfigureText(builder);

        builder.HasMany(visitor => visitor.Assets).WithOne().HasForeignKey(asset => asset.VisitorId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(visitor => visitor.DpsDocuments).WithOne().HasForeignKey(document => document.VisitorId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(visitor => visitor.ReceptionRecords).WithOne().HasForeignKey(record => record.VisitorId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(visitor => visitor.Versions).WithOne().HasForeignKey(version => version.VisitorId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(visitor => visitor.Assets).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(visitor => visitor.DpsDocuments).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(visitor => visitor.ReceptionRecords).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(visitor => visitor.Versions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void ConfigureText(EntityTypeBuilder<Visitor> builder)
    {
        builder.Property(visitor => visitor.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(visitor => visitor.MiddleName).HasMaxLength(100).IsRequired();
        builder.Property(visitor => visitor.LastName).HasMaxLength(100).IsRequired();
        builder.Property(visitor => visitor.FullName).HasMaxLength(320).IsRequired();
        builder.Property(visitor => visitor.Citizenship).HasMaxLength(100).IsRequired();
        builder.Property(visitor => visitor.Designation).HasMaxLength(200).IsRequired();
        builder.Property(visitor => visitor.CompanyName).HasMaxLength(250).IsRequired();
        builder.Property(visitor => visitor.CompanyAddress).HasMaxLength(1000).IsRequired();
        builder.Property(visitor => visitor.OfficeCity).HasMaxLength(120).IsRequired();
        builder.Property(visitor => visitor.OfficeCountry).HasMaxLength(120).IsRequired();
        builder.Property(visitor => visitor.PhoneCountry).HasMaxLength(120).IsRequired();
        builder.Property(visitor => visitor.PhoneDialCode).HasMaxLength(10).IsRequired();
        builder.Property(visitor => visitor.Telephone).HasMaxLength(30).IsRequired();
        builder.Property(visitor => visitor.Email).HasMaxLength(320).IsRequired();
        builder.Property(visitor => visitor.IdType).HasMaxLength(120).IsRequired();
        builder.Property(visitor => visitor.OtherIdType).HasMaxLength(200).IsRequired();
        builder.Property(visitor => visitor.ScreeningReason).HasMaxLength(2000).IsRequired();
    }
}

public sealed class DeclaredAssetConfiguration : IEntityTypeConfiguration<DeclaredAsset>
{
    public void Configure(EntityTypeBuilder<DeclaredAsset> builder)
    {
        builder.ToTable("DeclaredAssets");
        builder.HasKey(asset => asset.Id);
        builder.Property(asset => asset.AssetType).HasMaxLength(100).IsRequired();
        builder.Property(asset => asset.Description).HasMaxLength(500).IsRequired();
        builder.Property(asset => asset.SerialNumber).HasMaxLength(200).IsRequired();
        builder.Property(asset => asset.VerificationStatus).HasConversion<string>().HasMaxLength(30);
        builder.HasIndex(asset => new { asset.VisitorId, asset.SerialNumber }).IsUnique();
    }
}

public sealed class DpsDocumentConfiguration : IEntityTypeConfiguration<DpsDocument>
{
    public void Configure(EntityTypeBuilder<DpsDocument> builder)
    {
        builder.ToTable("DpsDocuments");
        builder.HasKey(document => document.Id);
        builder.Property(document => document.BlobName).HasMaxLength(500).IsRequired();
        builder.Property(document => document.FileName).HasMaxLength(255).IsRequired();
        builder.Property(document => document.Sha256).HasMaxLength(64).IsRequired();
        builder.Property(document => document.UploadedBy).HasMaxLength(64).IsRequired();
        builder.HasIndex(document => document.BlobName).IsUnique();
        builder.HasIndex(document => new { document.VisitorId, document.Version }).IsUnique();
    }
}

public sealed class VisitorVersionConfiguration : IEntityTypeConfiguration<VisitorVersion>
{
    public void Configure(EntityTypeBuilder<VisitorVersion> builder)
    {
        builder.ToTable("VisitorVersions");
        builder.HasKey(version => version.Id);
        builder.Property(version => version.SnapshotJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(version => version.CreatedBy).HasMaxLength(64).IsRequired();
        builder.HasIndex(version => new { version.VisitorId, version.Version }).IsUnique();
    }
}
