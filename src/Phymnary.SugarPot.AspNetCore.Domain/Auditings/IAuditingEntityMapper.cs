namespace Phymnary.SugarPot.AspNetCore.Auditings;

public interface IAuditingEntityMapper<TImplement>
    where TImplement : class
{
    TImplement Map(PropertyChangeAuditHydration concrete);
}
