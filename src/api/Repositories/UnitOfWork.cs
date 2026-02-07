using Habitera.Data;
using Habitera.Models;

namespace Habitera.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<ApplicationUser> Users { get; }
        IGenericRepository<UserProfile> UserProfiles { get; }
        IGenericRepository<AgentProfile> AgentProfiles { get; }
        IGenericRepository<RefreshToken> RefreshTokens { get; }
        IGenericRepository<AuditLog> AuditLogs { get; }
        IGenericRepository<SavedSearch> SavedSearches { get; }
        IGenericRepository<UserInteraction> UserInteractions { get; }
        IPasswordResetTokenRepository PasswordResetTokens { get; }
        IEmailVerificationTokenRepository EmailVerificationTokens { get; }
        IPropertyRepository Properties { get; }
        IPropertyImageRepository PropertyImages { get; }
        IPropertyAmenityRepository PropertyAmenities { get; }
        IFavoriteRepository Favorites { get; }
        IViewingBookingRepository ViewingBookings { get; }

        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }

    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IGenericRepository<ApplicationUser>? _users;
        private IGenericRepository<UserProfile>? _userProfiles;
        private IGenericRepository<AgentProfile>? _agentProfiles;
        private IGenericRepository<RefreshToken>? _refreshTokens;
        private IGenericRepository<SavedSearch>? _savedSearches;
        private IGenericRepository<UserInteraction>? _userInteractions;
        private IGenericRepository<AuditLog>? _auditLogs;
        private IPasswordResetTokenRepository? _passwordResetTokens;
        private IEmailVerificationTokenRepository? _emailVerificationTokens;
        private IPropertyRepository? _properties;
        private IPropertyImageRepository? _propertyImages;
        private IPropertyAmenityRepository? _propertyAmenities;
        private IFavoriteRepository? _favorites;
        private IViewingBookingRepository? _viewingBookings;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        public IGenericRepository<ApplicationUser> Users =>
            _users ??= new GenericRepository<ApplicationUser>(_context);

        public IGenericRepository<UserProfile> UserProfiles =>
            _userProfiles ??= new GenericRepository<UserProfile>(_context);

        public IGenericRepository<AgentProfile> AgentProfiles =>
            _agentProfiles ??= new GenericRepository<AgentProfile>(_context);

        public IGenericRepository<RefreshToken> RefreshTokens =>
            _refreshTokens ??= new GenericRepository<RefreshToken>(_context);

        public IGenericRepository<SavedSearch> SavedSearches =>
           _savedSearches ??= new GenericRepository<SavedSearch>(_context);
       public IGenericRepository<UserInteraction> UserInteractions =>
           _userInteractions ??= new GenericRepository<UserInteraction>(_context);
        public IGenericRepository<AuditLog> AuditLogs =>
            _auditLogs ??= new GenericRepository<AuditLog>(_context);
        public IPasswordResetTokenRepository PasswordResetTokens =>
            _passwordResetTokens ??= new PasswordResetTokenRepository(_context);

        public IEmailVerificationTokenRepository EmailVerificationTokens =>
            _emailVerificationTokens ??= new EmailVerificationTokenRepository(_context);


        public IPropertyRepository Properties =>
            _properties ??= new PropertyRepository(_context);

        public IPropertyImageRepository PropertyImages =>
            _propertyImages ??= new PropertyImageRepository(_context);

        public IPropertyAmenityRepository PropertyAmenities =>
            _propertyAmenities ??= new PropertyAmenityRepository(_context);

        public IFavoriteRepository Favorites =>
            _favorites ??= new FavoriteRepository(_context);

        public IViewingBookingRepository ViewingBookings =>
            _viewingBookings ??= new ViewingBookingRepository(_context);

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