using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ray.DDD;

namespace Ray.Repository
{
    public interface IRepository
    {
    }

    public interface IRepository<TEntity> : IRepository
        where TEntity : class, IEntity
    {
        IUnitOfWork UnitOfWork { get; }
    }

    public interface IRepository<TEntity, TKey> : IRepository<TEntity>
        where TEntity : class, IEntity
    {
    }
}
