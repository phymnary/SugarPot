namespace Phymnary.SugarPot.AspNetCore.Auditings;

public sealed class PropertyChangeAuditHydration : IPropertyChangeAudit
{
    public required string EntityName { get; init; }

    public required string PropertyName { get; init; }

    public required string TypeName { get; init; }

    public required string EntityId { get; init; }

    public required string OldValue { get; init; }

    public required string NewValue { get; init; }

    public Guid? ModifiedById { get; init; }

    public DateTimeOffset ModifiedAt { get; init; }
}
