using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Ray.DDD;

namespace Ray.Repository.EntityFramework
{
    public class EFBaseRepository<TDbContext, TEntity> : BaseRepository<TEntity>, IEFBaseRepository<TEntity>
        where TDbContext : DbContext
        where TEntity : class, IEntity
    {
        private readonly TDbContext _dbContext;

        public EFBaseRepository(TDbContext dbContext, IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _dbContext = dbContext;
        }

        async Task<DbContext> IEFBaseRepository<TEntity>.GetDbContextAsync()
        {
            return await GetDbContextAsync() as DbContext;
        }

        public async Task<DbSet<TEntity>> GetDbSetAsync()
        {
            return (await GetDbContextAsync()).Set<TEntity>();
        }

        protected virtual Task<TDbContext> GetDbContextAsync()
        {
            return Task.FromResult(_dbContext);
        }

        public override async Task<IQueryable<TEntity>> GetQueryableAsync()
        {
            return (await GetDbSetAsync()).AsQueryable();
        }

        public override async Task<IEnumerable<TEntity>> QueryableToListAsync(IQueryable<TEntity> query, CancellationToken cancellationToken = default)
        {
            return await query.ToListAsync(cancellationToken);
        }

        public override async Task<long> LongCountAsync(IQueryable<TEntity> query, CancellationToken cancellationToken = default)
        {
            return await query.LongCountAsync(cancellationToken);
        }

        public override async Task<TEntity> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await (await GetQueryableAsync())
                    .Where(predicate)
                    .SingleOrDefaultAsync(cancellationToken);
        }

        public override async Task<TEntity> InsertAsync(TEntity entity, bool autoSave = false, CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();

            var savedEntity = (await dbContext.Set<TEntity>().AddAsync(entity, cancellationToken)).Entity;

            if (autoSave)
            {
                await SaveChangesAsync(cancellationToken);
            }

            return savedEntity;
        }

        public override async Task InsertManyAsync(IEnumerable<TEntity> entities, bool autoSave = false, CancellationToken cancellationToken = default)
        {
            var entityArray = entities.ToArray();
            var dbContext = await GetDbContextAsync();

            await dbContext.Set<TEntity>().AddRangeAsync(entityArray, cancellationToken);

            if (autoSave)
            {
                await SaveChangesAsync(cancellationToken);
            }
        }

        public override async Task<TEntity> UpdateAsync(TEntity entity, bool autoSave = false, CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();

            dbContext.Attach(entity);

            var updatedEntity = dbContext.Update(entity).Entity;

            if (autoSave)
            {
                await SaveChangesAsync(cancellationToken);
            }

            return updatedEntity;
        }

        public override async Task UpdateManyAsync(IEnumerable<TEntity> entities, bool autoSave = false, CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();

            dbContext.Set<TEntity>().UpdateRange(entities);

            if (autoSave)
            {
                await SaveChangesAsync(cancellationToken);
            }
        }

        public override async Task DeleteAsync(TEntity entity, bool autoSave = false, CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();

            dbContext.Set<TEntity>().Remove(entity);

            if (autoSave)
            {
                await SaveChangesAsync(cancellationToken);
            }
        }

        public override async Task DeleteAsync(Expression<Func<TEntity, bool>> predicate, bool autoSave = false, CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();
            var dbSet = dbContext.Set<TEntity>();

            var entities = await dbSet
                .Where(predicate)
                .ToListAsync(cancellationToken);

            await DeleteManyAsync(entities, autoSave, cancellationToken);

            if (autoSave)
            {
                await SaveChangesAsync(cancellationToken);
            }
        }

        public override async Task DeleteManyAsync(IEnumerable<TEntity> entities, bool autoSave = false, CancellationToken cancellationToken = default)
        {
            var dbContext = await GetDbContextAsync();

            dbContext.RemoveRange(entities);

            if (autoSave)
            {
                await SaveChangesAsync(cancellationToken);
            }
        }
    }

    public class EFBaseRepository<TDbContext, TEntity, TKey> : EFBaseRepository<TDbContext, TEntity>, IEFBaseRepository<TEntity, TKey>
    where TDbContext : DbContext
    where TEntity : class, IEntity<TKey>
    {
        private readonly IDataFilter<ISoftDelete> _softDeleteFilter;

        public EFBaseRepository(TDbContext dbContext, IUnitOfWork unitOfWork)
            : base(dbContext, unitOfWork)
        {
        }

        public EFBaseRepository(TDbContext dbContext, IUnitOfWork unitOfWork, IDataFilter<ISoftDelete> softDeleteFilter)
            : base(dbContext, unitOfWork)
        {
            _softDeleteFilter = softDeleteFilter;
        }

        public async Task<TEntity> FindByIdAsync(TKey id, CancellationToken cancellationToken = default)
        {
            return await (await GetQueryableAsync())
                .OrderBy(e => e.Id)
                .FirstOrDefaultAsync(e => e.Id.Equals(id), cancellationToken);
        }

        public virtual async Task<TEntity> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
        {
            var entity = await FindByIdAsync(id, cancellationToken);

            if (entity == null)
            {
                throw new EntityNotFoundException($"EntityNotFound: {typeof(TEntity)}({id})");
            }

            return entity;
        }

        public virtual async Task DeleteByIdAsync(TKey id, bool autoSave = false, CancellationToken cancellationToken = default)
        {
            var entity = await FindByIdAsync(id, cancellationToken: cancellationToken);
            if (entity == null)
            {
                return;
            }

            await DeleteAsync(entity, autoSave, cancellationToken);
        }

        public virtual async Task DeleteManyByIdsAsync(IEnumerable<TKey> ids, bool autoSave = false, CancellationToken cancellationToken = default)
        {
            var query = (await GetQueryableAsync()).Where(x => ids.Contains(x.Id));
            var entities = await QueryableToListAsync(query, cancellationToken);

            await DeleteManyAsync(entities, autoSave, cancellationToken);
        }


        public virtual async Task HardDeleteByIdAsync(TKey id, bool autoSave = false, CancellationToken cancellationToken = default)
        {
            using (_softDeleteFilter?.Disable())
            {
                var entity = await FindByIdAsync(id, cancellationToken: cancellationToken);
                if (entity == null)
                {
                    return;
                }

                var hardDeleteEntities = (HashSet<IEntity>)UnitOfWork.Items.GetOrAdd(
                    UnitOfWorkItemNames.HardDeletedEntities,
                    () => new HashSet<IEntity>()
                );

                hardDeleteEntities.Add(entity);
                await DeleteAsync(entity, autoSave, cancellationToken);
            }
        }

        public virtual async Task HardDeleteManyByIdsAsync(IEnumerable<TKey> ids, bool autoSave = false, CancellationToken cancellationToken = default)
        {
            using (_softDeleteFilter?.Disable())
            {
                var entities = await (await GetQueryableAsync()).Where(x => ids.Contains(x.Id)).ToListAsync(cancellationToken);

                await HardDeleteManyAsync(entities, autoSave, cancellationToken);
            }
        }
    }


}