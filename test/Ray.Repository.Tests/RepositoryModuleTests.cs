using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ray.Repository.EntityFramework;

namespace Ray.Repository.Tests;

public class TestEntity : Ray.DDD.Entity<int>
{
    public string Name { get; set; } = string.Empty;
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
