using InvestCase.API.Data;
using InvestCase.API.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvestCase.API.Controllers;

/// <summary>Provides notifications for the Alerts/Messages screens.</summary>
[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly AppDbContext _context;
    public NotificationsController(AppDbContext context) => _context = context;

    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<IEnumerable<object>>> GetByUser(Guid userId, [FromQuery] string? type)
    {
        var query = _context.Notifications.AsNoTracking().Where(x => x.UserId == userId);
        if (!string.IsNullOrWhiteSpace(type) && !type.Equals("Todas", StringComparison.OrdinalIgnoreCase))
            query = query.Where(x => x.Type == type);

        var items = await query.OrderByDescending(x => x.CreatedAt).Take(50).ToListAsync();
        return Ok(items.Select(x => new
        {
            id = x.Id, type = x.Type, title = x.Title,
            time = PresentationHelper.RelativeTime(x.CreatedAt), unread = !x.IsRead
        }));
    }

    [HttpPatch("user/{userId:guid}/read-all")]
    public async Task<IActionResult> MarkAllAsRead(Guid userId)
    {
        var unread = await _context.Notifications.Where(x => x.UserId == userId && !x.IsRead).ToListAsync();
        foreach (var item in unread) item.IsRead = true;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
