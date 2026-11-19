using System.Reflection;
using System.Text.Json.Serialization;
using InvestCase.API.Data;
using Microsoft.EntityFrameworkCore;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        const string FrontendCorsPolicy = "Frontend";

        builder.Services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new()
            {
                Title = "InvestCase API",
                Version = "v1",
                Description = "Back-end API for InvestCase."
            });

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
                options.IncludeXmlComments(xmlPath);
        });

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        builder.Services.AddCors(options =>
        {
            options.AddPolicy(FrontendCorsPolicy, policy =>
            {
                policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            // Creates/updates the PostgreSQL tables from the EF Core migrations (Migrations folder).
            using (var scope = app.Services.CreateScope())
            {
                try
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    db.Database.Migrate();
                    // Demo accounts used by the front-end login screens (only when the database is empty).
                    DbSeeder.Seed(db);
                }
                catch (Exception ex)
                {
                    app.Logger.LogError(ex, "Could not apply database migrations or seed data. Check the connection string and whether PostgreSQL is running.");
                }
            }

            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseCors(FrontendCorsPolicy);
        app.MapControllers();
        app.Run();
    }
}
