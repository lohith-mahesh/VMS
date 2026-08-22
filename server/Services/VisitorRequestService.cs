using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RRVMS.Api.Data;
using RRVMS.Api.DTOs;
using RRVMS.Api.Models;

namespace RRVMS.Api.Services;

public interface IVisitorRequestService
{
    Task<(IReadOnlyList<VisitorRequestListItemDto> Items, int Total)> ListAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<VisitorRequestDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<VisitorRequestDetailDto> CreateAsync(CreateVisitorRequestDto input, string requesterKey, CancellationToken cancellationToken);
}

public sealed class VisitorRequestService(RrvmsDbContext dbContext) : IVisitorRequestService
{
    public async Task<(IReadOnlyList<VisitorRequestListItemDto> Items, int Total)> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = dbContext.VisitorRequests.AsNoTracking().Include(request => request.Visitor).OrderByDescending(request => request.CreatedAt);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(request => new VisitorRequestListItemDto(request.Id, request.RequestNumber, request.Visitor.FullName, request.Visitor.CompanyName, request.CurrentStatus, request.CreatedAt)).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<VisitorRequestDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var request = await dbContext.VisitorRequests.AsNoTracking().Include(item => item.Visitor).Include(item => item.VisitDays).Include(item => item.Assets).Include(item => item.EcReviews).FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (request is null) return null;
        var audit = await dbContext.AuditLogs.AsNoTracking().Where(log => log.EntityType == nameof(VisitorRequest) && log.EntityId == id).OrderByDescending(log => log.CreatedAt).Select(log => new AuditDto(log.Id, log.Action, log.EntityType, log.EntityId, log.Details, log.CreatedAt)).ToListAsync(cancellationToken);
        return ToDetail(request, audit);
    }

    public async Task<VisitorRequestDetailDto> CreateAsync(CreateVisitorRequestDto input, string requesterKey, CancellationToken cancellationToken)
    {
        if (input.VisitDays.Count == 0 || input.VisitDays.Any(day => day.VisitDate < DateOnly.FromDateTime(DateTime.UtcNow))) throw new ArgumentException("At least one future visit date is required.");
        if (input.VisitDays.GroupBy(day => day.VisitDate).Any(group => group.Count() > 1)) throw new ArgumentException("Visit dates must be unique.");

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var visitor = new Visitor { Id = Guid.NewGuid(), FullName = input.FullName.Trim(), CompanyName = input.CompanyName.Trim(), Citizenship = input.Citizenship.Trim(), Country = input.Country.Trim(), Designation = input.Designation.Trim(), Email = input.Email.Trim(), Phone = input.Phone.Trim(), IdType = input.IdType.Trim(), IdLast4 = input.IdLast4, VisitorType = VisitorType.External, CreatedAt = now, UpdatedAt = now };
        var request = new VisitorRequest { Id = Guid.NewGuid(), RequestNumber = await NextRequestNumber(cancellationToken), Visitor = visitor, RequesterId = StableGuid(requesterKey), MainHostId = StableGuid(requesterKey), Purpose = input.Purpose.Trim(), VisitingCompany = input.VisitingCompany.Trim(), VisitingSite = input.VisitingSite.Trim(), VisitPurposeType = input.VisitPurposeType.Trim(), CurrentStatus = "Submitted", CreatedAt = now, UpdatedAt = now, SubmittedAt = now };
        request.VisitDays = input.VisitDays.Select(day => new VisitDay { Id = Guid.NewGuid(), VisitDate = day.VisitDate, ExpectedArrivalTime = day.ExpectedArrivalTime, ExpectedDepartureTime = day.ExpectedDepartureTime, Status = VisitDayStatus.Expected, CreatedAt = now, UpdatedAt = now }).ToList();
        request.Assets = input.Assets.Select(asset => new Asset { Id = Guid.NewGuid(), AssetType = asset.AssetType.Trim(), Description = asset.Description.Trim(), SerialNumber = asset.SerialNumber.Trim(), IsDeclared = true, VerificationStatus = AssetVerificationStatus.NotVerified, CreatedAt = now, UpdatedAt = now }).ToList();
        dbContext.VisitorRequests.Add(request);
        dbContext.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), Action = "REQUEST_CREATED", EntityType = nameof(VisitorRequest), EntityId = request.Id, PerformedByUserId = StableGuid(requesterKey), Details = request.RequestNumber, CreatedAt = now });
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (await GetAsync(request.Id, cancellationToken))!;
    }

    private async Task<string> NextRequestNumber(CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"RRVMS-{year}-";
        var count = await dbContext.VisitorRequests.CountAsync(request => request.RequestNumber.StartsWith(prefix), cancellationToken);
        var candidate = $"{prefix}{count + 1:000000}";
        while (await dbContext.VisitorRequests.AnyAsync(request => request.RequestNumber == candidate, cancellationToken)) candidate = $"{prefix}{++count:000000}";
        return candidate;
    }

    private static Guid StableGuid(string value)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(value));
        return new Guid(bytes);
    }

    private static VisitorRequestDetailDto ToDetail(VisitorRequest request, IReadOnlyList<AuditDto> audit) => new(request.Id, request.RequestNumber, new VisitorDto(request.Visitor.Id, request.Visitor.FullName, request.Visitor.CompanyName, request.Visitor.Citizenship, request.Visitor.Country, request.Visitor.Designation, request.Visitor.Email, request.Visitor.Phone, request.Visitor.IdType, request.Visitor.IdLast4, request.Visitor.VisitorType.ToString()), request.Purpose, request.VisitingCompany, request.VisitingSite, request.VisitPurposeType, request.CurrentStatus, request.VisitDays.Select(day => new VisitDayDto(day.Id, day.VisitDate, day.ExpectedArrivalTime, day.ExpectedDepartureTime, day.Status.ToString(), day.ActualArrivalTime, day.ActualDepartureTime)).ToList(), request.Assets.Select(asset => new AssetDto(asset.Id, asset.AssetType, asset.Description, asset.SerialNumber, asset.IsDeclared, asset.IsVerified, asset.VerificationStatus.ToString())).ToList(), audit);
}
