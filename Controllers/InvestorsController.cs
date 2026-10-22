using InvestCase.API.Data;
using InvestCase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvestCase.API.Controllers;

/// <summary>
/// Provides endpoints for managing investors.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class InvestorsController : ControllerBase
{
    private readonly AppDbContext _context;

    public InvestorsController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>Retrieves all investors.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Investor>>> GetAll()
    {
        var investors = await _context.Investors
            .AsNoTracking()
            .Include(investor => investor.User)
            .ToListAsync();

        return Ok(investors);
    }

    /// <summary>Retrieves an investor by its unique identifier.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Investor>> GetById(Guid id)
    {
        var investor = await _context.Investors
            .AsNoTracking()
            .Include(investor => investor.User)
            .Include(investor => investor.Startups)
            .FirstOrDefaultAsync(investor => investor.Id == id);

        if (investor is null)
            return NotFound(new { message = "Investor not found." });

        return Ok(investor);
    }

    /// <summary>Creates a new investor profile.</summary>
    [HttpPost]
    public async Task<ActionResult<Investor>> Create(Investor investor)
    {
        var userExists = await _context.Users
            .AnyAsync(user => user.Id == investor.UserId);

        if (!userExists)
            return BadRequest(new { message = "The informed UserId does not exist." });

        var profileAlreadyExists = await _context.Investors
            .AnyAsync(existingInvestor => existingInvestor.UserId == investor.UserId);

        if (profileAlreadyExists)
            return Conflict(new { message = "This user already has an investor profile." });

        investor.Id = Guid.NewGuid();

        _context.Investors.Add(investor);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = investor.Id }, investor);
    }

    /// <summary>Updates an existing investor.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, Investor investor)
    {
        var existingInvestor = await _context.Investors.FindAsync(id);

        if (existingInvestor is null)
            return NotFound(new { message = "Investor not found." });

        existingInvestor.Company = investor.Company;
        existingInvestor.JobTitle = investor.JobTitle;
        existingInvestor.InvestmentType = investor.InvestmentType;
        existingInvestor.LinkedInUrl = investor.LinkedInUrl;
        existingInvestor.Description = investor.Description;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>Deletes an investor.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var investor = await _context.Investors.FindAsync(id);

        if (investor is null)
            return NotFound(new { message = "Investor not found." });

        _context.Investors.Remove(investor);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
