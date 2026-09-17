namespace Phymnary.SugarPot.AspNetCore.Auditings;

internal class AuditingEntityMapper<TConcrete, TImplement>
    : IAuditingEntityMapper<TConcrete, TImplement>
    where TImplement : class
{
    public required Func<TConcrete, TImplement> MapFn { get; init; }

    public TImplement Map(TConcrete concrete)
    {
        return MapFn(concrete);
    }
}
