using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LeafLoop.Repositories.Interfaces
{
    public interface IRepository<T> where T : class
    {
        // Get methods
        Task<T> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<T> SingleOrDefaultAsync(Expression<Func<T, bool>> predicate);

        // Add methods
        Task AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);

        // Update methods
        void Update(T entity);
        void UpdateRange(IEnumerable<T> entities);

        // Remove methods
        void Remove(T entity);
        void RemoveRange(IEnumerable<T> entities);

        // Count method
        Task<int> CountAsync(Expression<Func<T, bool>> predicate = null);

        // Exists method
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
    }
}