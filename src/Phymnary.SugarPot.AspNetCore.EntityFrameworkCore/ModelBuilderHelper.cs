using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Phymnary.SugarPot.AspNetCore.Entities;
using Phymnary.SugarPot.AspNetCore.MultiTenancy;
using static System.Linq.Expressions.Expression;

namespace Phymnary.SugarPot.AspNetCore;

public class ModelBuilderHelper
{
#if NET10_0_OR_GREATER
    public const string SoftDelelteQueryFilterName = "EfCoreBoilerplate.SoftDelete";

    public const string MultiTenancyQueryFilterName = "EfCoreBoilerplate.MultiTenancy";
#endif

    private ModelBuilder _builder;

    public ModelBuilderHelper(ModelBuilder builder)
    {
        _builder = builder;
        _builder.Ignore<EntityDomainStatus>();  
    }

    public Expression<Func<Guid>>? TenantIdAccessor { private get; init; }

    public ModelBuilderHelper BuildEntity<TEntity>(
        Action<EntityTypeBuilder<TEntity>>? additionalConfigure = null,
        string? schema = null
    )
        where TEntity : class, IEntity
    {
        _builder = BuildEntity(_builder, additionalConfigure, schema, TenantIdAccessor);

        return this;
    }

    public static ModelBuilder BuildEntity<TEntity>(
        ModelBuilder modelBuilder,
        Action<EntityTypeBuilder<TEntity>>? additionalConfigure = null,
        string? schema = null,
        Expression<Func<Guid>>? tenantIdProperty = null
    )
        where TEntity : class, IEntity
    {
        modelBuilder.Entity<TEntity>(b =>
        {
            var type = typeof(TEntity);

#if NET10_0_OR_GREATER
            if (type.IsAssignableTo(typeof(ISoftDelete)))
            {
                b.HasQueryFilter(
                    SoftDelelteQueryFilterName,
                    e => ((ISoftDelete)e).DeletedAt == null
                );
            }

            if (type.IsAssignableTo(typeof(IMultiTenant)))
            {
                if (tenantIdProperty == null)
                    throw new ArgumentNullException(
                        nameof(tenantIdProperty),
                        "Tenant ID property is required for multi-tenant entities"
                    );

                var entity = Parameter(type, "entity");
                var equal = Equal(
                    Property(entity, nameof(IMultiTenant.TenantId)),
                    tenantIdProperty.Body
                );
                b.HasQueryFilter(
                    MultiTenancyQueryFilterName,
                    Lambda<Func<TEntity, bool>>(equal, entity)
                );
            }
#else
            if (CreateQueryFilter<TEntity>(tenantIdProperty) is { } queryFilter)
                b.HasQueryFilter(queryFilter);
#endif
            additionalConfigure?.Invoke(b);
        });

        return modelBuilder;
    }

#if !NET10_0_OR_GREATER
    private static Expression<Func<TEntity, bool>>? CreateQueryFilter<TEntity>(
        Expression<Func<Guid>>? tenantIdProperty
    )
        where TEntity : IEntity
    {
        var type = typeof(TEntity);
        var entity = Parameter(type, "entity");
        List<BinaryExpression> conditions = [];

        if (type.IsAssignableTo(typeof(ISoftDelete)))
            conditions.Add(Equal(Property(entity, nameof(ISoftDelete.DeletedAt)), Constant(null)));

        if (type.IsAssignableTo(typeof(IMultiTenant)) && tenantIdProperty is not null)
            conditions.Add(
                Equal(Property(entity, nameof(IMultiTenant.TenantId)), tenantIdProperty.Body)
            );

        if (conditions.Count == 0)
            return null;

        var predicate = conditions.Aggregate<BinaryExpression, BinaryExpression?>(
            null,
            (current, condition) => current is null ? condition : And(current, condition)
        );

        return predicate is null ? null : Lambda<Func<TEntity, bool>>(predicate, entity);
    }
#endif
}
