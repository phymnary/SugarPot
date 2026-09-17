using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Phymnary.SugarPot.AspNetCore.Interceptors.Trackers;

public interface IAuditChangeTracker
{
    ValueTask TrackAsync(EntityEntry entry, DateTimeOffset modifiedAt, CancellationToken ct);
}
