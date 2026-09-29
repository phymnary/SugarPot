using Microsoft.EntityFrameworkCore.ChangeTracking;
using Phymnary.SugarPot.AspNetCore.Auditings;

namespace Phymnary.SugarPot.AspNetCore.Interceptors.Trackers;

public interface IAuditChangeTracker
{
    ValueTask TrackAsync(
        EntityEntry<IAuditable> entry,
        DateTimeOffset modifiedAt,
        CancellationToken ct = default
    );
}
