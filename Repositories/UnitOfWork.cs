using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using LeafLoop.Data; // Upewnij się, że LeafLoopDbContext jest tutaj
using LeafLoop.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace LeafLoop.Repositories // Zwykle UnitOfWork jest w tym samym namespace co Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly LeafLoopDbContext _context;
        private IDbContextTransaction _transaction;

        // Repositories
        private IUserRepository _userRepository;
        private IItemRepository _itemRepository;
        private ICategoryRepository _categoryRepository;
        private ITagRepository _tagRepository;
        private ITransactionRepository _transactionRepository;
        private IAddressRepository _addressRepository;
        private IPhotoRepository _photoRepository;
        private ICompanyRepository _companyRepository;
        private IEventRepository _eventRepository;
        private IBadgeRepository _badgeRepository;
        private IRatingRepository _ratingRepository;
        private IMessageRepository _messageRepository;
        private INotificationRepository _notificationRepository;
        private IReportRepository _reportRepository;
        private ICommentRepository _commentRepository;
        private ISubscriptionRepository _subscriptionRepository;
        private ISavedSearchRepository _savedSearchRepository;
        private IAdminRepository _adminRepository; // Zadeklarowane wcześniej

        // Dodaj repozytorium dla EventParticipant, jeśli operacje na nim są częste i złożone
        // private IRepository<EventParticipant> _eventParticipantRepository;


        public UnitOfWork(LeafLoopDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // Właściwość do uzyskania dostępu do kontekstu, jeśli potrzebne w serwisach (używaj ostrożnie)
        public LeafLoopDbContext Context => _context;


        // Repository properties with lazy loading
        public IUserRepository Users => _userRepository ??= new UserRepository(_context);
        public IItemRepository Items => _itemRepository ??= new ItemRepository(_context);
        public ICategoryRepository Categories => _categoryRepository ??= new CategoryRepository(_context);
        public ITagRepository Tags => _tagRepository ??= new TagRepository(_context);
        public ITransactionRepository Transactions => _transactionRepository ??= new TransactionRepository(_context);
        public IAddressRepository Addresses => _addressRepository ??= new AddressRepository(_context);
        public IPhotoRepository Photos => _photoRepository ??= new PhotoRepository(_context);
        public ICompanyRepository Companies => _companyRepository ??= new CompanyRepository(_context);
        public IEventRepository Events => _eventRepository ??= new EventRepository(_context);
        public IBadgeRepository Badges => _badgeRepository ??= new BadgeRepository(_context);
        public IRatingRepository Ratings => _ratingRepository ??= new RatingRepository(_context);
        public IMessageRepository Messages => _messageRepository ??= new MessageRepository(_context);

        public INotificationRepository Notifications =>
            _notificationRepository ??= new NotificationRepository(_context);

        public IReportRepository Reports => _reportRepository ??= new ReportRepository(_context);
        public ICommentRepository Comments => _commentRepository ??= new CommentRepository(_context);

        public ISubscriptionRepository Subscriptions =>
            _subscriptionRepository ??= new SubscriptionRepository(_context);

        public ISavedSearchRepository SavedSearches => _savedSearchRepository ??= new SavedSearchRepository(_context);
        public IAdminRepository AdminLogs => _adminRepository ??= new AdminRepository(_context);

        // Przykład generycznego repozytorium dla EventParticipant, jeśli nie masz dedykowanego
        // public IRepository<EventParticipant> EventParticipants => 
        //    _eventParticipantRepository ??= new Repository<EventParticipant>(_context);


        // Generic entity methods implementation (bez zmian)
        public async Task<T> GetEntityByIdAsync<T>(int id) where T : class
        {
            return await _context.Set<T>().FindAsync(id);
        }

        public async Task<IEnumerable<T>> GetAllEntitiesAsync<T>() where T : class
        {
            return await _context.Set<T>().ToListAsync();
        }

        public async Task<IEnumerable<T>> FindEntitiesAsync<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            return await _context.Set<T>().Where(predicate).ToListAsync();
        }

        public async Task<T> SingleOrDefaultEntityAsync<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            return await _context.Set<T>().SingleOrDefaultAsync(predicate);
        }

        public async Task AddEntityAsync<T>(T entity) where T : class
        {
            await _context.Set<T>().AddAsync(entity);
        }

        public async Task AddRangeEntitiesAsync<T>(IEnumerable<T> entities) where T : class
        {
            await _context.Set<T>().AddRangeAsync(entities);
        }

        public void UpdateEntity<T>(T entity) where T : class
        {
            _context.Set<T>()
                .Attach(entity); // Lub Add, jeśli nie jesteś pewien czy jest śledzona i chcesz uniknąć błędu
            _context.Entry(entity).State = EntityState.Modified;
        }

        public void UpdateRangeEntities<T>(IEnumerable<T> entities) where T : class
        {
            foreach (var entity in entities)
            {
                _context.Set<T>().Attach(entity);
                _context.Entry(entity).State = EntityState.Modified;
            }
        }

        public void RemoveEntity<T>(T entity) where T : class
        {
            _context.Set<T>().Remove(entity);
        }

        public void RemoveRangeEntities<T>(IEnumerable<T> entities) where T : class
        {
            _context.Set<T>().RemoveRange(entities);
        }

        // Basic save changes
        public async Task<int> CompleteAsync()
        {
            // Tutaj można dodać logowanie, jeśli jest potrzebne
            // _logger.LogInformation("UnitOfWork.CompleteAsync called. Saving changes...");
            return await _context.SaveChangesAsync();
        }

        // Transaction management methods (bez zmian - są poprawne)
        public async Task BeginTransactionAsync()
        {
            if (_transaction != null)
            {
                // Może warto zalogować ostrzeżenie lub rzucić wyjątek, jeśli transakcja już istnieje
                return;
            }

            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction == null)
            {
                // Może warto zalogować ostrzeżenie lub rzucić wyjątek
                throw new InvalidOperationException("Transaction has not been started.");
            }

            try
            {
                await _context.SaveChangesAsync(); // Upewnij się, że zmiany są zapisane PRZED commitem transakcji
                await _transaction.CommitAsync();
            }
            catch
            {
                await RollbackTransactionAsync(); // Rollback w przypadku błędu SaveChanges lub Commit
                throw;
            }
            finally
            {
                await DisposeTransactionAsync();
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction == null)
            {
                // Może warto zalogować ostrzeżenie
                return;
            }

            try
            {
                await _transaction.RollbackAsync();
            }
            finally
            {
                await DisposeTransactionAsync();
            }
        }

        private async Task DisposeTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }


        public async Task ExecuteInTransactionAsync(Func<Task> action)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    await action();
                    await _context.SaveChangesAsync(); // Zapisz zmiany w kontekście
                    await transaction.CommitAsync(); // Zatwierdź transakcję
                }
                catch
                {
                    await transaction.RollbackAsync(); // Wycofaj w przypadku błędu
                    throw; // Rzuć wyjątek dalej
                }
            });
        }

        public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var result = await action();
                    await _context.SaveChangesAsync(); // Zapisz zmiany w kontekście
                    await transaction.CommitAsync(); // Zatwierdź transakcję
                    return result;
                }
                catch
                {
                    await transaction.RollbackAsync(); // Wycofaj w przypadku błędu
                    throw; // Rzuć wyjątek dalej
                }
            });
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _transaction?.Dispose();
                _context?.Dispose();
            }
        }
    }

}