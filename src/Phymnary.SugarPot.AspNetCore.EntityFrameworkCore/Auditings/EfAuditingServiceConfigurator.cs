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

    internal EfAuditingServiceConfigurator(IServiceCollection services)
    {
        _services = services;

        _services.AddSingleton(_auditingStructure);
        _services.AddScoped<IAuditor, EfAuditor<TAuditingDbContext>>();
    }

    public EfAuditingServiceConfigurator<TAuditingDbContext> ConfigureStructure(
        Action<EfAuditingStructure> configure
    )
    {
        configure(_auditingStructure);
        return this;
    }

    public EfAuditingServiceConfigurator<TAuditingDbContext> AddPropertyChangeAudit<TAudit>(
        Func<PropertyChangeAuditHydration, TAudit>? mapper = null
    )
        where TAudit : class, IPropertyChangeAudit, IEntity
    {
        if (mapper != null)
        {
            _services.AddSingleton<IAuditingEntityMapper<TAudit>>(
                new AuditingEntityMapper<TAudit> { MapFn = mapper }
            );
        }

        _services.AddScoped<IAuditChangeTracker, EntityPropertyChangeTracker<TAudit>>();

        return this;
    }
}
