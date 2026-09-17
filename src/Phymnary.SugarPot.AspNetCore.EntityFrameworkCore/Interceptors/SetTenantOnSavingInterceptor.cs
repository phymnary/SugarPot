using Microsoft.EntityFrameworkCore;
using Phymnary.SugarPot.AspNetCore.Exceptions;
using Phymnary.SugarPot.AspNetCore.MultiTenancy;

namespace Phymnary.SugarPot.AspNetCore.Interceptors;

public class SetTenantOnSavingInterceptor<TDbContext>(
    TDbContext dbContext,
    ICurrentTenant currentTenant
) : IEfOnSavingEffect
    where TDbContext : DbContext
{
    public ValueTask RunAsync(CancellationToken ct = default)
    {
        foreach (var entry in dbContext.ChangeTracker.Entries<IMultiTenant>())
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Property(e => e.TenantId).CurrentValue =
                        currentTenant.Id
                        ?? throw new TenantMissingInContextException("Missing tenant id in scope");
                    break;
                case EntityState.Modified:
                case EntityState.Detached:
                case EntityState.Unchanged:
                case EntityState.Deleted:
                default:
                    break;
            }

        return ValueTask.CompletedTask;
    }
}
