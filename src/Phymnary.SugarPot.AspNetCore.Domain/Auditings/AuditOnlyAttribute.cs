namespace Phymnary.SugarPot.AspNetCore.Auditings;

/// <summary>
/// Specific what properties need to be audited of target class
/// </summary>
/// <param name="properties">Only audited these properties</param>
[AttributeUsage(AttributeTargets.Class)]
public class AuditOnlyAttribute(params string[] properties) : Attribute
{
    public string[] PropertyNames { get; } = properties;
}
