using InvestCase.API.Contracts;
using InvestCase.API.Data;
using InvestCase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvestCase.API.Controllers;

/// <summary>Creates interest records and turns reciprocal interests into a match.</summary>
[ApiController]
[Route("api/[controller]")]
public class InterestsController : ControllerBase
{
    private readonly AppDbContext _context;
    public InterestsController(AppDbContext context) => _context = context;

    [HttpPost]
    public async Task<ActionResult<object>> Create(CreateInterestRequest request)
    {
        if (request.FromUserId == request.ToUserId)
            return BadRequest(new { message = "A user cannot express interest in itself." });

        var users = await _context.Users
            .Where(x => x.Id == request.FromUserId || x.Id == request.ToUserId)
            .ToListAsync();
        var fromUser = users.FirstOrDefault(x => x.Id == request.FromUserId);
        var toUser = users.FirstOrDefault(x => x.Id == request.ToUserId);
        if (fromUser is null || toUser is null)
            return BadRequest(new { message = "One of the informed users does not exist." });

        // InvestCase connects investors and startups: an interest must go between the two roles.
        var roles = new[] { fromUser.Role, toUser.Role };
        if (!roles.Contains("Investor") || !roles.Contains("Startup"))
            return BadRequest(new { message = "Interests can only be sent between an Investor and a Startup." });

        var existing = await _context.Interests.FirstOrDefaultAsync(x =>
            x.FromUserId == request.FromUserId && x.ToUserId == request.ToUserId);
        if (existing is not null)
            return Conflict(new { message = "Interest already registered.", status = existing.Status });

        var reciprocal = await _context.Interests.FirstOrDefaultAsync(x =>
            x.FromUserId == request.ToUserId && x.ToUserId == request.FromUserId);
        var status = reciprocal is null ? "Interest Sent" : "Match";

        var interest = new Interest
        {
            Id = Guid.NewGuid(), FromUserId = request.FromUserId, ToUserId = request.ToUserId,
            Status = status, CreatedAt = DateTime.UtcNow
        };
        if (reciprocal is not null) reciprocal.Status = "Match";
        _context.Interests.Add(interest);

        _context.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(), UserId = request.ToUserId,
            Type = status == "Match" ? "Matches" : "Atividades",
            Title = status == "Match"
                ? $"Novo match com {fromUser.FullName}"
                : $"{fromUser.FullName} demonstrou interesse no seu perfil",
            CreatedAt = DateTime.UtcNow, IsRead = false
        });

        if (status == "Match")
        {
            _context.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(), UserId = request.FromUserId, Type = "Matches",
                Title = $"Novo match com {toUser.FullName}", CreatedAt = DateTime.UtcNow, IsRead = false
            });
        }

        await _context.SaveChangesAsync();
        return Created("", new { interest.Id, interest.FromUserId, interest.ToUserId, interest.Status, interest.CreatedAt });
    }
}
