using Gastito.DataAccess.Interfaces;
using Gastito.DataAccess.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gastito.DataAccess;

public static class DependencyInjection
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString, b => b.MigrationsAssembly("Gastito.DataAccess"));
        });

        // Register Stores
        services.AddScoped<ICategoryStore, CategoryStore>();
        services.AddScoped<ITarjetaStore, TarjetaStore>();
        services.AddScoped<ITransactionStore, TransactionStore>();

        return services;
    }
}
