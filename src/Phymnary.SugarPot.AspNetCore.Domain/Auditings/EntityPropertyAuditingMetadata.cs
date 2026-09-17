using System.Collections.Frozen;

namespace Phymnary.SugarPot.AspNetCore.Auditings;

public class EntityPropertyAuditingMetadata(IEnumerable<string> auditingProperties)
{
    private readonly FrozenSet<string> _auditingProperties = auditingProperties.ToFrozenSet();

    public bool CanAudit(string name)
    {
        return _auditingProperties.Contains(name);
    }
}
