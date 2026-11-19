using System.ComponentModel.DataAnnotations;

namespace InvestCase.API.Contracts;

public record CreateEventRequest(
    [Required, StringLength(180, MinimumLength = 3)] string Title,
    string? Description,
    [Required] DateTime StartsAt,
    DateTime? EndsAt,
    [StringLength(150)] string? Location,
    [StringLength(100)] string? Type,
    [StringLength(500)] string? RegistrationUrl,
    [Required] Guid CreatedByUserId);

public record UpdateEventRequest(
    [Required, StringLength(180, MinimumLength = 3)] string Title,
    string? Description,
    [Required] DateTime StartsAt,
    DateTime? EndsAt,
    [StringLength(150)] string? Location,
    [StringLength(100)] string? Type,
    [StringLength(500)] string? RegistrationUrl);

public record CreateFundingCallRequest(
    [Required, StringLength(180, MinimumLength = 3)] string Title,
    string? Description,
    [StringLength(120)] string? Industry,
    [Range(0, 9999999999999.99)] decimal? TotalAmount,
    DateTime? PublishedAt,
    DateTime? DeadlineAt,
    [StringLength(500)] string? CallUrl,
    [Required] Guid CreatedByUserId);

public record UpdateFundingCallRequest(
    [Required, StringLength(180, MinimumLength = 3)] string Title,
    string? Description,
    [StringLength(120)] string? Industry,
    [Range(0, 9999999999999.99)] decimal? TotalAmount,
    DateTime? PublishedAt,
    DateTime? DeadlineAt,
    [StringLength(500)] string? CallUrl);
