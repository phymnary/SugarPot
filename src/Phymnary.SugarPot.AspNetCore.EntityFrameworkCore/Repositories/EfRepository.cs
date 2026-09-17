using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Phymnary.SugarPot.AspNetCore.Entities;
using Phymnary.SugarPot.AspNetCore.Exceptions;
using Phymnary.SugarPot.AspNetCore.Repositories.AdvanceQueries;

namespace Phymnary.SugarPot.AspNetCore.Repositories;

public abstract class EfRepository<TDbContext, TEntity>(
    TDbContext dbContext,
    IRepositoryOptions<TEntity> options,
    EfRepositoryAddons addons
) : IRepository<TEntity>
    where TDbContext : DbContext
    where TEntity : class, IEntity
{
    private static EntityQueryOptions<TEntity>? _fallbackQueryOptions;
    private readonly EntityQueryOptions<TEntity> _queryOptions =
        options.QueryOptions ?? (_fallbackQueryOptions ??= new EntityQueryOptions<TEntity>());

    private static EntityUpdateOptions<TEntity>? _fallbackUpdateOptions;
    private readonly EntityUpdateOptions<TEntity> _updateOptions =
        options.UpdateOptions ?? (_fallbackUpdateOptions ??= new EntityUpdateOptions<TEntity>());

    protected DbSet<TEntity> DbSet { get; } = dbContext.Set<TEntity>();

    protected CancellationToken GetRequestAborted(CancellationToken cancellationToken)
    {
        return addons.AbortedProvider.Get(cancellationToken);
    }

    protected IQueryable<TEntity> Queryable(bool canIncludeDetails = false)
    {
        var queryable = _queryOptions.DefaultIncludeQuery.Invoke(DbSet);
        if (canIncludeDetails)
            queryable = _queryOptions.IncludeDetailsQuery.Invoke(queryable);

        return queryable;
    }

    protected async ValueTask ValidateAndThrowAsync(TEntity entity, CancellationToken ct)
    {
        if (options.Validator is null)
            return;

        var result = await options.Validator.ValidateAsync(entity, ct);
        if (!result.IsValid)
            throw new EntityValidationException(
                $"Entity {typeof(TEntity).Name} failed validation at domain level"
            )
            {
                Failures = [.. result.Errors],
            };
    }

    private async Task<int> SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            foreach (var interceptor in addons.SavingInterceptors)
            {
                await interceptor.RunAsync(ct);
            }

            return await dbContext.SaveChangesAsync(
                acceptAllChangesOnSuccess: !addons.DbStateManager.IsExecutionStrategyInTransaction,
                cancellationToken: ct
            );
        }
        catch (DbUpdateException ex)
        {
            throw new EntityPersistenceException(
                $"Failed to persist {typeof(TEntity).Name} to database",
                ex
            );
        }
    }

    public async Task<TEntity> InsertAsync(
        TEntity entity,
        bool autoSave = true,
        CancellationToken cancellationToken = default
    )
    {
        var ct = GetRequestAborted(cancellationToken);

        await ValidateAndThrowAsync(entity, ct);

        var inserted = DbSet.Add(entity).Entity;

        if (autoSave)
            await SaveChangesAsync(ct);

        return inserted;
    }

    public async Task<TEntity> UpsertAsync(
        TEntity entity,
        Expression<Func<TEntity, bool>> on,
        bool autoSave = true,
        bool canIncludeDetails = false,
        CancellationToken cancellationToken = default
    )
    {
        var ct = GetRequestAborted(cancellationToken);
        var upsert = await Queryable(canIncludeDetails).FirstOrDefaultAsync(on, ct);

        if (upsert is not null)
        {
            if (_updateOptions.Update is null)
                throw new DomainNotImplementedException(
                    "Must provide update function in EfUpdateOptions"
                );
            _updateOptions.Update(entity, upsert);
            await ValidateAndThrowAsync(upsert, ct);
        }
        else
        {
            await ValidateAndThrowAsync(entity, ct);
            upsert = await InsertAsync(entity, false, ct);
        }

        if (autoSave)
            await SaveChangesAsync(ct);

        return upsert;
    }

    public async Task<int> UpdateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default
    )
    {
        var ct = GetRequestAborted(cancellationToken);
        await ValidateAndThrowAsync(entity, ct);

        return await SaveChangesAsync(ct);
    }

    public async Task<TEntity?> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool canIncludeDetails = false,
        CancellationToken cancellationToken = default
    )
    {
        var ct = GetRequestAborted(cancellationToken);
        return await Queryable(canIncludeDetails).FirstOrDefaultAsync(predicate, ct);
    }

    public async Task<List<TEntity>> QueryAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        bool canIncludeDetails = false,
        CancellationToken cancellationToken = default
    )
    {
        var ct = GetRequestAborted(cancellationToken);
        if (predicate == null)
            return await Queryable(canIncludeDetails).ToListAsync(ct);

        return await Queryable(canIncludeDetails).Where(predicate).ToListAsync(ct);
    }

    public async Task<bool> AnyAsync(CancellationToken cancellationToken = default)
    {
        var ct = GetRequestAborted(cancellationToken);
        return await DbSet.AnyAsync(ct);
    }

    public async Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default
    )
    {
        var ct = GetRequestAborted(cancellationToken);
        return await DbSet.AnyAsync(predicate, ct);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        var ct = GetRequestAborted(cancellationToken);
        return await DbSet.CountAsync(ct);
    }

    public async Task<int> CountAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default
    )
    {
        var ct = GetRequestAborted(cancellationToken);
        return await DbSet.CountAsync(predicate, ct);
    }

    public IAdvanceOrderBuilding<TEntity> AdvanceQuery(
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? filter = null,
        bool? canIncludeDetails = null
    )
    {
        var dbSet = canIncludeDetails is { } include ? Queryable(include) : DbSet;
        var queryable = filter is not null ? filter(dbSet) : dbSet;

        return new AdvanceQueryBuilder<TEntity>(
            cancellationToken => queryable.CountAsync(cancellationToken),
            queryable,
            GetRequestAborted
        );
    }

    public void Delete(TEntity entity)
    {
        if (_updateOptions.OnDelete?.Invoke(entity) is true)
        {
            return;
        }

        if (entity is ISoftDelete softDelete)
        {
            softDelete.Delete();
        }
        else
        {
            dbContext.Remove(entity);
        }
    }
}

public abstract class EfRepository<TDbContext, TEntity, TKey>(
    TDbContext dbContext,
    IRepositoryOptions<TEntity> options,
    EfRepositoryAddons addons
) : EfRepository<TDbContext, TEntity>(dbContext, options, addons), IRepository<TEntity, TKey>
    where TDbContext : DbContext
    where TEntity : Entity<TKey>
    where TKey : IComparable, IComparable<TKey>, IEquatable<TKey>
{
    public async Task<TEntity> GetAsync(
        TKey id,
        bool canIncludeDetails = false,
        CancellationToken cancellationToken = default
    )
    {
        var ct = GetRequestAborted(cancellationToken);
        return await Queryable(canIncludeDetails).FirstOrDefaultAsync(e => e.Id.Equals(id), ct)
            ?? throw new EntityNotFoundException(
                $"Could not find {typeof(TEntity).Name} with id {id}"
            );
    }
}
