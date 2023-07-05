using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ray.DDD;

namespace Ray.Repository.EntityFramework
{
    public interface IEFBaseRepository<TEntity> : IBaseRepository<TEntity>
        where TEntity : class, IEntity
    {
        Task<DbContext> GetDbContextAsync();

        Task<DbSet<TEntity>> GetDbSetAsync();
    }

    public interface IEFBaseRepository<TEntity, TKey> : IEFBaseRepository<TEntity>, IBaseRepository<TEntity, TKey>
        where TEntity : class, IEntity<TKey>
    {

    }
}
