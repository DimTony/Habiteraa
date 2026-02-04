using Habitera.Data;
using Habitera.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Habitera.Repositories
{
    public interface IRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(Guid id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
        Task AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);
        void Update(T entity);
        void Remove(T entity);
        void RemoveRange(IEnumerable<T> entities);
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);

        Task<List<EmailVerificationToken>> GetActiveTokensByEmailAsync(string email);
        Task<EmailVerificationToken?> GetValidTokenAsync(string email, string tokenHash);
        Task InvalidateTokensAsync(IEnumerable<EmailVerificationToken> tokens);
        Task<EmailVerificationToken?> GetLatestTokenForUserAsync(Guid userId);

        Task<List<PasswordResetToken>> GetActivePasswordResetTokensByEmailAsync(Guid userId);
        Task<PasswordResetToken?> GetValidPasswordResetTokenAsync(Guid userId, string tokenHash);
        Task InvalidatePasswordResetTokensAsync(IEnumerable<PasswordResetToken> tokens);
        Task<PasswordResetToken?> GetLatestPasswordResetTokenForUserAsync(Guid userId);
    }

    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly ApplicationDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public virtual async Task<T?> GetByIdAsync(Guid id)
        {
            return await _dbSet.FindAsync(id);
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.FirstOrDefaultAsync(predicate);
        }

        public virtual async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public virtual async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await _dbSet.AddRangeAsync(entities);
        }

        public virtual void Update(T entity)
        {
            _dbSet.Update(entity);
        }

        public virtual void Remove(T entity)
        {
            _dbSet.Remove(entity);
        }

        public virtual void RemoveRange(IEnumerable<T> entities)
        {
            _dbSet.RemoveRange(entities);
        }

        public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
        {
            if (predicate == null)
                return await _dbSet.CountAsync();

            return await _dbSet.CountAsync(predicate);
        }

        public virtual async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }

        public async Task<List<EmailVerificationToken>> GetActiveTokensByEmailAsync(string email)
        {
            return await _context.EmailVerificationTokens
                .Where(t =>
                    t.Email == email &&
                    !t.Used &&
                    t.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();
        }

        public async Task<EmailVerificationToken?> GetValidTokenAsync(string email, string tokenHash)
        {
            return await _context.EmailVerificationTokens.FirstOrDefaultAsync(t =>
                t.Email == email &&
                t.TokenHash == tokenHash &&
                !t.Used &&
                t.ExpiresAt > DateTime.UtcNow
            );
        }

        public Task InvalidateTokensAsync(IEnumerable<EmailVerificationToken> tokens)
        {
            foreach (var token in tokens)
            {
                token.Used = true;
                token.UsedAt = DateTime.UtcNow;
            }

            return Task.CompletedTask;
        }

        public async Task<EmailVerificationToken?> GetLatestTokenForUserAsync(Guid userId)
        {
            return await _context.EmailVerificationTokens
                .Where(t =>
                    t.UserId == userId &&
                    !t.Used &&
                    t.ExpiresAt > DateTime.UtcNow
                )
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<List<PasswordResetToken>> GetActivePasswordResetTokensByEmailAsync(Guid userId)
        {
            return await _context.PasswordResetTokens
                .Where(t =>
                    t.UserId == userId &&
                    !t.Used &&
                    t.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();
        }

        public async Task<PasswordResetToken?> GetValidPasswordResetTokenAsync(Guid userId, string tokenHash)
        {
            return await _context.PasswordResetTokens.FirstOrDefaultAsync(t =>
                t.UserId == userId &&
                t.TokenHash == tokenHash &&
                !t.Used &&
                t.ExpiresAt > DateTime.UtcNow
            );
        }

        public Task InvalidatePasswordResetTokensAsync(IEnumerable<PasswordResetToken> tokens)
        {
            foreach (var token in tokens)
            {
                token.Used = true;
                token.UsedAt = DateTime.UtcNow;
            }
            return Task.CompletedTask;
        }

        public async Task<PasswordResetToken?> GetLatestPasswordResetTokenForUserAsync(Guid userId)
        {
            return await _context.PasswordResetTokens
                .Where(t =>
                    t.UserId == userId &&
                    !t.Used &&
                    t.ExpiresAt > DateTime.UtcNow
                )
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();
        }

    }
}