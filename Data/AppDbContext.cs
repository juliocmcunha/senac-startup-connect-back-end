using InvestCase.Models;
using Microsoft.EntityFrameworkCore;

namespace InvestCase.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Investor> Investors => Set<Investor>();
    public DbSet<Startup> Startups => Set<Startup>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<FundingCall> FundingCalls => Set<FundingCall>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(user => user.Id);

            entity.Property(user => user.FullName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(user => user.Email)
                .HasMaxLength(150)
                .IsRequired();

            entity.HasIndex(user => user.Email)
                .IsUnique();

            entity.Property(user => user.PasswordHash)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(user => user.ProfilePictureUrl)
                .HasMaxLength(500);

            entity.Property(user => user.CreatedAt)
                .IsRequired();

            entity.Property(user => user.IsActive)
                .IsRequired();
        });

        modelBuilder.Entity<Investor>(entity =>
        {
            entity.ToTable("Investors");
            entity.HasKey(investor => investor.Id);

            entity.Property(investor => investor.Company).HasMaxLength(100);
            entity.Property(investor => investor.JobTitle).HasMaxLength(100);
            entity.Property(investor => investor.InvestmentType).HasMaxLength(100);
            entity.Property(investor => investor.LinkedInUrl).HasMaxLength(500);

            entity.HasOne(investor => investor.User)
                .WithOne(user => user.Investor)
                .HasForeignKey<Investor>(investor => investor.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(investor => investor.UserId)
                .IsUnique();
        });

        modelBuilder.Entity<Startup>(entity =>
        {
            entity.ToTable("Startups");
            entity.HasKey(startup => startup.Id);

            entity.Property(startup => startup.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(startup => startup.Stage).HasMaxLength(50);
            entity.Property(startup => startup.Industry).HasMaxLength(100);
            entity.Property(startup => startup.WebsiteUrl).HasMaxLength(500);
            entity.Property(startup => startup.Location).HasMaxLength(150);

            entity.HasOne(startup => startup.User)
                .WithOne(user => user.Startup)
                .HasForeignKey<Startup>(startup => startup.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(startup => startup.UserId)
                .IsUnique();
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("Events");
            entity.HasKey(eventItem => eventItem.Id);

            entity.Property(eventItem => eventItem.Title)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(eventItem => eventItem.Location).HasMaxLength(150);
            entity.Property(eventItem => eventItem.Type).HasMaxLength(100);
            entity.Property(eventItem => eventItem.RegistrationUrl).HasMaxLength(500);

            entity.HasOne(eventItem => eventItem.CreatedByUser)
                .WithMany(user => user.CreatedEvents)
                .HasForeignKey(eventItem => eventItem.CreatedByUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FundingCall>(entity =>
        {
            entity.ToTable("FundingCalls");
            entity.HasKey(fundingCall => fundingCall.Id);

            entity.Property(fundingCall => fundingCall.Title)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(fundingCall => fundingCall.Industry).HasMaxLength(100);
            entity.Property(fundingCall => fundingCall.TotalAmount).HasPrecision(15, 2);
            entity.Property(fundingCall => fundingCall.CallUrl).HasMaxLength(500);

            entity.HasOne(fundingCall => fundingCall.CreatedByUser)
                .WithMany(user => user.CreatedFundingCalls)
                .HasForeignKey(fundingCall => fundingCall.CreatedByUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Investor>()
            .HasMany(investor => investor.Startups)
            .WithMany(startup => startup.Investors)
            .UsingEntity<Dictionary<string, object>>(
                "InvestorStartups",
                right => right
                    .HasOne<Startup>()
                    .WithMany()
                    .HasForeignKey("StartupId")
                    .OnDelete(DeleteBehavior.Cascade),
                left => left
                    .HasOne<Investor>()
                    .WithMany()
                    .HasForeignKey("InvestorId")
                    .OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.ToTable("InvestorStartups");
                    join.HasKey("InvestorId", "StartupId");
                });
    }
}
