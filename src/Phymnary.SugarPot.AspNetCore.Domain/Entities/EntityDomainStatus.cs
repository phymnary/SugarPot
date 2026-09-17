using System.ComponentModel.DataAnnotations.Schema;

namespace Phymnary.SugarPot.AspNetCore.Entities;

[NotMapped]
public class EntityDomainStatus
{
    /// <summary>
    /// If true, entity's state will become added in DbContext when run EfRepository.UpdateAsync.
    /// Main usage is for adding non-aggregate root entity
    /// </summary>
    public bool IsAdded { get; private set; }

    /// <summary>
    /// If true, modify the entity soft delete properties when run EfRepository.UpdateAsync.
    /// </summary>
    public bool IsSoftDeleted { get; private set; }

    public void OnAttached()
    {
        IsAdded = true;
    }

    public void SoftDelete()
    {
        IsSoftDeleted = true;
    }

    public void GotAdded()
    {
        IsAdded = false;
    }

    public void GotSoftDeleted()
    {
        IsSoftDeleted = false;
    }
}
