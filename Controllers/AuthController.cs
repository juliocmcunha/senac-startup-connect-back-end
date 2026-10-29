using InvestCase.API.Contracts;
using InvestCase.API.Data;
using InvestCase.API.Helpers;
using InvestCase.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvestCase.API.Controllers;

/// <summary>Handles the simple authentication flow used by the current front-end.</summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    public AuthController(AppDbContext context) => _context = context;

    /// <summary>Registers a new investor account and investor profile.</summary>
    [HttpPost("register/investor")]
    public async Task<ActionResult<AuthResponse>> RegisterInvestor(InvestorRegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _context.Users.AnyAsync(x => x.Email == email))
            return Conflict(new { message = "A user with this email already exists." });

        var user = new User
        {
            Id = Guid.NewGuid(), FullName = request.FullName.Trim(), Email = email,
            PasswordHash = PasswordHelper.Hash(request.Password), Role = "Investor",
            CreatedAt = DateTime.UtcNow, IsActive = true
        };
        var investor = new Investor
        {
            Id = Guid.NewGuid(), UserId = user.Id, InvestorType = request.InvestorType,
            InterestArea = request.InterestArea, InvestmentRange = request.InvestmentRange,
            InvestmentThesis = request.InvestmentThesis, Description = request.InvestmentThesis,
            Location = request.Location
        };

        _context.Users.Add(user);
        _context.Investors.Add(investor);
        await _context.SaveChangesAsync();
        return Created("", new AuthResponse(user.Id, investor.Id, user.Role, user.FullName, user.Email));
    }

    /// <summary>Registers a new startup account and startup profile.</summary>
    [HttpPost("register/startup")]
    public async Task<ActionResult<AuthResponse>> RegisterStartup(StartupRegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _context.Users.AnyAsync(x => x.Email == email))
            return Conflict(new { message = "A user with this email already exists." });

        var user = new User
        {
            Id = Guid.NewGuid(), FullName = request.StartupName.Trim(), Email = email,
            PasswordHash = PasswordHelper.Hash(request.Password), Role = "Startup",
            CreatedAt = DateTime.UtcNow, IsActive = true
        };
        var startup = new Startup
        {
            Id = Guid.NewGuid(), UserId = user.Id, Name = request.StartupName.Trim(),
            Industry = request.Industry, Stage = request.Stage,
            InvestmentNeededRange = request.InvestmentNeededRange,
            Description = request.Description, Location = request.Location,
            ProfileViews30Days = 0
        };

        _context.Users.Add(user);
        _context.Startups.Add(startup);
        await _context.SaveChangesAsync();
        return Created("", new AuthResponse(user.Id, startup.Id, user.Role, startup.Name, user.Email));
    }

    /// <summary>Validates email/password and returns the logged-in profile information.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.Include(x => x.Investor).Include(x => x.Startup)
            .FirstOrDefaultAsync(x => x.Email == email);

        if (user is null || !user.IsActive || !PasswordHelper.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        if (!string.IsNullOrWhiteSpace(request.ExpectedRole) &&
            !user.Role.Equals(request.ExpectedRole, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = $"This account is registered as {user.Role}." });

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var profileId = user.Role == "Investor" ? user.Investor?.Id : user.Startup?.Id;
        if (profileId is null)
            return StatusCode(500, new { message = "The user profile is incomplete." });

        var displayName = user.Role == "Startup" ? user.Startup?.Name ?? user.FullName : user.FullName;
        return Ok(new AuthResponse(user.Id, profileId.Value, user.Role, displayName, user.Email));
    }
}
