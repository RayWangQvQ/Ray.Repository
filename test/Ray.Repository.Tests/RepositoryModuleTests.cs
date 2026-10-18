using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ray.Repository;
using Ray.Repository.EntityFramework;

namespace Ray.Repository.Tests;

public class TestEntity : Ray.DDD.Entity<int>, Ray.DDD.ISoftDelete
{
    public string Name { get; set; } = string.Empty;

    public bool IsSoftDeleted { get; set; }
}

public class TestDbContext : RayDbContext<TestDbContext>
{
    public TestDbContext(DbContextOptions<TestDbContext> options)
        : base(options) { }

    public DbSet<TestEntity> TestEntities => Set<TestEntity>();
}

public class RepositoryModuleTests
{
    [Fact]
    public void AddDefaultRepositories_RegistersRepositoryAndUnitOfWork()
    {
        var services = new ServiceCollection();
        services.AddDefaultRepositories<TestDbContext>(o => o.UseSqlite("Data Source=:memory:"));

        using var provider = services.BuildServiceProvider();

        var repo = provider.GetService<IBaseRepository<TestEntity, int>>();
        var unitOfWork = provider.GetService<IUnitOfWork>();

        Assert.NotNull(repo);
        Assert.NotNull(unitOfWork);
    }
}

public class EFBaseRepositoryTests : IAsyncLifetime
{
    private readonly DbConnection _connection = new SqliteConnection("Data Source=:memory:");
    private TestDbContext _dbContext = null!;
    private EFBaseRepository<TestDbContext, TestEntity, int> _repository = null!;

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(_connection).Options;

        _dbContext = new TestDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();

        _repository = new EFBaseRepository<TestDbContext, TestEntity, int>(_dbContext, _dbContext);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task InsertAsync_AddsEntity()
    {
        var entity = new TestEntity { Name = "Book1" };

        var saved = await _repository.InsertAsync(entity, autoSave: true);

        Assert.NotEqual(0, saved.Id);
        var all = await _repository.GetAllListAsync();
        Assert.Single(all);
        Assert.Equal("Book1", all[0].Name);
    }

    [Fact]
    public async Task InsertManyAsync_AddsEntities()
    {
        var entities = new[]
        {
            new TestEntity { Name = "A" },
            new TestEntity { Name = "B" },
            new TestEntity { Name = "C" },
        };

        await _repository.InsertManyAsync(entities, autoSave: true);

        var all = await _repository.GetAllListAsync();
        Assert.Equal(3, all.Count);
    }

    [Fact]
    public async Task GetListAsync_FiltersByPredicate()
    {
        await _repository.InsertManyAsync(
            new[]
            {
                new TestEntity { Name = "Alpha" },
                new TestEntity { Name = "Beta" },
                new TestEntity { Name = "Gamma" },
            },
            autoSave: true
        );

        var result = await _repository.GetListAsync(e => e.Name.StartsWith("B"));

        Assert.Single(result);
        Assert.Equal("Beta", result[0].Name);
    }

    [Fact]
    public async Task GetCountAsync_ReturnsCount()
    {
        await _repository.InsertManyAsync(
            new[]
            {
                new TestEntity { Name = "A" },
                new TestEntity { Name = "B" },
            },
            autoSave: true
        );

        var count = await _repository.GetCountAsync();
        Assert.Equal(2, count);

        var filtered = await _repository.GetCountAsync(e => e.Name == "A");
        Assert.Equal(1, filtered);
    }

    [Fact]
    public async Task GetPagedListAsync_ReturnsPage()
    {
        await _repository.InsertManyAsync(
            new[]
            {
                new TestEntity { Name = "A" },
                new TestEntity { Name = "B" },
                new TestEntity { Name = "C" },
            },
            autoSave: true
        );

        var page = await _repository.GetPagedListAsync(
            skipCount: 1,
            maxResultCount: 2,
            sorting: "Id"
        );

        Assert.Equal(2, page.Count);
    }

    [Fact]
    public async Task FindAsync_ReturnsMatchingEntity()
    {
        await _repository.InsertAsync(new TestEntity { Name = "Target" }, autoSave: true);

        var found = await _repository.FindAsync(e => e.Name == "Target");

        Assert.NotNull(found);
        Assert.Equal("Target", found!.Name);
    }

    [Fact]
    public async Task GetAsync_ThrowsWhenNotFound()
    {
        await Assert.ThrowsAsync<Ray.DDD.EntityNotFoundException>(async () =>
            await _repository.GetAsync(e => e.Name == "Missing")
        );
    }

