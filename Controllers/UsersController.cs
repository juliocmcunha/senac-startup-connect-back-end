using InvestCase.API.Data;
using InvestCase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvestCase.API.Controllers;

/// <summary>Provides basic user CRUD endpoints.</summary>
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;
    public UsersController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAll()
        => Ok(await _context.Users.AsNoTracking().Select(x => new
        {
            x.Id, x.FullName, x.Email, x.Role, x.ProfilePictureUrl, x.Bio,
            x.CreatedAt, x.LastLoginAt, x.IsActive
        }).ToListAsync());

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<object>> GetById(Guid id)
    {
        var user = await _context.Users.AsNoTracking().Where(x => x.Id == id).Select(x => new
        {
            x.Id, x.FullName, x.Email, x.Role, x.ProfilePictureUrl, x.Bio,
            x.CreatedAt, x.LastLoginAt, x.IsActive
        }).FirstOrDefaultAsync();
        return user is null ? NotFound(new { message = "User not found." }) : Ok(user);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, User input)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null) return NotFound(new { message = "User not found." });
        user.FullName = input.FullName;
        user.ProfilePictureUrl = input.ProfilePictureUrl;
        user.Bio = input.Bio;
        user.IsActive = input.IsActive;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null) return NotFound(new { message = "User not found." });
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
