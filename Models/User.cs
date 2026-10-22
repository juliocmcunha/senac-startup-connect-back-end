using System.Text.Json.Serialization;

namespace InvestCase.Models;

public class User
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    [JsonIgnore]
    public string PasswordHash { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public string? Bio { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;

    public Investor? Investor { get; set; }
    public Startup? Startup { get; set; }
    public ICollection<Event> CreatedEvents { get; set; } = new List<Event>();
    public ICollection<FundingCall> CreatedFundingCalls { get; set; } = new List<FundingCall>();
}
