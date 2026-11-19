using InvestCase.API.Contracts;
using InvestCase.API.Data;
using InvestCase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvestCase.API.Controllers;

/// <summary>Chat between an investor and a startup. Only users with a match can talk.</summary>
[ApiController]
[Route("api/[controller]")]
public class MessagesController : ControllerBase
{
    private readonly AppDbContext _context;
    public MessagesController(AppDbContext context) => _context = context;

    /// <summary>Returns the conversation between two users, oldest message first.</summary>
    [HttpGet("{userId:guid}/with/{otherUserId:guid}")]
    public async Task<ActionResult<IEnumerable<object>>> GetConversation(Guid userId, Guid otherUserId)
    {
        if (!await HaveMatch(userId, otherUserId))
            return StatusCode(403, new { message = "You can only chat with users you have a match with." });

        var messages = await _context.Messages.AsNoTracking()
            .Where(x => (x.SenderId == userId && x.ReceiverId == otherUserId) ||
                        (x.SenderId == otherUserId && x.ReceiverId == userId))
            .OrderBy(x => x.SentAt)
            .Select(x => new { x.Id, x.SenderId, x.ReceiverId, x.Content, x.SentAt, x.ReadAt, isMine = x.SenderId == userId })
            .ToListAsync();

        return Ok(messages);
    }

    /// <summary>Sends a message. Both users must exist and have a match.</summary>
    [HttpPost]
    public async Task<ActionResult<object>> Send(SendMessageRequest request)
    {
        if (request.SenderId == request.ReceiverId)
            return BadRequest(new { message = "A user cannot send a message to itself." });

        var content = request.Content.Trim();
        if (content.Length == 0)
            return BadRequest(new { message = "The message cannot be empty." });

        var sender = await _context.Users.FindAsync(request.SenderId);
        var receiver = await _context.Users.FindAsync(request.ReceiverId);
        if (sender is null || receiver is null)
            return BadRequest(new { message = "One of the informed users does not exist." });

        if (!await HaveMatch(request.SenderId, request.ReceiverId))
            return StatusCode(403, new { message = "You can only chat with users you have a match with." });

        var message = new Message
        {
            Id = Guid.NewGuid(), SenderId = request.SenderId, ReceiverId = request.ReceiverId,
            Content = content, SentAt = DateTime.UtcNow
        };
        _context.Messages.Add(message);

        // One unread notification per conversation is enough: avoid one alert per message.
        var title = $"Nova mensagem de {sender.FullName}";
        var alreadyNotified = await _context.Notifications.AnyAsync(x =>
            x.UserId == request.ReceiverId && x.Type == "Mensagens" && !x.IsRead && x.Title == title);
        if (!alreadyNotified)
        {
            _context.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(), UserId = request.ReceiverId, Type = "Mensagens",
                Title = title, CreatedAt = DateTime.UtcNow, IsRead = false
            });
        }

        await _context.SaveChangesAsync();
        return Created($"/api/Messages/{message.SenderId}/with/{message.ReceiverId}", new
        {
            message.Id, message.SenderId, message.ReceiverId, message.Content, message.SentAt, message.ReadAt, isMine = true
        });
    }

    /// <summary>Marks as read every message that <paramref name="otherUserId"/> sent to <paramref name="userId"/>.</summary>
    [HttpPatch("{userId:guid}/with/{otherUserId:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid userId, Guid otherUserId)
    {
        var unread = await _context.Messages
            .Where(x => x.SenderId == otherUserId && x.ReceiverId == userId && x.ReadAt == null)
            .ToListAsync();
        var now = DateTime.UtcNow;
        foreach (var item in unread) item.ReadAt = now;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // A match is stored as an Interest with Status "Match" (in either direction).
    private Task<bool> HaveMatch(Guid userA, Guid userB) =>
        _context.Interests.AnyAsync(x => x.Status == "Match" &&
            ((x.FromUserId == userA && x.ToUserId == userB) || (x.FromUserId == userB && x.ToUserId == userA)));
}
