namespace Phymnary.SugarPot.AspNetCore.Auditings;

public interface IPropertyChangeAudit : IAudit
{
    string EntityName { get; }

    string EntityId { get; }

    string PropertyName { get; }

    string TypeName { get; }

    string OldValue { get; }

    string NewValue { get; }

    Guid? ModifiedById { get; }

    DateTimeOffset ModifiedAt { get; }
}
