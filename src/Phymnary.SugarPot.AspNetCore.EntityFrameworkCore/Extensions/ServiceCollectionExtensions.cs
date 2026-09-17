using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Phymnary.SugarPot.AspNetCore.Repositories;

namespace Phymnary.SugarPot.AspNetCore.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEfCoreServices<TDbContext>(
        this IServiceCollection services,
        Action<EfServicesConfigurator<TDbContext>>? configure = null
    )
        where TDbContext : DbContext
    {
        var configurator = new EfServicesConfigurator<TDbContext>(services);
        configure?.Invoke(configurator);

        return AddUnderlayServices<TDbContext>(services);
    }

    private static IServiceCollection AddUnderlayServices<TDbContext>(IServiceCollection services)
        where TDbContext : DbContext
    {
        return services
            .AddScoped<IDbFunctionProvider, DbFunctionProvider<TDbContext>>()
            .AddScoped<EfDbStateManager>()
            .AddScoped<EfRepositoryAddons>();
    }
}
