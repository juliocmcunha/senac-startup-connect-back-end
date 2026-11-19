using InvestCase.API.Data;
using InvestCase.API.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvestCase.API.Controllers;

/// <summary>
/// Returns reciprocal interests that became matches.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MatchesController : ControllerBase
{
    private readonly AppDbContext _context;

    public MatchesController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves all matches for a user.
    /// </summary>
    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<IEnumerable<object>>> GetByUser(Guid userId)
    {
        var matches = await _context.Interests
            .AsNoTracking()
            .Where(x => x.Status == "Match" &&
                        (x.FromUserId == userId || x.ToUserId == userId))
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        var otherUserIds = matches
            .Select(x => x.FromUserId == userId ? x.ToUserId : x.FromUserId)
            .Distinct()
            .ToList();

        var users = await _context.Users
            .AsNoTracking()
            .Where(x => otherUserIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id);

        // Profile ids let the front-end open the investor/startup profile screen.
        var investorProfiles = await _context.Investors.AsNoTracking()
            .Where(x => otherUserIds.Contains(x.UserId))
            .ToDictionaryAsync(x => x.UserId, x => x.Id);
        var startupProfiles = await _context.Startups.AsNoTracking()
            .Where(x => otherUserIds.Contains(x.UserId))
            .ToDictionaryAsync(x => x.UserId, x => x.Id);

        // Chat summary: last message and how many messages this user has not read yet.
        var conversationMessages = await _context.Messages.AsNoTracking()
            .Where(x => (x.SenderId == userId && otherUserIds.Contains(x.ReceiverId)) ||
                        (x.ReceiverId == userId && otherUserIds.Contains(x.SenderId)))
            .ToListAsync();

        var result = matches
            .GroupBy(x => x.FromUserId == userId ? x.ToUserId : x.FromUserId)
            .Select(group =>
            {
                var otherUserId = group.Key;
                users.TryGetValue(otherUserId, out var otherUser);

                return new
                {
                    userId = otherUserId,
                    profileId = investorProfiles.TryGetValue(otherUserId, out var investorProfileId)
                        ? investorProfileId
                        : startupProfiles.TryGetValue(otherUserId, out var startupProfileId) ? startupProfileId : (Guid?)null,
                    displayName = otherUser?.FullName ?? "Unknown user",
                    role = otherUser?.Role,
                    initials = PresentationHelper.Initials(otherUser?.FullName ?? "IC"),
                    matchedAt = group.Max(x => x.CreatedAt),
                    lastMessage = conversationMessages
                        .Where(m => m.SenderId == otherUserId || m.ReceiverId == otherUserId)
                        .OrderByDescending(m => m.SentAt).Select(m => m.Content).FirstOrDefault(),
                    unreadMessages = conversationMessages
                        .Count(m => m.SenderId == otherUserId && m.ReadAt == null)
                };
            })
            .OrderByDescending(x => x.matchedAt)
            .ToList();

        return Ok(result);
    }
}
