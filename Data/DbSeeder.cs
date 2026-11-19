using InvestCase.API.Helpers;
using InvestCase.Models;

namespace InvestCase.API.Data;

/// <summary>
/// Inserts demonstration data when the database is empty (Development only).
/// The front-end login screens come pre-filled with the two demo accounts below.
/// All demo accounts use the password "Demo@123".
/// </summary>
public static class DbSeeder
{
    public const string DemoPassword = "Demo@123";

    public static void Seed(AppDbContext context)
    {
        if (context.Users.Any()) return;

        var now = DateTime.UtcNow;

        User NewUser(string name, string email, string role) => new()
        {
            Id = Guid.NewGuid(), FullName = name, Email = email, Role = role,
            PasswordHash = PasswordHelper.Hash(DemoPassword), CreatedAt = now, IsActive = true
        };

        // Investors
        var rafael = NewUser("Rafael Albuquerque", "rafael@investcase.demo", "Investor");
        var camila = NewUser("Camila Torres", "camila@investcase.demo", "Investor");
        var bruno = NewUser("Bruno Menezes", "bruno@investcase.demo", "Investor");

        // Startups
        var nordestePay = NewUser("NordestePay", "nordestepay@investcase.demo", "Startup");
        var saudeJa = NewUser("SaúdeJá", "saudeja@investcase.demo", "Startup");
        var agroSense = NewUser("AgroSense", "agrosense@investcase.demo", "Startup");
        var eduMais = NewUser("EduMais", "edumais@investcase.demo", "Startup");

        context.Users.AddRange(rafael, camila, bruno, nordestePay, saudeJa, agroSense, eduMais);

        context.Investors.AddRange(
            new Investor
            {
                Id = Guid.NewGuid(), UserId = rafael.Id, Company = "Recife Angels", JobTitle = "Sócio",
                InvestorType = "Investidor Anjo", InterestArea = "Fintech, SaaS",
                InvestmentRange = "R$ 50.000 - R$ 200.000",
                InvestmentThesis = "Startups early stage do Nordeste com produto digital e receita recorrente.",
                Description = "Ex-fundador de fintech, apoia empreendedores na primeira rodada.",
                Location = "Recife, PE", LinkedInUrl = "https://www.linkedin.com/"
            },
            new Investor
            {
                Id = Guid.NewGuid(), UserId = camila.Id, Company = "Vale Ventures", JobTitle = "Analista de Investimentos",
                InvestorType = "Venture Capital", InterestArea = "Healthtech, Edtech",
                InvestmentRange = "R$ 500 mil a R$ 1,5 mi",
                InvestmentThesis = "Negócios de impacto em saúde e educação com tração comprovada.",
                Location = "São Paulo, SP"
            },
            new Investor
            {
                Id = Guid.NewGuid(), UserId = bruno.Id, Company = "Semente Capital", JobTitle = "Gestor",
                InvestorType = "Fundo Seed", InterestArea = "Agtech, Cleantech",
                InvestmentRange = "R$ 200.000 - R$ 500.000",
                InvestmentThesis = "Tecnologia para o agronegócio e sustentabilidade.",
                Location = "Petrolina, PE"
            });

        context.Startups.AddRange(
            new Startup
            {
                Id = Guid.NewGuid(), UserId = nordestePay.Id, Name = "NordestePay", Industry = "Fintech", Stage = "MVP",
                InvestmentNeededRange = "R$ 50.000 - R$ 200.000", Location = "Recife, PE",
                Description = "Maquininha e conta digital para pequenos comércios do interior.",
                WebsiteUrl = "https://example.com", FoundedAt = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                ProfileViews30Days = 42
            },
            new Startup
            {
                Id = Guid.NewGuid(), UserId = saudeJa.Id, Name = "SaúdeJá", Industry = "Healthtech", Stage = "Tração",
                InvestmentNeededRange = "R$ 500 mil a R$ 1 mi", Location = "Fortaleza, CE",
                Description = "Telemedicina e agendamento para clínicas populares.", ProfileViews30Days = 18
            },
            new Startup
            {
                Id = Guid.NewGuid(), UserId = agroSense.Id, Name = "AgroSense", Industry = "Agtech", Stage = "Ideação",
                InvestmentNeededRange = "R$ 200.000 - R$ 500.000", Location = "Petrolina, PE",
                Description = "Sensores de umidade de baixo custo para irrigação inteligente.", ProfileViews30Days = 7
            },
            new Startup
            {
                Id = Guid.NewGuid(), UserId = eduMais.Id, Name = "EduMais", Industry = "Edtech", Stage = "Série A",
                InvestmentNeededRange = "R$ 1 mi a R$ 3 mi", Location = "São Paulo, SP",
                Description = "Plataforma de reforço escolar com IA para a rede pública.", ProfileViews30Days = 63
            });

        context.Events.AddRange(
            new Event
            {
                Id = Guid.NewGuid(), Title = "Demo Day Porto Digital", Type = "Pitch",
                Description = "Startups apresentam seus negócios para investidores.",
                StartsAt = now.AddDays(10), EndsAt = now.AddDays(10).AddHours(4),
                Location = "Recife, PE", CreatedByUserId = rafael.Id
            },
            new Event
            {
                Id = Guid.NewGuid(), Title = "Meetup Investidores Anjo NE", Type = "Networking",
                StartsAt = now.AddDays(21), Location = "Online", CreatedByUserId = rafael.Id
            });

        context.FundingCalls.Add(new FundingCall
        {
            Id = Guid.NewGuid(), Title = "Edital Centelha PE", Industry = "Todos",
            Description = "Subvenção para transformar ideias em negócios inovadores.",
            TotalAmount = 1_500_000m, PublishedAt = now.AddDays(-5), DeadlineAt = now.AddDays(30),
            CreatedByUserId = camila.Id
        });

        // One interest already sent, so the demo startup has something on its dashboard.
        context.Interests.Add(new Interest
        {
            Id = Guid.NewGuid(), FromUserId = camila.Id, ToUserId = nordestePay.Id,
            Status = "Interest Sent", CreatedAt = now.AddHours(-3)
        });
        context.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(), UserId = nordestePay.Id, Type = "Atividades",
            Title = "Camila Torres demonstrou interesse no seu perfil", CreatedAt = now.AddHours(-3)
        });

        context.SaveChanges();
    }
}
