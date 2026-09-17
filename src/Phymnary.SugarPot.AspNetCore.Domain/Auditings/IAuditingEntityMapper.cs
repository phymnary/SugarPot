namespace Phymnary.SugarPot.AspNetCore.Auditings;

public interface IAuditingEntityMapper<TConcrete, TImplement>
    where TImplement : class
{
    TImplement Map(TConcrete concrete);
}