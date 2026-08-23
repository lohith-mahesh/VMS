using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RRVMS.Api.Data;
using RRVMS.Api.Services;

namespace RRVMS.Api.Controllers;

[ApiController]
[Route("api/notifications")]
public sealed class NotificationsController(RrvmsDbContext dbContext, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated) return Unauthorized();
        var userId = StableGuid(currentUser.UserId);
        return Ok(await dbContext.Notifications.AsNoTracking().Where(notification => notification.UserId == userId).OrderByDescending(notification => notification.CreatedAt).Select(notification => new { notification.Id, notification.Type, notification.Message, notification.IsRead, notification.CreatedAt }).ToListAsync(cancellationToken));
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated) return Unauthorized();
        var notification = await dbContext.Notifications.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (notification is null) return NotFound();
        notification.IsRead = true;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static Guid StableGuid(string value) => new(MD5.HashData(Encoding.UTF8.GetBytes(value)));
}
