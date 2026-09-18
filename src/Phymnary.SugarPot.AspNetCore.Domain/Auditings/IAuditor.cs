using Phymnary.SugarPot.AspNetCore.Entities;

namespace Phymnary.SugarPot.AspNetCore.Auditings;

public interface IAuditor
{
    void Add<TAudit>(IEnumerable<TAudit> propertyAudits)
        where TAudit : class, IAudit;
}
