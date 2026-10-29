namespace InvestCase.Models;

public class Interest
{
    public Guid Id { get; set; }
    public Guid FromUserId { get; set; }
    public Guid ToUserId { get; set; }
    public string Status { get; set; } = "Interest Sent";
    public DateTime CreatedAt { get; set; }
    public User FromUser { get; set; } = null!;
    public User ToUser { get; set; } = null!;
}
