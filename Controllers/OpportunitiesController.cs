using InvestCase.API.Data;
using InvestCase.API.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvestCase.API.Controllers;

/// <summary>Returns the opportunity lists displayed by investor and startup screens.</summary>
[ApiController]
[Route("api/[controller]")]
public class OpportunitiesController : ControllerBase
{
    private readonly AppDbContext _context;
    public OpportunitiesController(AppDbContext context) => _context = context;

    [HttpGet("investor/{userId:guid}")]
    public async Task<ActionResult<IEnumerable<object>>> GetInvestorOpportunities(Guid userId)
    {
        var investor = await _context.Investors.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId);
        if (investor is null) return NotFound(new { message = "Investor profile not found." });

        var interests = await _context.Interests.AsNoTracking()
            .Where(x => x.FromUserId == userId || x.ToUserId == userId)
            .OrderByDescending(x => x.CreatedAt).ToListAsync();
        // A match is stored as two interests (A->B and B->A); keep only the most recent one per counterpart.
        interests = interests.GroupBy(x => x.FromUserId == userId ? x.ToUserId : x.FromUserId)
            .Select(g => g.First()).ToList();
        var otherIds = interests.Select(x => x.FromUserId == userId ? x.ToUserId : x.FromUserId).Distinct().ToList();
        var startups = await _context.Startups.AsNoTracking().Where(x => otherIds.Contains(x.UserId)).ToDictionaryAsync(x => x.UserId);

        var result = interests.Select(interest =>
        {
            var otherUserId = interest.FromUserId == userId ? interest.ToUserId : interest.FromUserId;
            if (!startups.TryGetValue(otherUserId, out var startup)) return null;
            var score = PresentationHelper.CalculateMatch(investor, startup);
            var status = TranslateStatus(interest.Status);
            return new
            {
                id = interest.Id, profileId = startup.Id, userId = startup.UserId, name = startup.Name, location = startup.Location ?? "Não informado",
                match = $"{score}% compatível", matchType = PresentationHelper.MatchType(score),
                tags = new[] { startup.Industry ?? "Startup", startup.Stage ?? "Estágio não informado" },
                status, statusType = StatusType(status)
            };
        }).Where(x => x is not null).ToList();
        return Ok(result);
    }

    [HttpGet("startup/{userId:guid}")]
    public async Task<ActionResult<IEnumerable<object>>> GetStartupOpportunities(Guid userId)
    {
        var startup = await _context.Startups.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId);
        if (startup is null) return NotFound(new { message = "Startup profile not found." });

        var interests = await _context.Interests.AsNoTracking()
            .Where(x => x.FromUserId == userId || x.ToUserId == userId)
            .OrderByDescending(x => x.CreatedAt).ToListAsync();
        // A match is stored as two interests (A->B and B->A); keep only the most recent one per counterpart.
        interests = interests.GroupBy(x => x.FromUserId == userId ? x.ToUserId : x.FromUserId)
            .Select(g => g.First()).ToList();
        var otherIds = interests.Select(x => x.FromUserId == userId ? x.ToUserId : x.FromUserId).Distinct().ToList();
        var investors = await _context.Investors.AsNoTracking().Include(x => x.User)
            .Where(x => otherIds.Contains(x.UserId)).ToDictionaryAsync(x => x.UserId);

        var result = interests.Select(interest =>
        {
            var otherUserId = interest.FromUserId == userId ? interest.ToUserId : interest.FromUserId;
            if (!investors.TryGetValue(otherUserId, out var investor)) return null;
            var score = PresentationHelper.CalculateMatch(investor, startup);
            var status = TranslateStatus(interest.Status);
            return new
            {
                id = interest.Id, profileId = investor.Id, userId = investor.UserId,
                initials = PresentationHelper.Initials(investor.User.FullName),
                name = investor.User.FullName, type = investor.InvestorType ?? "Investidor",
                match = $"{score}% compatível", matchType = PresentationHelper.MatchType(score),
                range = $"Faixa de aporte: {investor.InvestmentRange ?? "Não informado"}",
                status, statusType = StatusType(status)
            };
        }).Where(x => x is not null).ToList();
        return Ok(result);
    }

    private static string TranslateStatus(string status) => status switch
    {
        "Match" => "Match",
        "Interest Sent" => "Interesse Enviado",
        _ => "Sem Resposta"
    };

    private static string StatusType(string status) => status switch
    {
        "Match" => "success",
        "Interesse Enviado" => "neutral",
        _ => "warning"
    };
}
