using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;

namespace WebApplication1.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly ApplicationDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public IQueryable<T> Query(bool trackChanges = false) =>
            trackChanges ? _dbSet : _dbSet.AsNoTracking();

        public IQueryable<T> Query(Expression<Func<T, bool>> expression, bool trackChanges = false) =>
            trackChanges ? _dbSet.Where(expression) : _dbSet.Where(expression).AsNoTracking();

        public async Task<T?> GetByIdAsync(object id, CancellationToken ct = default) =>
            await _dbSet.FindAsync(new[] { id }, ct);

        public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> expression, bool trackChanges = false, CancellationToken ct = default) =>
            trackChanges ? await _dbSet.FirstOrDefaultAsync(expression, ct) : await _dbSet.AsNoTracking().FirstOrDefaultAsync(expression, ct);

        public async Task<List<T>> GetAllAsync(bool trackChanges = false, CancellationToken ct = default) =>
            trackChanges ? await _dbSet.ToListAsync(ct) : await _dbSet.AsNoTracking().ToListAsync(ct);

        public async Task<List<T>> FindAsync(Expression<Func<T, bool>> expression, bool trackChanges = false, CancellationToken ct = default) =>
            trackChanges ? await _dbSet.Where(expression).ToListAsync(ct) : await _dbSet.Where(expression).AsNoTracking().ToListAsync(ct);

        public void Add(T entity) => _dbSet.Add(entity);

        public void AddRange(IEnumerable<T> entities) => _dbSet.AddRange(entities);

        public void Update(T entity) => _dbSet.Update(entity);

        public void Remove(T entity) => _dbSet.Remove(entity);

        public void RemoveRange(IEnumerable<T> entities) => _dbSet.RemoveRange(entities);

        public async Task<int> SaveChangesAsync(CancellationToken ct = default) =>
            await _context.SaveChangesAsync(ct);
    }
}
