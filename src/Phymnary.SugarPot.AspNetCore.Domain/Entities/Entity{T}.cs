using System.ComponentModel.DataAnnotations;

namespace Phymnary.SugarPot.AspNetCore.Entities;

public interface IEntity
{
    EntityDomainStatus DomainStatus { get; }
}

public abstract class Entity<TKey> : IEntity
    where TKey : IComparable, IComparable<TKey>, IEquatable<TKey>
{
    protected Entity() { }

    protected Entity(TKey id)
    {
        Id = id;
    }

    [Key]
    public TKey Id { get; protected init; } = default!;

    public EntityDomainStatus DomainStatus { get; } = new();
}
