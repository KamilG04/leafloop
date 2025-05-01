using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using LeafLoop.Repositories.Interfaces;

namespace LeafLoop.Repositories.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        // Repository properties for each entity
        IUserRepository Users { get; }
        IItemRepository Items { get; }
        ICategoryRepository Categories { get; }
        ITagRepository Tags { get; }
        ITransactionRepository Transactions { get; }
        IAddressRepository Addresses { get; }
        IPhotoRepository Photos { get; }
        ICompanyRepository Companies { get; }
        IEventRepository Events { get; }
        IBadgeRepository Badges { get; }
        IRatingRepository Ratings { get; }
        IMessageRepository Messages { get; }
        INotificationRepository Notifications { get; }
        IReportRepository Reports { get; }
        ICommentRepository Comments { get; }
        ISubscriptionRepository Subscriptions { get; }
        ISavedSearchRepository SavedSearches { get; }
        
        // Generic entity methods (for entities without specific repositories)
        Task<T> GetEntityByIdAsync<T>(int id) where T : class;
        Task<IEnumerable<T>> GetAllEntitiesAsync<T>() where T : class;
        Task<IEnumerable<T>> FindEntitiesAsync<T>(Expression<Func<T, bool>> predicate) where T : class;
        Task<T> SingleOrDefaultEntityAsync<T>(Expression<Func<T, bool>> predicate) where T : class;
        Task AddEntityAsync<T>(T entity) where T : class;
        Task AddRangeEntitiesAsync<T>(IEnumerable<T> entities) where T : class;
        void UpdateEntity<T>(T entity) where T : class;
        void UpdateRangeEntities<T>(IEnumerable<T> entities) where T : class;
        void RemoveEntity<T>(T entity) where T : class;
        void RemoveRangeEntities<T>(IEnumerable<T> entities) where T : class;
        
        // Save changes to database
        Task<int> CompleteAsync();
        
        // Transaction management methods
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        
        // Execute with transaction
        Task ExecuteInTransactionAsync(Func<Task> action);
        Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action);
    }
}