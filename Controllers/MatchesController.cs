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

        var result = matches
            .GroupBy(x => x.FromUserId == userId ? x.ToUserId : x.FromUserId)
            .Select(group =>
            {
                var otherUserId = group.Key;
                users.TryGetValue(otherUserId, out var otherUser);

                return new
                {
                    userId = otherUserId,
                    displayName = otherUser?.FullName ?? "Unknown user",
                    role = otherUser?.Role,
                    initials = PresentationHelper.Initials(otherUser?.FullName ?? "IC"),
                    matchedAt = group.Max(x => x.CreatedAt)
                };
            })
            .OrderByDescending(x => x.matchedAt)
            .ToList();

        return Ok(result);
    }
}
