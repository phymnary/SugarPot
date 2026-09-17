using Microsoft.EntityFrameworkCore;
using Phymnary.SugarPot.AspNetCore.Entities;

namespace Phymnary.SugarPot.AspNetCore.Auditings;

public interface IEfAuditor
{
    void AddPropertyAuditings<TAudit>(IEnumerable<TAudit> propertyAudits)
        where TAudit : class, IPropertyChangeAudit, IEntity;
}

internal class EfAuditor<TAuditDbContext>(
    TAuditDbContext auditDbContext,
    EfAuditingStructure structure
) : IEfAuditor
    where TAuditDbContext : DbContext
{
    public void AddPropertyAuditings<TAudit>(IEnumerable<TAudit> propertyAudits)
        where TAudit : class, IPropertyChangeAudit, IEntity
    {
        auditDbContext.Set<TAudit>().AddRange(propertyAudits);
    }

    public async Task SavingChangesAsync(
        Func<Task> mainDbSaveChangesAsync,
        CancellationToken cancellationToken
    )
    {
        if (!structure.HasDifferentDbContextForAuditing)
        {
            await mainDbSaveChangesAsync();
            return;
        }

        await auditDbContext.SaveChangesAsync(cancellationToken);
        try
        {
            await mainDbSaveChangesAsync();
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
