using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Phymnary.SugarPot.Module.Extensions;

namespace Phymnary.SugarPot.AspNetCore.Auditings;

file record PropertyWithOwner(PropertyInfo PropertyInfo, string OwnedBy);

/// <summary>
/// Singleton service to store the auditing structure of application. It also caches the auditing metadata for each entity type.
/// </summary>
public class EfAuditingStructure
{
    private readonly Dictionary<
        Type,
        EntityPropertyAuditingMetadata
    > _propertyAuditingMetadataCaches = [];

    public TrackBy TrackBy { internal get; set; }

    public string IdPostfix { internal get; set; } = "Id";

    private IEnumerable<string> GetAuditingPropertyNames(IEnumerable<PropertyInfo> propertyInfos)
    {
        Stack<PropertyWithOwner> stack = new(
            propertyInfos.Select(p => new PropertyWithOwner(p, ""))
        );

        while (stack.TryPop(out var item))
        {
            var (propertyInfo, owned) = item;

            if (
                propertyInfo.HasAttribute<DisabledAuditingAttribute>()
                || propertyInfo.HasAttribute<NotMappedAttribute>()
                || propertyInfo.PropertyType.HasAttribute<NotMappedAttribute>()
            )
                continue;

            var propertyType = propertyInfo.PropertyType;

            if (propertyType.IsClass && propertyType != typeof(string))
            {
                if (propertyType.HasAttribute<OwnedAttribute>())
                {
                    foreach (var child in propertyType.GetProperties())
                    {
                        stack.Push(new PropertyWithOwner(child, owned + propertyInfo.Name + "."));
                    }
                }
                else
                {
                    yield return owned + propertyInfo.Name + IdPostfix;
                }
            }
            else
            {
                yield return owned + propertyInfo.Name;
            }
        }
    }

    internal EntityPropertyAuditingMetadata GetPropertyAuditingMetadata(Type entityType)
    {
        if (_propertyAuditingMetadataCaches.TryGetValue(entityType, out var value))
            return value;

        value = new EntityPropertyAuditingMetadata(
            entityType.GetCustomAttribute<AuditOnlyAttribute>()?.PropertyNames
                ?? GetAuditingPropertyNames(entityType.GetProperties())
        );

        _propertyAuditingMetadataCaches[entityType] = value;

        return value;
    }
}
