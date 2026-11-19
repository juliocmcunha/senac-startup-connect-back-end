using InvestCase.API.Contracts;
using InvestCase.API.Data;
using InvestCase.API.Helpers;
using InvestCase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvestCase.API.Controllers;

/// <summary>Provides investor CRUD and search endpoints.</summary>
[ApiController]
[Route("api/[controller]")]
public class InvestorsController : ControllerBase
{
    private readonly AppDbContext _context;
    public InvestorsController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAll(
        [FromQuery] string? search, [FromQuery] string? type,
        [FromQuery] string? industry, [FromQuery] string? location,
        [FromQuery] Guid? startupUserId)
    {
        var query = _context.Investors.AsNoTracking().Include(x => x.User).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x => x.User.FullName.ToLower().Contains(term) ||
                (x.Company != null && x.Company.ToLower().Contains(term)) ||
                (x.InterestArea != null && x.InterestArea.ToLower().Contains(term)));
        }
        // ILike = case-insensitive LIKE in PostgreSQL ("fintech" also finds "Fintech").
        if (!string.IsNullOrWhiteSpace(type)) query = query.Where(x => x.InvestorType != null && EF.Functions.ILike(x.InvestorType, type.Trim()));
        if (!string.IsNullOrWhiteSpace(industry)) query = query.Where(x => x.InterestArea != null && EF.Functions.ILike(x.InterestArea, $"%{industry.Trim()}%"));
        if (!string.IsNullOrWhiteSpace(location)) query = query.Where(x => x.Location != null && EF.Functions.ILike(x.Location, $"%{location.Trim()}%"));

        Startup? startup = null;
        if (startupUserId.HasValue)
            startup = await _context.Startups.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == startupUserId.Value);

        var investors = await query.ToListAsync();
        var result = investors.Select(investor =>
        {
            var score = startup is null ? 75 : PresentationHelper.CalculateMatch(investor, startup);
            return new
            {
                id = investor.Id, userId = investor.UserId,
                initials = PresentationHelper.Initials(investor.User.FullName),
                name = investor.User.FullName, type = investor.InvestorType ?? "Investidor",
                location = investor.Location ?? "Não informado",
                match = $"{score}% compatível", matchScore = score,
                matchType = PresentationHelper.MatchType(score),
                investmentRange = investor.InvestmentRange ?? "Não informado",
                interestArea = investor.InterestArea, investmentThesis = investor.InvestmentThesis
            };
        }).OrderByDescending(x => x.matchScore).ToList();
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Investor>> GetById(Guid id)
    {
        var item = await _context.Investors.AsNoTracking().Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id);
        return item is null ? NotFound(new { message = "Investor not found." }) : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<Investor>> Create(CreateInvestorRequest request)
    {
        var user = await _context.Users.FindAsync(request.UserId);
        if (user is null)
            return BadRequest(new { message = "The informed UserId does not exist." });
        if (user.Role != "Investor")
            return BadRequest(new { message = $"This user is registered as {user.Role}, not Investor." });
        if (await _context.Investors.AnyAsync(x => x.UserId == request.UserId))
            return Conflict(new { message = "This user already has an investor profile." });

        var investor = new Investor { Id = Guid.NewGuid(), UserId = request.UserId };
        Apply(investor, new InvestorRequest(request.Company, request.JobTitle, request.InvestorType,
            request.InterestArea, request.InvestmentRange, request.InvestmentThesis,
            request.LinkedInUrl, request.Description, request.Location));
        _context.Investors.Add(investor);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = investor.Id }, investor);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, InvestorRequest input)
    {
        var item = await _context.Investors.FindAsync(id);
        if (item is null) return NotFound(new { message = "Investor not found." });
        Apply(item, input);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static void Apply(Investor item, InvestorRequest input)
    {
        item.Company = input.Company; item.JobTitle = input.JobTitle; item.InvestorType = input.InvestorType;
        item.InterestArea = input.InterestArea; item.InvestmentRange = input.InvestmentRange;
        item.InvestmentThesis = input.InvestmentThesis; item.LinkedInUrl = input.LinkedInUrl;
        item.Description = input.Description; item.Location = input.Location;
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await _context.Investors.FindAsync(id);
        if (item is null) return NotFound(new { message = "Investor not found." });
        _context.Investors.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
