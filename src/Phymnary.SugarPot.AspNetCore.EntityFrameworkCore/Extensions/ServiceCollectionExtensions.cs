using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Phymnary.SugarPot.AspNetCore.Repositories;

namespace Phymnary.SugarPot.AspNetCore.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEfCoreServices<TDbContext>(
        this IServiceCollection services,
        Func<EfServicesConfigurator<TDbContext>, EfServicesConfigurator<TDbContext>>? configure =
            null
    )
        where TDbContext : DbContext
    {
        configure ??= (configurator => configurator);

        return AddCoreServices<TDbContext>(
            configure(new EfServicesConfigurator<TDbContext>(services)).Finish()
        );
    }

    private static IServiceCollection AddCoreServices<TDbContext>(IServiceCollection services)
        where TDbContext : DbContext
    {
        return services
            .AddScoped<IDbFunctionProvider, DbFunctionProvider<TDbContext>>()
            .AddScoped<EfDbStateManager>()
            .AddScoped<EfRepositoryAddons>();
    }
}
