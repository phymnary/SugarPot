using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Phymnary.SugarPot.AspNetCore.Auditings;
using Phymnary.SugarPot.AspNetCore.Entities;
using Phymnary.SugarPot.AspNetCore.Extensions;
using Phymnary.SugarPot.AspNetCore.Repositories;
using Phymnary.SugarPot.AspNetCore.Security;

namespace Phymnary.SugarPot.AspNetCore.EntityFrameworkCore.Tests.Auditings;

public class AuditingEntityTests
{
    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<AuditableBook> Books => Set<AuditableBook>();

        public DbSet<AuditableCustomer> Customers => Set<AuditableCustomer>();

        public DbSet<PropertyChangeAuditLog> PropertyChangeAudits => Set<PropertyChangeAuditLog>();
    }

    private sealed class EmptyRepositoryOptions<TEntity> : EfRepositoryOptions<TEntity>
        where TEntity : class, IEntity;

    private sealed class AuditableBookRepository(
        TestDbContext dbContext,
        IRepositoryOptions<AuditableBook> options,
        EfRepositoryAddons addons
    ) : EfRepository<TestDbContext, AuditableBook, Guid>(dbContext, options, addons);

    private sealed class AuditableCustomerRepository(
        TestDbContext dbContext,
        IRepositoryOptions<AuditableCustomer> options,
        EfRepositoryAddons addons
    ) : EfRepository<TestDbContext, AuditableCustomer, Guid>(dbContext, options, addons);

    private sealed class AuditableBook : Entity<Guid>, IAuditable
    {
        public AuditableBook() { }

        public AuditableBook(Guid id)
            : base(id) { }

        public required string Title { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public Guid? CreatedById { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        public Guid? UpdatedById { get; set; }

        public string GetAuditKey() => Id.ToString();
    }

    private sealed class AuditableCustomer : Entity<Guid>, IAuditable
    {
        public AuditableCustomer() { }

        public AuditableCustomer(Guid id)
            : base(id) { }

        public required string Name { get; set; }

        public required CustomerAddress Address { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public Guid? CreatedById { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        public Guid? UpdatedById { get; set; }

        public string GetAuditKey() => Id.ToString();
    }

    [Owned]
    private sealed class CustomerAddress
    {
        public required string City { get; set; }
    }

    private sealed class StubCurrentUser(Guid? id) : ICurrentUser
    {
        public Guid? Id { get; } = id;
    }

    private sealed class StubRunAt(DateTimeOffset value) : IRunAt
    {
        public DateTimeOffset Value { get; } = value;
    }

    private sealed class StubAbortedToken : IAbortedToken
    {
        public CancellationToken Get(CancellationToken cancellationToken)
        {
            return cancellationToken;
        }
    }

    private sealed class PropertyChangeAuditLog : Entity<Guid>, IPropertyChangeAudit
    {
        public PropertyChangeAuditLog() { }

        public PropertyChangeAuditLog(Guid id)
            : base(id) { }

        public required string EntityName { get; set; }

        public required string PropertyName { get; set; }

        public required string TypeName { get; set; }

        public required string EntityId { get; set; }

        public required string OldValue { get; set; }

        public required string NewValue { get; set; }

        public Guid? ModifiedById { get; set; }

        public DateTimeOffset ModifiedAt { get; set; }
    }

    private static ServiceProvider BuildServices(string dbName, Guid? userId, DateTimeOffset now)
    {
        var services = new ServiceCollection();

        services
            .AddScoped<ICurrentUser>(_ => new StubCurrentUser(userId))
            .AddScoped<IRunAt>(_ => new StubRunAt(now))
            .AddScoped<IAbortedToken, StubAbortedToken>()
            .AddScoped<IRepositoryOptions<AuditableBook>, EmptyRepositoryOptions<AuditableBook>>()
            .AddScoped<AuditableBookRepository>()
            .AddScoped<
                IRepositoryOptions<AuditableCustomer>,
                EmptyRepositoryOptions<AuditableCustomer>
            >()
            .AddScoped<AuditableCustomerRepository>()
            .AddDbContext<TestDbContext>(options => options.UseInMemoryDatabase(dbName))
            .AddEfCoreServices<TestDbContext>(configurator =>
                configurator.AddAuditing(auditing =>
                    auditing.AddPropertyChangeAudit(data => new PropertyChangeAuditLog(
                        Guid.NewGuid()
                    )
                    {
                        EntityId = data.EntityId,
                        EntityName = data.EntityName,
                        PropertyName = data.PropertyName,
                        TypeName = data.TypeName,
                        OldValue = data.OldValue,
                        NewValue = data.NewValue,
                        ModifiedById = data.ModifiedById,
                        ModifiedAt = data.ModifiedAt,
                    })
                )
            );

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task on_entity_added_set_created_auditing_values()
    {
        var dbName = $"audit-added-{Guid.NewGuid()}";
        var ct = TestContext.Current.CancellationToken;
        var userId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 01, 17, 08, 15, 00, TimeSpan.Zero);
        var services = BuildServices(dbName, userId, now);

        var entityId = Guid.NewGuid();

        await using (var scope = services.CreateAsyncScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<AuditableBookRepository>();
            await repository.InsertAsync(
                new AuditableBook(entityId) { Title = "Domain-Driven Design" },
                cancellationToken: ct
            );
        }

        await using (var scope = services.CreateAsyncScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<AuditableBookRepository>();
            var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            var entity = await repository.GetAsync(entityId, cancellationToken: ct);
            var auditsCount = await dbContext.PropertyChangeAudits.CountAsync(ct);

            Assert.Equal(now, entity.CreatedAt);
            Assert.Equal(userId, entity.CreatedById);
            Assert.Null(entity.UpdatedAt);
            Assert.Null(entity.UpdatedById);
            Assert.Equal(0, auditsCount);
        }
    }

    [Fact]
    public async Task on_entity_modified_set_updated_auditing_values_and_track_changes()
    {
        var dbName = $"audit-modified-{Guid.NewGuid()}";
        var ct = TestContext.Current.CancellationToken;
        var userId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 01, 17, 08, 20, 00, TimeSpan.Zero);
        var services = BuildServices(dbName, userId, now);

        var entityId = Guid.NewGuid();

        await using (var scope = services.CreateAsyncScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<AuditableBookRepository>();
            await repository.InsertAsync(
                new AuditableBook(entityId) { Title = "Clean Code" },
                cancellationToken: ct
            );
        }

        await using (var scope = services.CreateAsyncScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<AuditableBookRepository>();
            var entity = await repository.GetAsync(entityId, cancellationToken: ct);
            entity.Title = "Clean Coder";

            await repository.UpdateAsync(entity, ct);
        }

        await using (var scope = services.CreateAsyncScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<AuditableBookRepository>();
            var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            var entity = await repository.GetAsync(entityId, cancellationToken: ct);
            var audit = await dbContext.PropertyChangeAudits.SingleAsync(ct);

            Assert.Equal(now, entity.UpdatedAt);
            Assert.Equal(userId, entity.UpdatedById);
            Assert.Equal("AuditableBook", audit.EntityName);
            Assert.Equal("Title", audit.PropertyName);
            Assert.Equal(entityId.ToString(), audit.EntityId);
            Assert.Equal("\"Clean Code\"", audit.OldValue);
            Assert.Equal("\"Clean Coder\"", audit.NewValue);
            Assert.Equal(userId, audit.ModifiedById);
            Assert.Equal(now, audit.ModifiedAt);
        }
    }

    [Fact]
    public async Task on_owned_entity_property_modified_track_owned_property_change()
    {
        var dbName = $"audit-owned-{Guid.NewGuid()}";
        var ct = TestContext.Current.CancellationToken;
        var userId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 01, 17, 08, 25, 00, TimeSpan.Zero);
        var services = BuildServices(dbName, userId, now);

        var entityId = Guid.NewGuid();

        await using (var scope = services.CreateAsyncScope())
        {
            var repository =
                scope.ServiceProvider.GetRequiredService<AuditableCustomerRepository>();
            await repository.InsertAsync(
                new AuditableCustomer(entityId)
                {
                    Name = "Jane",
                    Address = new CustomerAddress { City = "New York" },
                },
                cancellationToken: ct
            );
        }

        await using (var scope = services.CreateAsyncScope())
        {
            var repository =
                scope.ServiceProvider.GetRequiredService<AuditableCustomerRepository>();
            var entity = await repository.GetAsync(entityId, cancellationToken: ct);
            entity.Address.City = "Paris";

            await repository.UpdateAsync(entity, ct);
        }

        await using (var scope = services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            var audit = await dbContext.PropertyChangeAudits.SingleAsync(ct);

            Assert.Equal("AuditableCustomer", audit.EntityName);
            Assert.Equal("Address.City", audit.PropertyName);
            Assert.Equal(entityId.ToString(), audit.EntityId);
            Assert.Equal("\"New York\"", audit.OldValue);
            Assert.Equal("\"Paris\"", audit.NewValue);
            Assert.Equal(userId, audit.ModifiedById);
            Assert.Equal(now, audit.ModifiedAt);
        }
    }
}
