using Core.Models;
using CoreAPI.Data;
using CoreAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace CoreAPI.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction _transaction;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        private IRepository<User> _users;
        public IRepository<User> Users => _users ??= new Repository<User>(_context);

        private IRepository<Tenant> _tenants;
        public IRepository<Tenant> Tenants => _tenants ??= new Repository<Tenant>(_context);

        private IRepository<Role> _roles;
        public IRepository<Role> Roles => _roles ??= new Repository<Role>(_context);

        private IRepository<UserRole> _userRoles;
        public IRepository<UserRole> UserRoles => _userRoles ??= new Repository<UserRole>(_context);

        private IRepository<Feature> _features;
        public IRepository<Feature> Features => _features ??= new Repository<Feature>(_context);

        private IRepository<FeaturePolicy> _featurePolicies;
        public IRepository<FeaturePolicy> FeaturePolicies => _featurePolicies ??= new Repository<FeaturePolicy>(_context);

        private IRepository<Entity> _entities;
        public IRepository<Entity> Entities => _entities ??= new Repository<Entity>(_context);

        private IRepository<Component> _components;
        public IRepository<Component> Components => _components ??= new Repository<Component>(_context);

        private IRepository<Partner> _partners;
        public IRepository<Partner> Partners => _partners ??= new Repository<Partner>(_context);

        private IRepository<Resource> _resources;
        public IRepository<Resource> Resources => _resources ??= new Repository<Resource>(_context);

        private IRepository<UserSetting> _userSettings;
        public IRepository<UserSetting> UserSettings => _userSettings ??= new Repository<UserSetting>(_context);

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
                if (_transaction != null)
                {
                    await _transaction.CommitAsync();
                }
            }
            catch
            {
                await RollbackAsync();
                throw;
            }
            finally
            {
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }

        public async Task RollbackAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}
