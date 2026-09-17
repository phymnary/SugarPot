using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Phymnary.SugarPot.AspNetCore.Auditings;
using Phymnary.SugarPot.AspNetCore.Interceptors;

namespace Phymnary.SugarPot.AspNetCore;

public class EfServicesConfigurator<TDbContext>
    where TDbContext : DbContext
{
    private readonly IServiceCollection _services;

    private bool _hasConfigureAuditing = false;

    internal EfServicesConfigurator(IServiceCollection services)
    {
        _services = services;
        // Call first so that OnAttachedInterceptor is always the first interceptor to be executed, ensuring that entities are properly attached before any other interceptor runs.
        _services.AddScoped<IEfOnSavingEffect, OnAttachedInterceptor<TDbContext>>();
    }

    public EfServicesConfigurator<TDbContext> AddSoftDelete()
    {
        _services.AddScoped<IEfOnSavingEffect, SoftDeleteInterceptor<TDbContext>>();
        return this;
    }

    public EfServicesConfigurator<TDbContext> AddMultiTenancy()
    {
        _services.AddScoped<IEfOnSavingEffect, SetTenantOnSavingInterceptor<TDbContext>>();
        return this;
    }

    public void CheckIfAuditingIsAlreadyConfigured()
    {
        if (_hasConfigureAuditing)
            throw new InvalidOperationException("Auditing is already configured.");
        _hasConfigureAuditing = true;
    }

    public EfServicesConfigurator<TDbContext> AddAuditing(
        Action<EfAuditingServiceConfigurator<TDbContext>> auditConfigurator
    )
    {
        CheckIfAuditingIsAlreadyConfigured();

        _services.AddScoped<IEfOnSavingEffect, AuditOnSavingInterceptor<TDbContext>>();
        var auditingServiceConfigurator = new EfAuditingServiceConfigurator<TDbContext>(
            _services,
            typeof(TDbContext)
        );
        auditConfigurator.Invoke(auditingServiceConfigurator);
        return this;
    }

    public EfServicesConfigurator<TDbContext> AddAuditing<TAuditingDbContext>(
        Action<EfAuditingServiceConfigurator<TAuditingDbContext>> auditConfigurator
    )
        where TAuditingDbContext : DbContext
    {
        CheckIfAuditingIsAlreadyConfigured();

        _services.AddScoped<IEfOnSavingEffect, AuditOnSavingInterceptor<TAuditingDbContext>>();
        var auditingServiceConfigurator = new EfAuditingServiceConfigurator<TAuditingDbContext>(
            _services,
            typeof(TDbContext)
        );
        auditConfigurator.Invoke(auditingServiceConfigurator);
        return this;
    }
}
