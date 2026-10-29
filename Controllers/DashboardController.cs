using InvestCase.API.Data;
using InvestCase.API.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvestCase.API.Controllers;

/// <summary>Aggregates data used by the two home dashboards.</summary>
[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;
    public DashboardController(AppDbContext context) => _context = context;

    [HttpGet("investor/{userId:guid}")]
    public async Task<ActionResult<object>> InvestorHome(Guid userId)
    {
        var investor = await _context.Investors.AsNoTracking().Include(x => x.User)
            .FirstOrDefaultAsync(x => x.UserId == userId);
        if (investor is null) return NotFound(new { message = "Investor profile not found." });

        var startups = await _context.Startups.AsNoTracking().ToListAsync();
        var recommended = startups.Select(startup =>
        {
            var score = PresentationHelper.CalculateMatch(investor, startup);
            return new
            {
                id = startup.Id, userId = startup.UserId, name = startup.Name,
                category = $"{startup.Industry ?? "Startup"} · {startup.Stage ?? "Estágio não informado"}",
                match = $"{score}% match", matchScore = score,
                matchType = PresentationHelper.MatchType(score), industry = startup.Industry
            };
        }).OrderByDescending(x => x.matchScore).Take(12).ToList();

        return Ok(new { investorName = investor.User.FullName, recommendedStartups = recommended });
    }

    [HttpGet("startup/{userId:guid}")]
    public async Task<ActionResult<object>> StartupHome(Guid userId)
    {
        var startup = await _context.Startups.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId);
        if (startup is null) return NotFound(new { message = "Startup profile not found." });

        var incoming = await _context.Interests.AsNoTracking().Where(x => x.ToUserId == userId)
            .OrderByDescending(x => x.CreatedAt).ToListAsync();
        var investorIds = incoming.Select(x => x.FromUserId).Distinct().ToList();
        var investors = await _context.Investors.AsNoTracking().Include(x => x.User)
            .Where(x => investorIds.Contains(x.UserId)).ToListAsync();

        var interestedInvestors = investors.Select(investor =>
        {
            var score = PresentationHelper.CalculateMatch(investor, startup);
            return new
            {
                id = investor.Id, userId = investor.UserId,
                initials = PresentationHelper.Initials(investor.User.FullName),
                name = investor.User.FullName,
                description = $"{investor.InvestorType ?? "Investidor"} · {investor.InvestmentRange ?? "Faixa não informada"}",
                match = $"{score}% match", matchScore = score,
                matchType = PresentationHelper.MatchType(score)
            };
        }).OrderByDescending(x => x.matchScore).Take(5).ToList();

        var events = await _context.Events.AsNoTracking()
            .Where(x => x.StartsAt >= DateTime.UtcNow.AddDays(-1))
            .OrderBy(x => x.StartsAt).Take(3).ToListAsync();
        var fundingCalls = await _context.FundingCalls.AsNoTracking()
            .Where(x => !x.DeadlineAt.HasValue || x.DeadlineAt >= DateTime.UtcNow)
            .OrderBy(x => x.DeadlineAt).Take(3).ToListAsync();

        var recommendations = events.Select(x => new DashboardItem(x.Id, "EVENTO", x.Title, x.StartsAt.ToString("dd/MM/yyyy")))
            .Concat(fundingCalls.Select(x => new DashboardItem(x.Id, "EDITAL", x.Title, (x.DeadlineAt ?? x.PublishedAt).ToString("dd/MM/yyyy"))))
            .Take(3).ToList();

        var completedFields = new[]
        {
            !string.IsNullOrWhiteSpace(startup.Name), !string.IsNullOrWhiteSpace(startup.Description),
            !string.IsNullOrWhiteSpace(startup.Stage), !string.IsNullOrWhiteSpace(startup.Industry),
            !string.IsNullOrWhiteSpace(startup.InvestmentNeededRange), !string.IsNullOrWhiteSpace(startup.Location)
        }.Count(x => x);
        var completion = (int)Math.Round(completedFields / 6d * 100);

        return Ok(new
        {
            startupName = startup.Name,
            metrics = new { profileCompletion = $"{completion}%", views30Days = startup.ProfileViews30Days, interestsReceived = incoming.Count },
            investors = interestedInvestors,
            events = recommendations
        });
    }

    private sealed record DashboardItem(Guid Id, string Type, string Title, string Date);
}
