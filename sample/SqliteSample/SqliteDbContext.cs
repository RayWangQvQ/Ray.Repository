using Microsoft.EntityFrameworkCore;
using Ray.Repository.EntityFramework;
using SqliteSample.Entities;

namespace SqliteSample
{
    public class SqliteDbContext : RayDbContext<SqliteDbContext>
    {
        public SqliteDbContext(DbContextOptions<SqliteDbContext> options) :base(options)
        {
        }

        public virtual DbSet<Book> Books { get; set; }
    }
}
