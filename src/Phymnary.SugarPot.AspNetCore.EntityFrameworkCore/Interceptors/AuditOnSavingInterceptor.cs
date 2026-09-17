using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Phymnary.SugarPot.AspNetCore.Auditings;
using Phymnary.SugarPot.AspNetCore.Interceptors.Trackers;
using Phymnary.SugarPot.AspNetCore.Security;
using Phymnary.SugarPot.Module.Extensions;

namespace Phymnary.SugarPot.AspNetCore.Interceptors;

internal class AuditOnSavingInterceptor<TDbContext>(
    TDbContext dbContext,
    ICurrentUser currentUser,
    IEnumerable<IAuditChangeTracker> changeTrackers,
    IRunAt requestedAt
) : IEfOnSavingEffect
    where TDbContext : DbContext
{
    private readonly DateTimeOffset _at = requestedAt.Value;

    public async ValueTask RunAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in dbContext.ChangeTracker.Entries<IAuditable>())
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Property(e => e.CreatedAt).CurrentValue = _at;
                    entry.Property(e => e.CreatedById).CurrentValue = currentUser.Id.NullIfEmpty();
                    break;
                case EntityState.Modified:
                    await AuditChangesAsync(entry, cancellationToken);
                    break;
                case EntityState.Unchanged:
                    if (IsModified(entry))
                        await AuditChangesAsync(entry, cancellationToken);
                    break;
                case EntityState.Detached:
                case EntityState.Deleted:
                default:
                    break;
            }
    }

    private static bool IsModified(EntityEntry entry)
    {
        return entry.State == EntityState.Modified
            || entry.References.Any(refEntry =>
                refEntry.TargetEntry != null
                && refEntry.TargetEntry.Metadata.IsOwned()
                && (refEntry.IsModified || IsModified(refEntry.TargetEntry))
            );
    }

    public async Task AuditChangesAsync(EntityEntry<IAuditable> entry, CancellationToken ct)
    {
        foreach (var tracker in changeTrackers)
        {
            await tracker.TrackAsync(entry, _at, ct);
        }

        entry.Property(e => e.UpdatedAt).CurrentValue = _at;
        entry.Property(e => e.UpdatedById).CurrentValue = currentUser.Id.NullIfEmpty();
    }
}
