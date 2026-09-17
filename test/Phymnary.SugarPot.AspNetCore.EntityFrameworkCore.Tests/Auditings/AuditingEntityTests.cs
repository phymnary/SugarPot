using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Diagnostics.Internal;
using Microsoft.Extensions.DependencyInjection;
using Phymnary.SugarPot.AspNetCore.Auditings;
using Phymnary.SugarPot.AspNetCore.Entities;
using Phymnary.SugarPot.AspNetCore.Extensions;
using Phymnary.SugarPot.AspNetCore.Security;

namespace Phymnary.SugarPot.AspNetCore.EntityFrameworkCore.Tests.Auditings;

public class AuditingEntityTests
{
    private sealed class TestDbContext(
        DbContextOptions<TestDbContext> options,
        IEnumerable<IInterceptor> interceptors
    ) : DbContext(options)
    {
        public DbSet<AuditableBook> Books => Set<AuditableBook>();

        public DbSet<PropertyChangeAuditLog> PropertyChangeAudits => Set<PropertyChangeAuditLog>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
            optionsBuilder.AddInterceptors(interceptors);
    }

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

        public string GetAuditKey() => "\"" + Id.ToString() + "\"";
    }

    private sealed class StubCurrentUser(Guid? id) : ICurrentUser
    {
        public Guid? Id { get; } = id;
    }

    private sealed class StubRunAt(DateTimeOffset value) : IRunAt
    {
        public DateTimeOffset Value { get; } = value;
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

        public bool IsDeleted { get; set; }
    }

    private static ServiceProvider BuildServices(string dbName, Guid? userId, DateTimeOffset now)
    {
        var services = new ServiceCollection();

        services
            .AddScoped<ICurrentUser>(_ => new StubCurrentUser(userId))
            .AddScoped<IRunAt>(_ => new StubRunAt(now))
            .AddDbContext<TestDbContext>((options) => options.UseInMemoryDatabase(dbName))
            .AddEfCoreServices<TestDbContext>(configurator =>
            {
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
                        IsDeleted = data.IsDeleted,
                    })
                );
            });

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
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            db.Books.Add(new AuditableBook(entityId) { Title = "Domain-Driven Design" });

            await db.SaveChangesAsync(ct);
        }

        await using (var scope = services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            var entity = await db.Books.SingleAsync(x => x.Id == entityId, ct);
            var auditsCount = await db.PropertyChangeAudits.CountAsync(ct);

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
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            db.Books.Add(new AuditableBook(entityId) { Title = "Clean Code" });
            await db.SaveChangesAsync(ct);
        }

        await using (var scope = services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            var entity = await db.Books.SingleAsync(x => x.Id == entityId, ct);
            entity.Title = "Clean Coder";

            await db.SaveChangesAsync(ct);
        }

        await using (var scope = services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            var entity = await db.Books.SingleAsync(x => x.Id == entityId, ct);
            var audit = await db.PropertyChangeAudits.SingleAsync(ct);

            Assert.Equal(now, entity.UpdatedAt);
            Assert.Equal(userId, entity.UpdatedById);
            Assert.Equal("AuditableBook", audit.EntityName);
            Assert.Equal("Title", audit.PropertyName);
            Assert.Equal("\"" + entityId.ToString() + "\"", audit.EntityId);
            Assert.Equal("\"Clean Code\"", audit.OldValue);
            Assert.Equal("\"Clean Coder\"", audit.NewValue);
            Assert.Equal(userId, audit.ModifiedById);
            Assert.Equal(now, audit.ModifiedAt);
        }
    }

    [Fact]
    public void test()
    {
        var services = new ServiceCollection();
        var x = services.BuildServiceProvider().GetService<IEnumerable<IInterceptor>>();
    }
}
