using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.Repository
{
    public interface IUnitOfWork
    {
        Dictionary<string, object> Items { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken));

        Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default(CancellationToken));
    }

    public static class UnitOfWorkItemNames
    {
        public const string HardDeletedEntities = "HardDeletedEntities";
    }
}
