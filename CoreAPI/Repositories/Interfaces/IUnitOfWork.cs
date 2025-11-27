using Core.Models;
using CoreAPI.Repositories.Interfaces;

namespace CoreAPI.Repositories.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<User> Users { get; }
        IRepository<Tenant> Tenants { get; }
        IRepository<Role> Roles { get; }
        IRepository<UserRole> UserRoles { get; }
        IRepository<Feature> Features { get; }
        IRepository<FeaturePolicy> FeaturePolicies { get; }
        IRepository<Entity> Entities { get; }
        IRepository<Component> Components { get; }
        IRepository<Partner> Partners { get; }
        IRepository<Resource> Resources { get; }
        IRepository<UserSetting> UserSettings { get; }
        
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitAsync();
        Task RollbackAsync();
    }
}
