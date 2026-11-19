using System.ComponentModel.DataAnnotations;

namespace InvestCase.API.Contracts;

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password,
    string? ExpectedRole);

public record InvestorRegisterRequest(
    [Required, StringLength(120, MinimumLength = 2)] string FullName,
    [Required, EmailAddress, StringLength(180)] string Email,
    [Required, StringLength(100, MinimumLength = 6)] string Password,
    [Required, StringLength(100)] string InvestorType,
    [Required, StringLength(120)] string InterestArea,
    [Required, StringLength(120)] string InvestmentRange,
    [Required] string InvestmentThesis,
    [StringLength(150)] string? Location);

public record StartupRegisterRequest(
    [Required, StringLength(120, MinimumLength = 2)] string StartupName,
    [Required, EmailAddress, StringLength(180)] string Email,
    [Required, StringLength(100, MinimumLength = 6)] string Password,
    [Required, StringLength(120)] string Industry,
    [Required, StringLength(80)] string Stage,
    [Required, StringLength(120)] string InvestmentNeededRange,
    [Required] string Description,
    [StringLength(150)] string? Location);

public record AuthResponse(
    Guid UserId,
    Guid ProfileId,
    string Role,
    string DisplayName,
    string Email);
