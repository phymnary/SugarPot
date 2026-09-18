using Microsoft.EntityFrameworkCore;

namespace Phymnary.SugarPot.AspNetCore.Auditings;

internal class EfAuditor<TAuditDbContext>(TAuditDbContext auditDbContext) : IAuditor
    where TAuditDbContext : DbContext
{
    public void Add<TAudit>(IEnumerable<TAudit> propertyAudits)
        where TAudit : class, IAudit
    {
        auditDbContext.Set<TAudit>().AddRange(propertyAudits);
    }
}
