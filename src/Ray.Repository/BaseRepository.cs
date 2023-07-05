using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Ray.DDD;
using System.Linq.Dynamic.Core;

namespace Ray.Repository
{
    public abstract class BaseRepository<TEntity> : IBaseRepository<TEntity>
            where TEntity : class, IEntity
    {
        protected BaseRepository(IUnitOfWork unitOfWork)
        {
            UnitOfWork = unitOfWork;
        }

        public IUnitOfWork UnitOfWork { get; }

        public abstract Task<IQueryable<TEntity>> GetQueryableAsync();

        public virtual async Task<IEnumerable<TEntity>> QueryableToListAsync(IQueryable<TEntity> query,
            CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(query.ToList());
        }

        public virtual async Task<long> LongCountAsync(IQueryable<TEntity> query, CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(query.LongCount());
        }

        public virtual async Task<List<TEntity>> GetAllListAsync(CancellationToken cancellationToken = default)
        {
            var query = await GetQueryableAsync();
            return (await QueryableToListAsync(query, cancellationToken))
                .ToList();
        }

        public virtual async Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            var query = (await GetQueryableAsync()).Where(predicate);
            return (await QueryableToListAsync(query, cancellationToken))
                .ToList();
        }

        public virtual async Task<long> GetCountAsync(Expression<Func<TEntity, bool>> predicate = null, CancellationToken cancellationToken = default)
        {
            var query = await GetQueryableAsync();
            if (predicate != null) query = query.Where(predicate);

            return await LongCountAsync(query, cancellationToken);
        }

        public virtual async Task<List<TEntity>> GetPagedListAsync(int skipCount, int maxResultCount, string sorting,
            CancellationToken cancellationToken = default)
        {
            var queryable = (await GetQueryableAsync())
                .OrderBy(sorting)
                .Skip(skipCount)
                .Take(maxResultCount);

            return (await QueryableToListAsync(queryable, cancellationToken))
                .ToList();
        }

        public abstract Task<TEntity> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        public virtual async Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            var entity = await FindAsync(predicate, cancellationToken);

            if (entity == null)
            {
                throw new EntityNotFoundException($"EntityNotFound: {typeof(TEntity)}");
            }

            return entity;
        }

        public abstract Task<TEntity> InsertAsync(TEntity entity, bool autoSave = false, CancellationToken cancellationToken = default);

        public virtual async Task InsertManyAsync(IEnumerable<TEntity> entities, bool autoSave = false, CancellationToken cancellationToken = default)
        {
            foreach (var entity in entities)
            {
                await InsertAsync(entity, cancellationToken: cancellationToken);
            }

            if (autoSave)
            {
                await SaveChangesAsync(cancellationToken);
            }
        }

        public abstract Task<TEntity> UpdateAsync(TEntity entity, bool autoSave = false, CancellationToken cancellationToken = default);

        public virtual async Task UpdateManyAsync(IEnumerable<TEntity> entities, bool autoSave = false, CancellationToken cancellationToken = default)
        {
            foreach (var entity in entities)
            {
                await UpdateAsync(entity, cancellationToken: cancellationToken);
            }

            if (autoSave)
            {
                await SaveChangesAsync(cancellationToken);
            }
        }

        public abstract Task DeleteAsync(TEntity entity, bool autoSave = false, CancellationToken cancellationToken = default);

        public virtual async Task DeleteManyAsync(IEnumerable<TEntity> entities, bool autoSave = false, CancellationToken cancellationToken = default)
        {
            foreach (var entity in entities)
            {
                await DeleteAsync(entity, cancellationToken: cancellationToken);
            }

            if (autoSave)
            {
                await SaveChangesAsync(cancellationToken);
            }
        }

        public virtual async Task DeleteAsync(Expression<Func<TEntity, bool>> predicate, bool autoSave = false,
            CancellationToken cancellationToken = default)
        {
            var entities = (await GetQueryableAsync()).Where(predicate);

            await DeleteManyAsync(entities, autoSave, cancellationToken);

            if (autoSave)
            {
                await SaveChangesAsync(cancellationToken);
            }
        }

        public virtual async Task HardDeleteAsync(TEntity entity, bool autoSave = false, CancellationToken cancellationToken = default)
        {
            var hardDeleteEntities = (HashSet<IEntity>)UnitOfWork.Items.GetOrAdd(
                UnitOfWorkItemNames.HardDeletedEntities,
                () => new HashSet<IEntity>()
            );

            hardDeleteEntities.Add(entity);
            await DeleteAsync(entity, autoSave, cancellationToken);
        }

        public virtual async Task HardDeleteManyAsync(IEnumerable<TEntity> entities, bool autoSave = false, CancellationToken cancellationToken = default)
        {
            var hardDeleteEntities = (HashSet<IEntity>)UnitOfWork.Items.GetOrAdd(
                UnitOfWorkItemNames.HardDeletedEntities,
                () => new HashSet<IEntity>()
            );

            hardDeleteEntities.UnionWith(entities);
            await DeleteManyAsync(entities, autoSave, cancellationToken);
        }

        public virtual async Task HardDeleteAsync(Expression<Func<TEntity, bool>> predicate, bool autoSave = false, CancellationToken cancellationToken = default)
        {
            var query = (await GetQueryableAsync()).Where(predicate);
            var entities = await QueryableToListAsync(query, cancellationToken);
            await HardDeleteManyAsync(entities, autoSave, cancellationToken);
        }

        protected virtual async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await UnitOfWork.SaveEntitiesAsync(cancellationToken);
        }
    }

    public abstract class BaseRepository<TEntity, TKey> : BaseRepository<TEntity>, IBaseRepository<TEntity, TKey>
        where TEntity : class, IEntity<TKey>
    {
        private readonly IDataFilter<ISoftDelete> _softDeleteFilter;

        protected BaseRepository(IUnitOfWork unitOfWork) : base(unitOfWork)
        {

        }

        protected BaseRepository(IUnitOfWork unitOfWork, IDataFilter<ISoftDelete> softDeleteFilter) : base(unitOfWork)
        {
            _softDeleteFilter = softDeleteFilter;
        }

        public abstract Task<TEntity> FindByIdAsync(TKey id, CancellationToken cancellationToken = default);

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
                var query = (await GetQueryableAsync()).Where(x => ids.Contains(x.Id));
                var entities = await QueryableToListAsync(query, cancellationToken);

                await HardDeleteManyAsync(entities, autoSave, cancellationToken);
            }
        }
    }
}
