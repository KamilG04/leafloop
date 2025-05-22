using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using LeafLoop.Data; // Upewnij się, że to jest poprawny namespace dla Twojego DbContext
using LeafLoop.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore; // WAŻNE: dla metod asynchronicznych EF Core

namespace LeafLoop.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly LeafLoopDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(LeafLoopDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = _context.Set<T>();
        }
        public IQueryable<T> GetAllAsQueryable() // <<< --- IMPLEMENTACJA NOWEJ METODY --- >>>
        {
            return _dbSet.AsQueryable();
        }
        public async Task<T> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        // DODANA IMPLEMENTACJA METODY:
        public async Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.FirstOrDefaultAsync(predicate);
        }

        public async Task<T> SingleOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.SingleOrDefaultAsync(predicate);
        }

        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await _dbSet.AddRangeAsync(entities);
        }

        public void Update(T entity)
        {
            // Jeśli encja nie jest śledzona, dołącz ją najpierw.
            // Jeśli jest już śledzona, Attach() nic nie zrobi lub rzuci wyjątek,
            // w zależności od stanu encji. Bezpieczniej jest sprawdzić stan lub po prostu ustawić.
            var entry = _context.Entry(entity);
            if (entry.State == EntityState.Detached)
            {
                _dbSet.Attach(entity);
            }
            entry.State = EntityState.Modified;
        }

        public void UpdateRange(IEnumerable<T> entities)
        {
            foreach (var entity in entities)
            {
                // Podobnie jak w Update(T entity)
                var entry = _context.Entry(entity);
                if (entry.State == EntityState.Detached)
                {
                    _dbSet.Attach(entity);
                }

                entry.State = EntityState.Modified;
            }
        }

        public void Remove(T entity)
        {
            if (_context.Entry(entity).State == EntityState.Detached)
            {
                _dbSet.Attach(entity);
            }
            _dbSet.Remove(entity);
        }

        public void RemoveRange(IEnumerable<T> entities)
        {
            // Upewnij się, że wszystkie encje są śledzone przed usunięciem, jeśli mogą być rozłączone.
            // Jednak RemoveRange zazwyczaj radzi sobie z tym, jeśli encje mają klucze.
            _dbSet.RemoveRange(entities);
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>> predicate = null)
        {
            if (predicate == null)
                return await _dbSet.CountAsync();
            
            return await _dbSet.CountAsync(predicate);
        }

        public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }
    }
}