using InvestCase.Models;
using Microsoft.EntityFrameworkCore;

namespace InvestCase.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Investor> Investors => Set<Investor>();
    public DbSet<Startup> Startups => Set<Startup>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<FundingCall> FundingCalls => Set<FundingCall>();
    public DbSet<Interest> Interests => Set<Interest>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FullName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(180).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
            entity.Property(x => x.Role).HasMaxLength(20).IsRequired();
            entity.Property(x => x.ProfilePictureUrl).HasMaxLength(500);
        });

        modelBuilder.Entity<Investor>(entity =>
        {
            entity.ToTable("Investors");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.UserId).IsUnique();
            entity.Property(x => x.InvestorType).HasMaxLength(100);
            entity.Property(x => x.InterestArea).HasMaxLength(120);
            entity.Property(x => x.InvestmentRange).HasMaxLength(120);
            entity.Property(x => x.Location).HasMaxLength(150);
            entity.Property(x => x.LinkedInUrl).HasMaxLength(500);
            entity.HasOne(x => x.User).WithOne(x => x.Investor).HasForeignKey<Investor>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Startup>(entity =>
        {
            entity.ToTable("Startups");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.UserId).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Stage).HasMaxLength(80);
            entity.Property(x => x.Industry).HasMaxLength(120);
            entity.Property(x => x.InvestmentNeededRange).HasMaxLength(120);
            entity.Property(x => x.WebsiteUrl).HasMaxLength(500);
            entity.Property(x => x.Location).HasMaxLength(150);
            entity.HasOne(x => x.User).WithOne(x => x.Startup).HasForeignKey<Startup>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("Events");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(180).IsRequired();
            entity.Property(x => x.Location).HasMaxLength(150);
            entity.Property(x => x.Type).HasMaxLength(100);
            entity.Property(x => x.RegistrationUrl).HasMaxLength(500);
            entity.HasOne(x => x.CreatedByUser).WithMany(x => x.CreatedEvents).HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FundingCall>(entity =>
        {
            entity.ToTable("FundingCalls");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(180).IsRequired();
            entity.Property(x => x.Industry).HasMaxLength(120);
            entity.Property(x => x.TotalAmount).HasPrecision(15, 2);
            entity.Property(x => x.CallUrl).HasMaxLength(500);
            entity.HasOne(x => x.CreatedByUser).WithMany(x => x.CreatedFundingCalls).HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Interest>(entity =>
        {
            entity.ToTable("Interests");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasMaxLength(40).IsRequired();
            entity.HasIndex(x => new { x.FromUserId, x.ToUserId }).IsUnique();
            entity.HasOne(x => x.FromUser).WithMany().HasForeignKey(x => x.FromUserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ToUser).WithMany().HasForeignKey(x => x.ToUserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Type).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(300).IsRequired();
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.ToTable("Messages");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Content).HasMaxLength(2000).IsRequired();
            // Speeds up loading a conversation between two users in chronological order.
            entity.HasIndex(x => new { x.SenderId, x.ReceiverId, x.SentAt });
            entity.HasOne(x => x.Sender).WithMany().HasForeignKey(x => x.SenderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Receiver).WithMany().HasForeignKey(x => x.ReceiverId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
