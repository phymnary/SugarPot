namespace Phymnary.SugarPot.AspNetCore.Auditings;

internal class AuditingEntityMapper<TImplement> : IAuditingEntityMapper<TImplement>
    where TImplement : class
{
    public required Func<PropertyChangeAuditHydration, TImplement> MapFn { get; init; }

    public TImplement Map(PropertyChangeAuditHydration concrete)
    {
        return MapFn(concrete);
    }
}
