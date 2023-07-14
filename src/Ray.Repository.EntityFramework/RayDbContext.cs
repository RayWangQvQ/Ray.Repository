using MediatR;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Ray.DDD;

namespace Ray.Repository.EntityFramework
{
    public abstract class RayDbContext<TServiceDbContext> : DbContext, IUnitOfWork
        where TServiceDbContext : DbContext
    {
        private readonly IMediator _mediator;

        private readonly IDataFilter<ISoftDelete> _softDeleteFilter;

        public RayDbContext()
        {
            Initialize();
        }

        public RayDbContext(DbContextOptions<TServiceDbContext> options)
            : base(options)
        {
            Initialize();
        }

        public RayDbContext(DbContextOptions<TServiceDbContext> options, IMediator mediator) : base(options)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            Initialize();
        }

        public RayDbContext(DbContextOptions<TServiceDbContext> options, IMediator mediator, IDataFilter<ISoftDelete> softDeleteFilter) : base(options)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _softDeleteFilter = softDeleteFilter;
            Initialize();
        }

        private void Initialize()
        {
            ChangeTracker.Tracked += ChangeTracker_Tracked;
            ChangeTracker.StateChanged += ChangeTracker_StateChanged;
        }

        protected virtual bool IsSoftDeleteFilterEnabled => _softDeleteFilter?.IsEnabled ?? false;
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                ConfigureBasePropertiesMethodInfo
                    .MakeGenericMethod(entityType.ClrType)
                    .Invoke(this, new object[] { modelBuilder, entityType });
            }
        }

        private static readonly MethodInfo ConfigureBasePropertiesMethodInfo
            = typeof(RayDbContext<TServiceDbContext>)
                .GetMethod(
                    nameof(ConfigureBaseProperties),
                    BindingFlags.Instance | BindingFlags.NonPublic
                );

        protected virtual void ConfigureBaseProperties<TEntity>(ModelBuilder modelBuilder, IMutableEntityType mutableEntityType)
            where TEntity : class
        {
            if (mutableEntityType.IsOwned())
            {
                return;
            }

            if (!typeof(IEntity).IsAssignableFrom(typeof(TEntity)))
            {
                return;
            }

            TryConfigureSoftDeleteProperty(modelBuilder.Entity<TEntity>());

            TryConfigureGlobalFilters<TEntity>(modelBuilder, mutableEntityType);
        }

        protected virtual void TryConfigureSoftDeleteProperty(EntityTypeBuilder b)
        {
            if (typeof(ISoftDelete).IsAssignableFrom(b.Metadata.ClrType))
            {
                b.Property(nameof(ISoftDelete.IsSoftDeleted))
                    .IsRequired()
                    .HasDefaultValue(false)
                    .HasColumnName(nameof(ISoftDelete.IsSoftDeleted))
                    ;
            }
        }

        protected virtual void TryConfigureGlobalFilters<TEntity>(ModelBuilder modelBuilder, IMutableEntityType mutableEntityType) where TEntity : class
        {
            if (mutableEntityType.BaseType == null && typeof(ISoftDelete).IsAssignableFrom(typeof(TEntity)))
            {
                Expression<Func<TEntity, bool>> filterExpression = null;

                if (typeof(ISoftDelete).IsAssignableFrom(typeof(TEntity)))
                {
                    filterExpression = e => !IsSoftDeleteFilterEnabled || !EF.Property<bool>(e, nameof(ISoftDelete.IsSoftDeleted));
                }

                if (filterExpression != null)
                {
                    modelBuilder.Entity<TEntity>().HasQueryFilter(filterExpression);
                }
            }
        }

        public Dictionary<string, object> Items { get; private set; } = new Dictionary<string, object>();

        public virtual async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
        {
            var result = await SaveChangesAsync(cancellationToken);
            if (_mediator != null) await this.DispatchDomainEventsAsync(_mediator);
            return true;
        }

        protected virtual void ChangeTracker_Tracked(object sender, EntityTrackedEventArgs e)
        {
            ApplyForTrackedEntity(e.Entry);
        }

        protected virtual void ChangeTracker_StateChanged(object sender, EntityStateChangedEventArgs e)
        {
            ApplyForTrackedEntity(e.Entry);
        }

        private void ApplyForTrackedEntity(EntityEntry entry)
        {
            switch (entry.State)
            {
                case EntityState.Deleted:
                    ApplyConceptsForDeletedEntity(entry);
                    break;
            }
        }

        protected virtual void ApplyConceptsForDeletedEntity(EntityEntry entry)
        {
            if (!(entry.Entity is ISoftDelete)) return;

            if (IsHardDeleted(entry)) return;

            entry.Reload();
            ((ISoftDelete)entry.Entity).IsSoftDeleted = true;
        }

        protected virtual bool IsHardDeleted(EntityEntry entry)
        {
            var hardDeletedEntityItemValue = this.Items.TryGetValue(UnitOfWorkItemNames.HardDeletedEntities, out object obj)
                ? obj
                : default;
            var hardDeletedEntities = hardDeletedEntityItemValue as HashSet<IEntity>;
            if (hardDeletedEntities == null) return false;

            return hardDeletedEntities.Contains(entry.Entity);
        }

        public virtual async Task DispatchDomainEventsAsync(IMediator mediator)
        {
            var domainEntities = this.ChangeTracker
                .Entries<Entity>()
                .Where(x => x.Entity.DomainEvents != null && x.Entity.DomainEvents.Any())
                .ToList();

            var domainEvents = domainEntities
                .SelectMany(x => x.Entity.DomainEvents)
                .ToList();

            domainEntities.ForEach(entity => entity.Entity.ClearDomainEvents());

            foreach (var domainEvent in domainEvents)
                await mediator.Publish(domainEvent);
        }
    }
}
