using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace WebApplication1.Repositories
{
    public interface IRepository<T> where T : class
    {
        IQueryable<T> Query(bool trackChanges = false);
        IQueryable<T> Query(Expression<Func<T, bool>> expression, bool trackChanges = false);
        Task<T?> GetByIdAsync(object id, CancellationToken ct = default);
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> expression, bool trackChanges = false, CancellationToken ct = default);
        Task<List<T>> GetAllAsync(bool trackChanges = false, CancellationToken ct = default);
        Task<List<T>> FindAsync(Expression<Func<T, bool>> expression, bool trackChanges = false, CancellationToken ct = default);
        void Add(T entity);
        void AddRange(IEnumerable<T> entities);
        void Update(T entity);
        void Remove(T entity);
        void RemoveRange(IEnumerable<T> entities);
        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}
