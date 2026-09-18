using Microsoft.EntityFrameworkCore;

namespace Phymnary.SugarPot.AspNetCore.Repositories.SaveChangesStrategies;

internal class DifferentDbContextAuditSaveChangesStrategy<TAuditDbContext>(
    TAuditDbContext auditDbContext
) : ISaveChangesStrategy
    where TAuditDbContext : DbContext
{
    public async Task SavingChangesAsync(
        Func<CancellationToken, Task> mainDbSaveChangesAsync,
        CancellationToken cancellationToken
    )
    {
        await auditDbContext.SaveChangesAsync(cancellationToken);
        try
        {
            await mainDbSaveChangesAsync(cancellationToken);
        }
        catch
        {
            foreach (var entry in auditDbContext.ChangeTracker.Entries())
            {
                entry.State = EntityState.Deleted;
            }
            await auditDbContext.SaveChangesAsync(cancellationToken);
            throw;
        }
    }
}
