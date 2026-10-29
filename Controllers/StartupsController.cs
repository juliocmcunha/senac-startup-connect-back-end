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
        if (!string.IsNullOrWhiteSpace(industry)) query = query.Where(x => x.Industry == industry);
        if (!string.IsNullOrWhiteSpace(stage)) query = query.Where(x => x.Stage == stage);
        if (!string.IsNullOrWhiteSpace(location)) query = query.Where(x => x.Location != null && x.Location.Contains(location));

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
    public async Task<ActionResult<Startup>> Create(Startup startup)
    {
        if (!await _context.Users.AnyAsync(x => x.Id == startup.UserId))
            return BadRequest(new { message = "The informed UserId does not exist." });
        startup.Id = Guid.NewGuid();
        _context.Startups.Add(startup);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = startup.Id }, startup);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, Startup input)
    {
        var item = await _context.Startups.FindAsync(id);
        if (item is null) return NotFound(new { message = "Startup not found." });
        item.Name = input.Name; item.Description = input.Description; item.Stage = input.Stage;
        item.Industry = input.Industry; item.InvestmentNeededRange = input.InvestmentNeededRange;
        item.WebsiteUrl = input.WebsiteUrl; item.Location = input.Location; item.FoundedAt = input.FoundedAt;
        await _context.SaveChangesAsync();
        return NoContent();
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
