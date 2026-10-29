namespace InvestCase.API.Contracts;

public record LoginRequest(string Email, string Password, string? ExpectedRole);

public record InvestorRegisterRequest(
    string FullName,
    string Email,
    string Password,
    string InvestorType,
    string InterestArea,
    string InvestmentRange,
    string InvestmentThesis,
    string? Location);

public record StartupRegisterRequest(
    string StartupName,
    string Email,
    string Password,
    string Industry,
    string Stage,
    string InvestmentNeededRange,
    string Description,
    string? Location);

public record AuthResponse(
    Guid UserId,
    Guid ProfileId,
    string Role,
    string DisplayName,
    string Email);
