# SqliteSample

## 新建Entity

实现`IEntity`接口

```
    public class Book:IEntity<long>
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string Title { get; set; }

        public string Author { get; set; }
    }
```

## 新建DbContext

继承`RayDbContext`，并注入`DbContextOptions<SqlServerDbContext>`

```
    public class SqlServerDbContext : RayDbContext<SqlServerDbContext>
    {
        public SqlServerDbContext(DbContextOptions<SqlServerDbContext> options) :base(options)
        {
        }

        public virtual DbSet<Book> Books { get; set; }
    }
```

## 注册依赖注入

```
            builder.Services.AddDefaultRepositories<SqlServerDbContext>(op =>
            {
                var conn = builder.Configuration["ConnectionStrings:Default"];
                op.UseSqlServer(conn);
            });
```

## 生成迁移文件

需要nuget安装Microsoft.EntityFrameworkCore.Tools

程序包管理器控制台执行：

```
Add-Migration
```

移除是：

```
Remove-Migration
```

## 刷库

程序包管理器控制台执行：

```
Update-Database
```

## 运行WebApi

Swagger验证增删改查。