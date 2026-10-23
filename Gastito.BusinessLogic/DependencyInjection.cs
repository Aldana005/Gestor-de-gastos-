using Gastito.BusinessLogic.Interfaces;
using Gastito.BusinessLogic.Managers;
using Gastito.DataAccess;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gastito.BusinessLogic;

public static class DependencyInjection
{
    public static IServiceCollection AddBusinessLogic(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDataAccess(configuration);

        // Register Managers
        services.AddScoped<IAuthManager, AuthManager>();
        services.AddScoped<ICategoryManager, CategoryManager>();
        services.AddScoped<ITarjetaManager, TarjetaManager>();
        services.AddScoped<ITransactionManager, TransactionManager>();
        services.AddScoped<IDashboardManager, DashboardManager>();
        services.AddScoped<IReportesManager, ReportesManager>();

        return services;
    }
}
