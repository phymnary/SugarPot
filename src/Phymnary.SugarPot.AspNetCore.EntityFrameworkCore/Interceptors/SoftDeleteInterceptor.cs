using Microsoft.EntityFrameworkCore;
using Phymnary.SugarPot.AspNetCore.Entities;
using Phymnary.SugarPot.AspNetCore.Security;

namespace Phymnary.SugarPot.AspNetCore.Interceptors;

public class SoftDeleteInterceptor<TDbContext>(
    TDbContext dbContext,
    ICurrentUser currentUser,
    IRunAt requestedAt
) : IEfOnSavingEffect
    where TDbContext : DbContext
{
    public ValueTask RunAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in dbContext.ChangeTracker.Entries<ISoftDelete>())
        {
            if (!entry.Entity.DomainStatus.IsSoftDeleted)
                continue;

            entry.Entity.DeletedAt = requestedAt.Value;
            entry.Entity.DeletedById = currentUser.Id;

            entry.Entity.DomainStatus.GotSoftDeleted();
        }

        return ValueTask.CompletedTask;
    }
}
