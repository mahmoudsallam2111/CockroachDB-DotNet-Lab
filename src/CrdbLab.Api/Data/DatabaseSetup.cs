using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CrdbLab.Api.Data;

public static class DatabaseSetup
{
    public static IServiceCollection AddBankDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<BankDbContext>(options =>
            options
                .UseNpgsql(configuration.GetConnectionString("Bank"))
                // 1) no LOCK TABLE   2) each migration statement runs on its own, without a transaction
                .ReplaceService<IHistoryRepository, CockroachHistoryRepository>()
                .ReplaceService<IMigrationsSqlGenerator, CockroachMigrationsSqlGenerator>());

        return services;
    }
    public static WebApplication ApplyMigrations(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BankDbContext>();
        db.Database.Migrate();
        return app;
    }
}
