using Microsoft.EntityFrameworkCore;
using Phymnary.SugarPot.AspNetCore.Entities;

namespace Phymnary.SugarPot.AspNetCore.Interceptors;

public class OnAttachedInterceptor<TDbContext>(TDbContext dbContext) : IEfOnSavingEffect
    where TDbContext : DbContext
{
    public ValueTask RunAsync(CancellationToken cancellationToken = default)
    {
        foreach (
            var entry in dbContext
                .ChangeTracker.Entries<IEntity>()
                .Where(entry => entry.Entity.DomainStatus.IsAdded)
        )
        {
            entry.State = EntityState.Added;
            entry.Entity.DomainStatus.IsAdded = false;
        }

        return ValueTask.CompletedTask;
    }
}
