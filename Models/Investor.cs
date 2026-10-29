namespace InvestCase.Models;

public class Investor
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? Company { get; set; }
    public string? JobTitle { get; set; }
    public string? InvestorType { get; set; }
    public string? InterestArea { get; set; }
    public string? InvestmentRange { get; set; }
    public string? InvestmentThesis { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public User User { get; set; } = null!;
}
