using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Ray.DDD;

namespace Ray.Repository
{
    public interface IBaseRepository<TEntity> : IRepository<TEntity>
        where TEntity : class, IEntity
    {
        #region Read
        Task<IQueryable<TEntity>> GetQueryableAsync();

        Task<IEnumerable<TEntity>> QueryableToListAsync(IQueryable<TEntity> query, CancellationToken cancellationToken = default);

        Task<long> LongCountAsync(IQueryable<TEntity> query, CancellationToken cancellationToken = default);

        Task<List<TEntity>> GetAllListAsync(CancellationToken cancellationToken = default);

        Task<List<TEntity>> GetListAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default);

        Task<long> GetCountAsync(Expression<Func<TEntity, bool>> predicate = null, CancellationToken cancellationToken = default);

        Task<List<TEntity>> GetPagedListAsync(
            int skipCount,
            int maxResultCount,
            string sorting,
            CancellationToken cancellationToken = default);

        Task<TEntity> FindAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default
        );

        Task<TEntity> GetAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default
        );
        #endregion

        #region Create
        Task<TEntity> InsertAsync(TEntity entity, bool autoSave = false, CancellationToken cancellationToken = default);

        Task InsertManyAsync(IEnumerable<TEntity> entities, bool autoSave = false, CancellationToken cancellationToken = default);
        #endregion

        #region Update 
        Task<TEntity> UpdateAsync(TEntity entity, bool autoSave = false, CancellationToken cancellationToken = default);

        Task UpdateManyAsync(IEnumerable<TEntity> entities, bool autoSave = false, CancellationToken cancellationToken = default);
        #endregion

        #region Delete
        Task DeleteAsync(TEntity entity, bool autoSave = false, CancellationToken cancellationToken = default);

        Task DeleteManyAsync(IEnumerable<TEntity> entities, bool autoSave = false, CancellationToken cancellationToken = default);

        Task DeleteAsync(
            Expression<Func<TEntity, bool>> predicate,
            bool autoSave = false,
            CancellationToken cancellationToken = default
        );
        #endregion

        #region Hard Delete
        Task HardDeleteAsync(TEntity entity, bool autoSave = false, CancellationToken cancellationToken = default);

        Task HardDeleteManyAsync(IEnumerable<TEntity> entities, bool autoSave = false, CancellationToken cancellationToken = default);

        Task HardDeleteAsync(
            Expression<Func<TEntity, bool>> predicate,
            bool autoSave = false,
            CancellationToken cancellationToken = default
        );
        #endregion
    }

    public interface IBaseRepository<TEntity, TKey> : IBaseRepository<TEntity>
        where TEntity : class, IEntity<TKey>
    {
        #region Read
        Task<TEntity> FindByIdAsync(TKey id, CancellationToken cancellationToken = default);//return exception if null

        Task<TEntity> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);//throw exception if null
        #endregion

        #region Delete
        Task DeleteByIdAsync(TKey id, bool autoSave = false, CancellationToken cancellationToken = default);

        Task DeleteManyByIdsAsync(IEnumerable<TKey> ids, bool autoSave = false, CancellationToken cancellationToken = default);
        #endregion

        #region Hard Delete
        Task HardDeleteByIdAsync(TKey id, bool autoSave = false, CancellationToken cancellationToken = default);

        Task HardDeleteManyByIdsAsync(IEnumerable<TKey> ids, bool autoSave = false, CancellationToken cancellationToken = default);
        #endregion
    }
}
