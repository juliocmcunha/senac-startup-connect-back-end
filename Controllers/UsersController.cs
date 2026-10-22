using InvestCase.API.Data;
using InvestCase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvestCase.API.Controllers;

/// <summary>
/// Provides endpoints for managing users.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>Retrieves all users.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetAll()
    {
        var users = await _context.Users
            .AsNoTracking()
            .ToListAsync();

        return Ok(users);
    }

    /// <summary>Retrieves a user by its unique identifier.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<User>> GetById(Guid id)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Include(user => user.Investor)
            .Include(user => user.Startup)
            .FirstOrDefaultAsync(user => user.Id == id);

        if (user is null)
            return NotFound(new { message = "User not found." });

        return Ok(user);
    }

    /// <summary>Creates a new user.</summary>
    [HttpPost]
    public async Task<ActionResult<User>> Create(User user)
    {
        var emailAlreadyExists = await _context.Users
            .AnyAsync(existingUser => existingUser.Email == user.Email);

        if (emailAlreadyExists)
            return Conflict(new { message = "A user with this email already exists." });

        user.Id = Guid.NewGuid();
        user.CreatedAt = DateTime.UtcNow;
        user.LastLoginAt = null;

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    /// <summary>Updates an existing user.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, User user)
    {
        var existingUser = await _context.Users.FindAsync(id);

        if (existingUser is null)
            return NotFound(new { message = "User not found." });

        var emailInUse = await _context.Users.AnyAsync(candidate =>
            candidate.Email == user.Email &&
            candidate.Id != id);

        if (emailInUse)
            return Conflict(new { message = "A user with this email already exists." });

        existingUser.FullName = user.FullName;
        existingUser.Email = user.Email;
        existingUser.ProfilePictureUrl = user.ProfilePictureUrl;
        existingUser.Bio = user.Bio;
        existingUser.LastLoginAt = user.LastLoginAt;
        existingUser.IsActive = user.IsActive;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>Deletes a user.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user is null)
            return NotFound(new { message = "User not found." });

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
