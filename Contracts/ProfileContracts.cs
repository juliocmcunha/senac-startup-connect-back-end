using System.ComponentModel.DataAnnotations;

namespace InvestCase.API.Contracts;

// DTOs (Data Transfer Objects) used as request bodies instead of the EF entities.
// They expose only the fields the client is allowed to send, so Swagger shows a
// clean schema and the client can no longer send navigation objects (User, etc.)
// that EF Core would try to insert into the database.

public record UpdateUserRequest(
    [Required, StringLength(120, MinimumLength = 2)] string FullName,
    [StringLength(500)] string? ProfilePictureUrl,
    string? Bio,
    bool IsActive = true);

public record InvestorRequest(
    string? Company,
    string? JobTitle,
    [StringLength(100)] string? InvestorType,
    [StringLength(120)] string? InterestArea,
    [StringLength(120)] string? InvestmentRange,
    string? InvestmentThesis,
    [StringLength(500)] string? LinkedInUrl,
    string? Description,
    [StringLength(150)] string? Location);

public record CreateInvestorRequest(
    [Required] Guid UserId,
    string? Company,
    string? JobTitle,
    [StringLength(100)] string? InvestorType,
    [StringLength(120)] string? InterestArea,
    [StringLength(120)] string? InvestmentRange,
    string? InvestmentThesis,
    [StringLength(500)] string? LinkedInUrl,
    string? Description,
    [StringLength(150)] string? Location);

public record StartupRequest(
    [Required, StringLength(120, MinimumLength = 2)] string Name,
    string? Description,
    [StringLength(80)] string? Stage,
    [StringLength(120)] string? Industry,
    [StringLength(120)] string? InvestmentNeededRange,
    [StringLength(500)] string? WebsiteUrl,
    [StringLength(150)] string? Location,
    DateTime? FoundedAt);

public record CreateStartupRequest(
    [Required] Guid UserId,
    [Required, StringLength(120, MinimumLength = 2)] string Name,
    string? Description,
    [StringLength(80)] string? Stage,
    [StringLength(120)] string? Industry,
    [StringLength(120)] string? InvestmentNeededRange,
    [StringLength(500)] string? WebsiteUrl,
    [StringLength(150)] string? Location,
    DateTime? FoundedAt);