    [Fact]
    public async Task UpdateAsync_UpdatesEntity()
    {
        var saved = await _repository.InsertAsync(new TestEntity { Name = "Old" }, autoSave: true);

        saved.Name = "New";
        await _repository.UpdateAsync(saved, autoSave: true);

        var updated = await _repository.FindByIdAsync(saved.Id);
        Assert.Equal("New", updated!.Name);
    }

    [Fact]
    public async Task UpdateManyAsync_UpdatesEntities()
    {
        await _repository.InsertManyAsync(
            new[]
            {
                new TestEntity { Name = "A" },
                new TestEntity { Name = "B" },
            },
            autoSave: true
        );

        var all = await _repository.GetAllListAsync();
        foreach (var entity in all)
        {
            entity.Name = "Updated";
        }

        await _repository.UpdateManyAsync(all, autoSave: true);

        var reloaded = await _repository.GetAllListAsync();
        Assert.All(reloaded, e => Assert.Equal("Updated", e.Name));
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesEntity()
    {
        var saved = await _repository.InsertAsync(
            new TestEntity { Name = "ToDelete" },
            autoSave: true
        );

        await _repository.DeleteAsync(saved, autoSave: true);

        var deleted = await _repository.FindByIdAsync(saved.Id);
        Assert.NotNull(deleted);
        Assert.True(deleted!.IsSoftDeleted);
    }

    [Fact]
    public async Task DeleteManyAsync_SoftDeletesEntities()
    {
        await _repository.InsertManyAsync(
            new[]
            {
                new TestEntity { Name = "A" },
                new TestEntity { Name = "B" },
            },
            autoSave: true
        );

        var all = await _repository.GetAllListAsync();
        await _repository.DeleteManyAsync(all, autoSave: true);

        foreach (var entity in all)
        {
            var deleted = await _repository.FindByIdAsync(entity.Id);
            Assert.True(deleted!.IsSoftDeleted);
        }
    }

    [Fact]
    public async Task DeleteByIdAsync_SoftDeletesEntity()
    {
        var saved = await _repository.InsertAsync(new TestEntity { Name = "ById" }, autoSave: true);

        await _repository.DeleteByIdAsync(saved.Id, autoSave: true);

        var deleted = await _repository.FindByIdAsync(saved.Id);
        Assert.NotNull(deleted);
        Assert.True(deleted!.IsSoftDeleted);
    }

    [Fact]
    public async Task DeleteManyByIdsAsync_SoftDeletesEntities()
    {
        var a = await _repository.InsertAsync(new TestEntity { Name = "A" }, autoSave: true);
        var b = await _repository.InsertAsync(new TestEntity { Name = "B" }, autoSave: true);
        var c = await _repository.InsertAsync(new TestEntity { Name = "C" }, autoSave: true);

        await _repository.DeleteManyByIdsAsync(new[] { a.Id, b.Id }, autoSave: true);

        var deletedA = await _repository.FindByIdAsync(a.Id);
        var deletedB = await _repository.FindByIdAsync(b.Id);
        var cEntity = await _repository.FindByIdAsync(c.Id);

        Assert.True(deletedA!.IsSoftDeleted);
        Assert.True(deletedB!.IsSoftDeleted);
        Assert.False(cEntity!.IsSoftDeleted);
    }

    [Fact]
    public async Task HardDeleteByIdAsync_RemovesEntityPermanently()
    {
        var saved = await _repository.InsertAsync(new TestEntity { Name = "Hard" }, autoSave: true);

        await _repository.HardDeleteByIdAsync(saved.Id, autoSave: true);

        Assert.Empty(await _repository.GetAllListAsync());
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsEntityOrThrows()
    {
        var saved = await _repository.InsertAsync(new TestEntity { Name = "ById" }, autoSave: true);

        var found = await _repository.GetByIdAsync(saved.Id);
        Assert.NotNull(found);

        await Assert.ThrowsAsync<Ray.DDD.EntityNotFoundException>(async () =>
            await _repository.GetByIdAsync(9999)
        );
    }

    [Fact]
    public async Task LongCountAsync_ReturnsCount()
    {
        await _repository.InsertManyAsync(
            new[]
            {
                new TestEntity { Name = "A" },
                new TestEntity { Name = "B" },
            },
            autoSave: true
        );

        var query = await _repository.GetQueryableAsync();
        var count = await _repository.LongCountAsync(query);
        Assert.Equal(2, count);
    }
}

// Test stub for BaseRepository<TEntity, TKey> which has no concrete production
// implementation in this codebase, but exposes valuable shared logic.
public class InMemoryRepository : BaseRepository<TestEntity, int>
{
    private readonly Dictionary<int, TestEntity> _store = new();
    private int _nextId = 1;

    public InMemoryRepository(IUnitOfWork unitOfWork)
        : base(unitOfWork) { }

    public override Task<IQueryable<TestEntity>> GetQueryableAsync() =>
        Task.FromResult(_store.Values.AsQueryable());

    public override Task<TestEntity?> FindAsync(
        System.Linq.Expressions.Expression<Func<TestEntity, bool>> predicate,
        CancellationToken cancellationToken = default
    ) => Task.FromResult(_store.Values.AsQueryable().FirstOrDefault(predicate));

    public override Task<TestEntity> InsertAsync(
        TestEntity entity,
        bool autoSave = false,
        CancellationToken cancellationToken = default
    )
    {
        entity.Id = _nextId++;
        _store[entity.Id] = entity;
        return Task.FromResult(entity);
    }

    public override Task<TestEntity> UpdateAsync(
        TestEntity entity,
        bool autoSave = false,
        CancellationToken cancellationToken = default
    )
    {
        _store[entity.Id] = entity;
        return Task.FromResult(entity);
    }

    public override Task DeleteAsync(
        TestEntity entity,
        bool autoSave = false,
        CancellationToken cancellationToken = default
    )
    {
        // Simulate hard vs soft delete: entities marked hard-deleted are removed,
        // otherwise soft-deleted (mirrors RayDbContext.ApplyConceptsForDeletedEntity).
        var hardDeleted =
            UnitOfWork.Items.TryGetValue(UnitOfWorkItemNames.HardDeletedEntities, out var obj)
            && obj is HashSet<Ray.DDD.IEntity> set
            && set.Contains(entity);

        if (hardDeleted)
        {
            _store.Remove(entity.Id);
        }
        else
        {
            entity.IsSoftDeleted = true;
        }

        return Task.CompletedTask;
    }

    public override Task<TestEntity?> FindByIdAsync(
        int id,
        CancellationToken cancellationToken = default
    ) => Task.FromResult(_store.TryGetValue(id, out var e) ? e : null);
}

public class FakeUnitOfWork : IUnitOfWork
{
    public Dictionary<string, object> Items { get; } = new();

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(0);

    public Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(true);
}

public class BaseRepositoryTKeyTests
{
    private readonly InMemoryRepository _repository = new(new FakeUnitOfWork());

    [Fact]
    public async Task GetByIdAsync_ReturnsEntity()
    {
        var saved = await _repository.InsertAsync(new TestEntity { Name = "A" });

        var found = await _repository.GetByIdAsync(saved.Id);

        Assert.Equal("A", found.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsWhenMissing()
    {
        await Assert.ThrowsAsync<Ray.DDD.EntityNotFoundException>(async () =>
            await _repository.GetByIdAsync(999)
        );
    }

    [Fact]
    public async Task DeleteByIdAsync_SoftDeletes()
    {
        var saved = await _repository.InsertAsync(new TestEntity { Name = "A" });

        await _repository.DeleteByIdAsync(saved.Id);

        var entity = await _repository.FindByIdAsync(saved.Id);
        Assert.True(entity!.IsSoftDeleted);
    }

    [Fact]
    public async Task DeleteManyByIdsAsync_SoftDeletes()
    {
        var a = await _repository.InsertAsync(new TestEntity { Name = "A" });
        var b = await _repository.InsertAsync(new TestEntity { Name = "B" });

        await _repository.DeleteManyByIdsAsync(new[] { a.Id, b.Id });

        Assert.True((await _repository.FindByIdAsync(a.Id))!.IsSoftDeleted);
        Assert.True((await _repository.FindByIdAsync(b.Id))!.IsSoftDeleted);
    }

    [Fact]
    public async Task HardDeleteByIdAsync_Removes()
    {
        var saved = await _repository.InsertAsync(new TestEntity { Name = "A" });

        await _repository.HardDeleteByIdAsync(saved.Id);

        Assert.Null(await _repository.FindByIdAsync(saved.Id));
    }

    [Fact]
    public async Task HardDeleteManyByIdsAsync_Removes()
    {
        var a = await _repository.InsertAsync(new TestEntity { Name = "A" });
        var b = await _repository.InsertAsync(new TestEntity { Name = "B" });

        await _repository.HardDeleteManyByIdsAsync(new[] { a.Id, b.Id });

        Assert.Null(await _repository.FindByIdAsync(a.Id));
        Assert.Null(await _repository.FindByIdAsync(b.Id));
    }
}
