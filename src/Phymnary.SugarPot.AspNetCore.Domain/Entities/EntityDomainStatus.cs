using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Phymnary.SugarPot.AspNetCore.EntityFrameworkCore")]
[assembly: InternalsVisibleTo("Phymnary.SugarPot.AspNetCore.EntityFrameworkCore.Tests")]

namespace Phymnary.SugarPot.AspNetCore.Entities;

[NotMapped]
public class EntityDomainStatus
{
    /// <summary>
    /// If true, entity's state will become added in DbContext when run EfRepository.UpdateAsync.
    /// Main usage is for adding non-aggregate root entity
    /// </summary>
    internal bool IsAdded { get; private set; }

    /// <summary>
    /// If true, modify the entity soft delete properties when run EfRepository.UpdateAsync.
    /// </summary>
    internal bool IsSoftDeleted { get; private set; }

    public void OnAttached()
    {
        IsAdded = true;
    }

    public void SoftDelete()
    {
        IsSoftDeleted = true;
    }

    internal void GotAdded()
    {
        IsAdded = false;
    }

    internal void GotSoftDeleted()
    {
        IsSoftDeleted = false;
    }
}
