using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ray.DDD;

namespace Ray.Repository.EntityFramework
{
    public static class RepositoryModule
    {
        public static void AddDefaultRepositories<TDbContext>(this IServiceCollection services, Action<DbContextOptionsBuilder> optionsAction)
            where TDbContext : RayDbContext<TDbContext>
        {
            // MediatR
            services.AddMediatR(c =>
            {
                c.RegisterServicesFromAssembly(typeof(TDbContext).Assembly);
            });

            // DateFilter
            services.AddDataFilter();

            // DbContext
            services.AddDbContext<TDbContext>(optionsBuilder =>
            {
#if DEBUG
                optionsBuilder.LogTo(Console.WriteLine);
                optionsBuilder.EnableSensitiveDataLogging();
#endif

                optionsAction?.Invoke(optionsBuilder);
            });

            // UnitOfWork
            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<TDbContext>());

            var entityTypes = GetEntityTypesFromDbContext(typeof(TDbContext));
            foreach (var entityType in entityTypes)
            {
                services.AddDefaultRepository(
                    entityType,
                    GetDefaultRepositoryImplementationType(typeof(TDbContext), entityType)
                );
            }
        }

        public static void AddIdentityDefaultRepositories<TDbContext>(this IServiceCollection services, Action<DbContextOptionsBuilder> optionsAction)
            where TDbContext : RayIdentityDbContext<TDbContext>
        {
            // MediatR
            services.AddMediatR(c =>
            {
                c.RegisterServicesFromAssembly(typeof(TDbContext).Assembly);
            });

            // DateFilter
            services.AddDataFilter();

            // DbContext
            services.AddDbContext<TDbContext>(optionsBuilder =>
            {
#if DEBUG
                optionsBuilder.LogTo(Console.WriteLine);
                optionsBuilder.EnableSensitiveDataLogging();
#endif

                optionsAction?.Invoke(optionsBuilder);
            });

            // UnitOfWork
            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<TDbContext>());

            var entityTypes = GetEntityTypesFromDbContext(typeof(TDbContext));
            foreach (var entityType in entityTypes)
            {
                services.AddDefaultRepository(
                    entityType,
                    GetDefaultRepositoryImplementationType(typeof(TDbContext), entityType)
                );
            }
        }

        public static IServiceCollection AddDefaultRepository(this IServiceCollection services,
            Type entityType,
            Type repositoryImplementationType)
        {
            //IBasicRepository<TEntity>
            var basicRepositoryInterface = typeof(IBaseRepository<>).MakeGenericType(entityType);
            if (basicRepositoryInterface.IsAssignableFrom(repositoryImplementationType))
            {
                services.TryAddTransient(basicRepositoryInterface, repositoryImplementationType);

                //IRepository<TEntity>
                var repositoryInterface = typeof(IRepository<>).MakeGenericType(entityType);
                if (repositoryInterface.IsAssignableFrom(repositoryImplementationType))
                {
                    services.TryAddTransient(repositoryInterface, repositoryImplementationType);
                }
            }

            var primaryKeyType = GetPrimaryKeyType(entityType);
            if (primaryKeyType != null)
            {
                //IBasicRepository<TEntity, TKey>
                var basicRepositoryInterfaceWithPk = typeof(IBaseRepository<,>).MakeGenericType(entityType, primaryKeyType);
                if (basicRepositoryInterfaceWithPk.IsAssignableFrom(repositoryImplementationType))
                {
                    services.TryAddTransient(basicRepositoryInterfaceWithPk, repositoryImplementationType);

                    //IRepository<TEntity, TKey>
                    var repositoryInterfaceWithPk = typeof(IRepository<,>).MakeGenericType(entityType, primaryKeyType);
                    if (repositoryInterfaceWithPk.IsAssignableFrom(repositoryImplementationType))
                    {
                        services.TryAddTransient(repositoryInterfaceWithPk, repositoryImplementationType);
                    }
                }
            }

            return services;
        }

        private static IEnumerable<Type> GetEntityTypesFromDbContext(Type dbContextType)
        {
            //取dbContext里所有DbSet，然后过滤出所有实现IEntity的实体
            var dbContextDbSetProperties = dbContextType.GetTypeInfo().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(pi => IsAssignableToGenericType(pi.PropertyType, typeof(DbSet<>)));
            foreach (var property in dbContextDbSetProperties)
            {
                if (typeof(IEntity).IsAssignableFrom(property.PropertyType.GenericTypeArguments[0]))
                    yield return property.PropertyType.GenericTypeArguments[0];
            }
        }

        private static bool IsAssignableToGenericType(Type givenType, Type genericType)
        {
            var givenTypeInfo = givenType.GetTypeInfo();

            if (givenTypeInfo.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
            {
                return true;
            }

            foreach (var interfaceType in givenTypeInfo.GetInterfaces())
            {
                if (interfaceType.GetTypeInfo().IsGenericType && interfaceType.GetGenericTypeDefinition() == genericType)
                {
                    return true;
                }
            }

            if (givenTypeInfo.BaseType == null)
            {
                return false;
            }

            return IsAssignableToGenericType(givenTypeInfo.BaseType, genericType);
        }

        private static Type GetDefaultRepositoryImplementationType(Type originalDbContextType, Type entityType)
        {
            var primaryKeyType = GetPrimaryKeyType(entityType);

            if (primaryKeyType == null)
            {
                return typeof(EFBaseRepository<,>).MakeGenericType(originalDbContextType, entityType);
            }

            return typeof(EFBaseRepository<,,>).MakeGenericType(originalDbContextType, entityType, primaryKeyType);
        }

        private static Type GetPrimaryKeyType(Type entityType)
        {
            if (!typeof(IEntity).IsAssignableFrom(entityType))
            {
                throw new Exception($"Given {nameof(entityType)} is not an entity. It should implement {typeof(IEntity).AssemblyQualifiedName}!");
            }

            foreach (var interfaceType in entityType.GetTypeInfo().GetInterfaces())
            {
                if (interfaceType.GetTypeInfo().IsGenericType &&
                    interfaceType.GetGenericTypeDefinition() == typeof(IEntity<>))
                {
                    return interfaceType.GenericTypeArguments[0];
                }
            }

            return null;
        }
    }
}
