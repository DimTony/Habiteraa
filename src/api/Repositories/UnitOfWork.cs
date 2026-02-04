using Habitera.Data;
using Habitera.Models;

namespace Habitera.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<ApplicationUser> Users { get; }
        IRepository<UserProfile> UserProfiles { get; }
        IRepository<AgentProfile> AgentProfiles { get; }
        IRepository<RefreshToken> RefreshTokens { get; }
        IRepository<PasswordResetToken> PasswordResetTokens { get; }
        IRepository<EmailVerificationToken> EmailVerificationTokens { get; }
        IRepository<AuditLog> AuditLogs { get; }
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }

    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IRepository<ApplicationUser>? _users;
        private IRepository<UserProfile>? _userProfiles;
        private IRepository<AgentProfile>? _agentProfiles;
        private IRepository<RefreshToken>? _refreshTokens;
        private IRepository<PasswordResetToken>? _passwordResetTokens;
        private IRepository<EmailVerificationToken>? _emailVerificationTokens;
        private IRepository<AuditLog>? _auditLogs;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        public IRepository<ApplicationUser> Users =>
            _users ??= new Repository<ApplicationUser>(_context);

        public IRepository<UserProfile> UserProfiles =>
            _userProfiles ??= new Repository<UserProfile>(_context);

        public IRepository<AgentProfile> AgentProfiles =>
            _agentProfiles ??= new Repository<AgentProfile>(_context);

        public IRepository<RefreshToken> RefreshTokens =>
    _refreshTokens ??= new Repository<RefreshToken>(_context);

        public IRepository<PasswordResetToken> PasswordResetTokens =>
    _passwordResetTokens ??= new Repository<PasswordResetToken>(_context);

        public IRepository<EmailVerificationToken> EmailVerificationTokens =>
    _emailVerificationTokens ??= new Repository<EmailVerificationToken>(_context);

        public IRepository<AuditLog> AuditLogs =>
    _auditLogs ??= new Repository<AuditLog>(_context);

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            await _context.Database.CommitTransactionAsync();
        }

        public async Task RollbackTransactionAsync()
        {
            await _context.Database.RollbackTransactionAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}