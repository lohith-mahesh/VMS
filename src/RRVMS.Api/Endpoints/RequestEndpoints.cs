using Microsoft.AspNetCore.Mvc;
using RRVMS.Application.Contracts;
using RRVMS.Application.Services;
using RRVMS.Domain.Enums;

namespace RRVMS.Api.Endpoints;

public static class RequestEndpoints
{
    public static IEndpointRouteBuilder MapRequestEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var requests = endpoints.MapGroup("/api/requests").RequireAuthorization().WithTags("Requests");
        requests.MapGet("/", ListAsync);
        requests.MapPost("/", CreateAsync).RequireAuthorization("HostRequester");
        requests.MapGet("/{requestId:guid}", GetAsync);
        requests.MapPut("/{requestId:guid}", ReviseRequestAsync).RequireAuthorization("HostRequester");
        requests.MapPut("/{requestId:guid}/visitors/{visitorId:guid}", SubmitVisitorAsync).RequireAuthorization("HostRequester");
        requests.MapPost("/{requestId:guid}/visitors/{visitorId:guid}/dps", UploadDpsAsync)
            .RequireAuthorization("HostRequester")
            .DisableAntiforgery();
        requests.MapGet("/{requestId:guid}/visitors/{visitorId:guid}/dps/{documentId:guid}", DownloadDpsAsync).RequireAuthorization("ExportControl");
        requests.MapPost("/{requestId:guid}/submit", SubmitForScreeningAsync).RequireAuthorization("HostRequester");
        requests.MapPost("/{requestId:guid}/review", ReviewAsync).RequireAuthorization("ExportControl");
        requests.MapPost("/{requestId:guid}/corrections", RequestCorrectionAsync).RequireAuthorization("ExportControl");
        requests.MapPost("/{requestId:guid}/reschedule", RescheduleAsync).RequireAuthorization("HostRequester");
        requests.MapPost("/{requestId:guid}/cancel", CancelAsync).RequireAuthorization("HostRequester");
        requests.MapPost("/{requestId:guid}/reception/verify", VerifyEntryAsync).RequireAuthorization("Reception");
        requests.MapPost("/{requestId:guid}/reception/check-in", CheckInAsync).RequireAuthorization("Reception");
        requests.MapPost("/{requestId:guid}/reception/check-out", CheckOutAsync).RequireAuthorization("Reception");
        requests.MapPost("/{requestId:guid}/reception/no-show", MarkNoShowAsync).RequireAuthorization("Reception");
        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        VisitorRequestService service,
        [FromQuery] string? search,
        [FromQuery] VisitorRequestStatus? status,
        [FromQuery] string? siteCode,
        [FromQuery] DateOnly? visitDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default) =>
        Results.Ok(await service.ListAsync(search, status, siteCode, visitDate, page, pageSize, cancellationToken));

    private static async Task<IResult> CreateAsync(CreateVisitorRequestCommand command, VisitorRequestService service, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(command, cancellationToken);
        return Results.Created($"/api/requests/{result.Id}", result);
    }

    private static async Task<IResult> GetAsync(Guid requestId, VisitorRequestService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.GetAsync(requestId, cancellationToken));

    private static async Task<IResult> SubmitVisitorAsync(Guid requestId, Guid visitorId, SubmitVisitorCommand command, VisitorRequestService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.SubmitVisitorAsync(requestId, visitorId, command, cancellationToken));

    private static async Task<IResult> ReviseRequestAsync(Guid requestId, ReviseRequestCommand command, VisitorRequestService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.ReviseRequestAsync(requestId, command, cancellationToken));

    private static async Task<IResult> UploadDpsAsync(
        Guid requestId,
        Guid visitorId,
        IFormFile file,
        [FromForm] string rowVersion,
        VisitorRequestService service,
        CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var result = await service.UploadDpsAsync(requestId, visitorId, file.FileName, file.ContentType, stream, ParseRowVersion(rowVersion), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> DownloadDpsAsync(Guid requestId, Guid visitorId, Guid documentId, VisitorRequestService service, CancellationToken cancellationToken)
    {
        var download = await service.DownloadDpsAsync(requestId, visitorId, documentId, cancellationToken);
        return Results.Stream(download.Content, download.ContentType, download.FileName, enableRangeProcessing: false);
    }

    private static async Task<IResult> SubmitForScreeningAsync(Guid requestId, SubmitForScreeningCommand command, VisitorRequestService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.SubmitForScreeningAsync(requestId, command, cancellationToken));

    private static async Task<IResult> ReviewAsync(Guid requestId, ReviewVisitorsCommand command, VisitorRequestService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.ReviewAsync(requestId, command, cancellationToken));

    private static async Task<IResult> RequestCorrectionAsync(Guid requestId, RequestCorrectionCommand command, VisitorRequestService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.RequestCorrectionAsync(requestId, command, cancellationToken));

    private static async Task<IResult> RescheduleAsync(Guid requestId, RescheduleCommand command, VisitorRequestService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.RescheduleAsync(requestId, command, cancellationToken));

    private static async Task<IResult> CancelAsync(Guid requestId, CancelRequestCommand command, VisitorRequestService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.CancelAsync(requestId, command, cancellationToken));

    private static async Task<IResult> VerifyEntryAsync(Guid requestId, VerifyEntryCommand command, VisitorRequestService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.VerifyEntryAsync(requestId, command, cancellationToken));

    private static async Task<IResult> CheckInAsync(Guid requestId, CheckInCommand command, VisitorRequestService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.CheckInAsync(requestId, command, cancellationToken));

    private static async Task<IResult> CheckOutAsync(Guid requestId, CheckOutCommand command, VisitorRequestService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.CheckOutAsync(requestId, command, cancellationToken));

    private static async Task<IResult> MarkNoShowAsync(Guid requestId, MarkNoShowCommand command, VisitorRequestService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.MarkNoShowAsync(requestId, command, cancellationToken));

    private static byte[] ParseRowVersion(string value)
    {
        try
        {
            return Convert.FromBase64String(value);
        }
        catch (FormatException)
        {
            return [];
        }
    }
}
