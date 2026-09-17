using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Phymnary.SugarPot.AspNetCore.Entities;
using Phymnary.SugarPot.AspNetCore.Interceptors.Trackers;

namespace Phymnary.SugarPot.AspNetCore.Auditings;

public class EfAuditingServiceConfigurator<TAuditingDbContext>
    where TAuditingDbContext : DbContext
{
    private readonly IServiceCollection _services;

    private readonly EfAuditingStructure _auditingStructure = new();

    internal EfAuditingServiceConfigurator(IServiceCollection services, Type mainDbContextType)
    {
        _services = services;

        _services.AddSingleton(_auditingStructure);
        _services.AddScoped<IEfAuditor, EfAuditor<TAuditingDbContext>>();
        _auditingStructure.HasDifferentDbContextForAuditing =
            typeof(TAuditingDbContext) != mainDbContextType;
    }

    public EfAuditingServiceConfigurator<TAuditingDbContext> ConfigureStructure(
        Action<EfAuditingStructure> configure
    )
    {
        configure(_auditingStructure);
        return this;
    }

    public EfAuditingServiceConfigurator<TAuditingDbContext> AddPropertyChangeAudit<TAudit>(
        Func<IPropertyChangeAudit, TAudit>? mapper = null
    )
        where TAudit : class, IPropertyChangeAudit, IEntity
    {
        if (mapper != null)
        {
            _services.AddSingleton<IAuditingEntityMapper<IPropertyChangeAudit, TAudit>>(
                new AuditingEntityMapper<IPropertyChangeAudit, TAudit> { MapFn = mapper }
            );
        }

        _services.AddScoped<IAuditChangeTracker, EntityPropertyChangeTracker<TAudit>>();

        return this;
    }
}
