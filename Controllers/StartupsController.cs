using InvestCase.API.Contracts;
using InvestCase.API.Data;
using InvestCase.API.Helpers;
using InvestCase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvestCase.API.Controllers;

/// <summary>Provides startup CRUD and search endpoints.</summary>
[ApiController]
[Route("api/[controller]")]
public class StartupsController : ControllerBase
{
    private readonly AppDbContext _context;
    public StartupsController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAll(
        [FromQuery] string? search, [FromQuery] string? industry,
        [FromQuery] string? stage, [FromQuery] string? location,
        [FromQuery] Guid? investorUserId)
    {
        var query = _context.Startups.AsNoTracking().Include(x => x.User).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(term) ||
                (x.Industry != null && x.Industry.ToLower().Contains(term)) ||
                (x.Description != null && x.Description.ToLower().Contains(term)));
        }
        // ILike = case-insensitive LIKE in PostgreSQL ("fintech" also finds "Fintech").
        if (!string.IsNullOrWhiteSpace(industry)) query = query.Where(x => x.Industry != null && EF.Functions.ILike(x.Industry, industry.Trim()));
        if (!string.IsNullOrWhiteSpace(stage)) query = query.Where(x => x.Stage != null && EF.Functions.ILike(x.Stage, stage.Trim()));
        if (!string.IsNullOrWhiteSpace(location)) query = query.Where(x => x.Location != null && EF.Functions.ILike(x.Location, $"%{location.Trim()}%"));

        Investor? investor = null;
        if (investorUserId.HasValue)
            investor = await _context.Investors.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == investorUserId.Value);

        var startups = await query.ToListAsync();
        var result = startups.Select(startup =>
        {
            var score = investor is null ? 75 : PresentationHelper.CalculateMatch(investor, startup);
            return new
            {
                id = startup.Id, userId = startup.UserId, name = startup.Name,
                location = startup.Location ?? "Não informado",
                match = $"{score}% compatível", matchScore = score,
                matchType = PresentationHelper.MatchType(score),
                tags = new[] { startup.Industry ?? "Startup", startup.Stage ?? "Estágio não informado" },
                industry = startup.Industry, stage = startup.Stage,
                investmentNeededRange = startup.InvestmentNeededRange, description = startup.Description
            };
        }).OrderByDescending(x => x.matchScore).ToList();
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Startup>> GetById(Guid id)
    {
        var item = await _context.Startups.AsNoTracking().Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id);
        return item is null ? NotFound(new { message = "Startup not found." }) : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<Startup>> Create(CreateStartupRequest request)
    {
        var user = await _context.Users.FindAsync(request.UserId);
        if (user is null)
            return BadRequest(new { message = "The informed UserId does not exist." });
        if (user.Role != "Startup")
            return BadRequest(new { message = $"This user is registered as {user.Role}, not Startup." });
        if (await _context.Startups.AnyAsync(x => x.UserId == request.UserId))
            return Conflict(new { message = "This user already has a startup profile." });

        var startup = new Startup { Id = Guid.NewGuid(), UserId = request.UserId, ProfileViews30Days = 0 };
        Apply(startup, new StartupRequest(request.Name, request.Description, request.Stage, request.Industry,
            request.InvestmentNeededRange, request.WebsiteUrl, request.Location, request.FoundedAt));
        _context.Startups.Add(startup);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = startup.Id }, startup);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, StartupRequest input)
    {
        var item = await _context.Startups.FindAsync(id);
        if (item is null) return NotFound(new { message = "Startup not found." });
        Apply(item, input);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static void Apply(Startup item, StartupRequest input)
    {
        item.Name = input.Name.Trim(); item.Description = input.Description; item.Stage = input.Stage;
        item.Industry = input.Industry; item.InvestmentNeededRange = input.InvestmentNeededRange;
        item.WebsiteUrl = input.WebsiteUrl; item.Location = input.Location;
        item.FoundedAt = DateTimeHelper.ToUtc(input.FoundedAt);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await _context.Startups.FindAsync(id);
        if (item is null) return NotFound(new { message = "Startup not found." });
        _context.Startups.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
