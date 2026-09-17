using Microsoft.EntityFrameworkCore;
using Phymnary.SugarPot.AspNetCore.Entities;
using Phymnary.SugarPot.AspNetCore.MultiTenancy;

namespace Phymnary.SugarPot.AspNetCore.EntityFrameworkCore.Tests;

public class ModelBuilderHelperQueryFilterTests
{
    #region Soft Delete

    private class SoftDeletablePost : Entity<Guid>, ISoftDelete
    {
        protected SoftDeletablePost() { }

        public SoftDeletablePost(Guid id)
            : base(id) { }

        public required string Title { get; set; }

        public Guid? DeletedById { get; set; }

        public DateTimeOffset? DeletedAt { get; set; }
    }

    private sealed class SoftDeleteDbContext(DbContextOptions<SoftDeleteDbContext> options)
        : DbContext(options)
    {
        public DbSet<SoftDeletablePost> Posts => Set<SoftDeletablePost>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ModelBuilderHelper.BuildEntity<SoftDeletablePost>(modelBuilder);
        }
    }

    [Fact]
    public async Task soft_delete_query_filter_excludes_deleted_entities()
    {
        var dbName = $"soft-delete-{Guid.NewGuid()}";
        var ct = TestContext.Current.CancellationToken;
        var options = new DbContextOptionsBuilder<SoftDeleteDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var activeId = Guid.NewGuid();
        var deletedId = Guid.NewGuid();

        await using (var db = new SoftDeleteDbContext(options))
        {
            db.Posts.AddRange(
                new SoftDeletablePost(activeId) { Title = "Active Post" },
                new SoftDeletablePost(deletedId)
                {
                    Title = "Deleted Post",
                    DeletedAt = DateTimeOffset.UtcNow,
                    DeletedById = Guid.NewGuid(),
                }
            );
            await db.SaveChangesAsync(ct);
        }

        await using (var db = new SoftDeleteDbContext(options))
        {
            var posts = await db.Posts.ToListAsync(ct);

            Assert.Single(posts);
            Assert.Equal(activeId, posts[0].Id);
        }
    }

    [Fact]
    public async Task soft_delete_query_filter_can_be_bypassed_with_ignore_query_filters()
    {
        var dbName = $"soft-delete-bypass-{Guid.NewGuid()}";
        var ct = TestContext.Current.CancellationToken;
        var options = new DbContextOptionsBuilder<SoftDeleteDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var activeId = Guid.NewGuid();
        var deletedId = Guid.NewGuid();

        await using (var db = new SoftDeleteDbContext(options))
        {
            db.Posts.AddRange(
                new SoftDeletablePost(activeId) { Title = "Active Post" },
                new SoftDeletablePost(deletedId)
                {
                    Title = "Deleted Post",
                    DeletedAt = DateTimeOffset.UtcNow,
                    DeletedById = Guid.NewGuid(),
                }
            );
            await db.SaveChangesAsync(ct);
        }

        await using (var db = new SoftDeleteDbContext(options))
        {
            var posts = await db.Posts.IgnoreQueryFilters().ToListAsync(ct);

            Assert.Equal(2, posts.Count);
        }
    }

    #endregion

    #region Multi-Tenancy

    private class TenantAwareProduct : Entity<Guid>, IMultiTenant
    {
        protected TenantAwareProduct() { }

        public TenantAwareProduct(Guid id)
            : base(id) { }

        public required string Name { get; set; }

        public Guid TenantId { get; set; }
    }

    private sealed class MultiTenantDbContext(
        DbContextOptions<MultiTenantDbContext> options,
        Guid? currentTenantId
    ) : DbContext(options)
    {
        public DbSet<TenantAwareProduct> Products => Set<TenantAwareProduct>();

        public Guid CurrentTenantId => currentTenantId ?? default;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ModelBuilderHelper.BuildEntity<TenantAwareProduct>(
                modelBuilder,
                tenantIdProperty: () => CurrentTenantId
            );
        }
    }

    [Fact]
    public async Task multi_tenancy_query_filter_returns_only_current_tenant_entities()
    {
        var dbName = $"multi-tenancy-{Guid.NewGuid()}";
        var ct = TestContext.Current.CancellationToken;

        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var optionsA = new DbContextOptionsBuilder<MultiTenantDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        await using (var db = new MultiTenantDbContext(optionsA, tenantA))
        {
            db.Products.AddRange(
                new TenantAwareProduct(Guid.NewGuid()) { Name = "Product A1", TenantId = tenantA },
                new TenantAwareProduct(Guid.NewGuid()) { Name = "Product A2", TenantId = tenantA },
                new TenantAwareProduct(Guid.NewGuid()) { Name = "Product B1", TenantId = tenantB }
            );
            await db.SaveChangesAsync(ct);
        }

        await using (var db = new MultiTenantDbContext(optionsA, tenantA))
        {
            var products = await db.Products.ToListAsync(ct);

            Assert.Equal(2, products.Count);
            Assert.All(products, p => Assert.Equal(tenantA, p.TenantId));
        }

        await using (var db = new MultiTenantDbContext(optionsA, tenantB))
        {
            var products = await db.Products.ToListAsync(ct);

            Assert.Single(products);
            Assert.Equal(tenantB, products[0].TenantId);
        }
    }

    [Fact]
    public async Task multi_tenancy_query_filter_can_be_bypassed_with_ignore_query_filters()
    {
        var dbName = $"multi-tenancy-bypass-{Guid.NewGuid()}";
        var ct = TestContext.Current.CancellationToken;

        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var options = new DbContextOptionsBuilder<MultiTenantDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        await using (var db = new MultiTenantDbContext(options, tenantA))
        {
            db.Products.AddRange(
                new TenantAwareProduct(Guid.NewGuid()) { Name = "Product A1", TenantId = tenantA },
                new TenantAwareProduct(Guid.NewGuid()) { Name = "Product B1", TenantId = tenantB }
            );
            await db.SaveChangesAsync(ct);
        }

        await using (var db = new MultiTenantDbContext(options, tenantA))
        {
            var products = await db.Products.IgnoreQueryFilters().ToListAsync(ct);

            Assert.Equal(2, products.Count);
        }
    }

    #endregion

    #region Combined Soft Delete + Multi-Tenancy

    private class TenantAwareArticle : Entity<Guid>, ISoftDelete, IMultiTenant
    {
        protected TenantAwareArticle() { }

        public TenantAwareArticle(Guid id)
            : base(id) { }

        public required string Title { get; set; }

        public Guid TenantId { get; set; }

        public Guid? DeletedById { get; set; }

        public DateTimeOffset? DeletedAt { get; set; }
    }

    private sealed class CombinedDbContext(
        DbContextOptions<CombinedDbContext> options,
        Guid currentTenantId
    ) : DbContext(options)
    {
        public DbSet<TenantAwareArticle> Articles => Set<TenantAwareArticle>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ModelBuilderHelper.BuildEntity<TenantAwareArticle>(
                modelBuilder,
                tenantIdProperty: () => currentTenantId
            );
        }
    }

    [Fact]
    public async Task combined_filters_exclude_deleted_and_other_tenant_entities()
    {
        var dbName = $"combined-{Guid.NewGuid()}";
        var ct = TestContext.Current.CancellationToken;

        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var options = new DbContextOptionsBuilder<CombinedDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        await using (var db = new CombinedDbContext(options, tenantA))
        {
            db.Articles.AddRange(
                new TenantAwareArticle(Guid.NewGuid()) { Title = "A - Active", TenantId = tenantA },
                new TenantAwareArticle(Guid.NewGuid())
                {
                    Title = "A - Deleted",
                    TenantId = tenantA,
                    DeletedAt = DateTimeOffset.UtcNow,
                    DeletedById = Guid.NewGuid(),
                },
                new TenantAwareArticle(Guid.NewGuid()) { Title = "B - Active", TenantId = tenantB }
            );
            await db.SaveChangesAsync(ct);
        }

        await using (var db = new CombinedDbContext(options, tenantA))
        {
            var articles = await db.Articles.ToListAsync(ct);

            Assert.Single(articles);
            Assert.Equal("A - Active", articles[0].Title);
        }
    }

    #endregion
}
