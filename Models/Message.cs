namespace InvestCase.Models;

/// <summary>A chat message between two users that have a match.</summary>
public class Message
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public Guid ReceiverId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public User Sender { get; set; } = null!;
    public User Receiver { get; set; } = null!;
}
