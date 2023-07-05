using Microsoft.EntityFrameworkCore;
using Ray.Repository.EntityFramework;
using SqlServerSample.Entities;

namespace SqlServerSample
{
    public class SqlServerDbContext : RayDbContext<SqlServerDbContext>
    {
        public SqlServerDbContext(DbContextOptions<SqlServerDbContext> options) :base(options)
        {
        }

        public virtual DbSet<Book> Books { get; set; }
    }
}
