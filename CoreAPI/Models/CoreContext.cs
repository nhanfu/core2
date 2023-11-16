using Microsoft.EntityFrameworkCore;

namespace Core.Models;

public partial class CoreContext : DbContext
{
    public CoreContext(DbContextOptions<CoreContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Chat> Chat { get; set; }

    public virtual DbSet<Component> Component { get; set; }

    public virtual DbSet<ComponentGroup> ComponentGroup { get; set; }

    public virtual DbSet<Conversation> Conversation { get; set; }

    public virtual DbSet<Dictionary> Dictionary { get; set; }

    public virtual DbSet<Entity> Entity { get; set; }

    public virtual DbSet<EntityRef> EntityRef { get; set; }

    public virtual DbSet<Feature> Feature { get; set; }

    public virtual DbSet<FeaturePolicy> FeaturePolicy { get; set; }

    public virtual DbSet<FileUpload> FileUpload { get; set; }

    public virtual DbSet<Intro> Intro { get; set; }

    public virtual DbSet<MasterData> MasterData { get; set; }

    public virtual DbSet<RequestLog> RequestLog { get; set; }

    public virtual DbSet<Role> Role { get; set; }

    public virtual DbSet<Services> Services { get; set; }

    public virtual DbSet<TaskNotification> TaskNotification { get; set; }

    public virtual DbSet<User> User { get; set; }

    public virtual DbSet<UserLogin> UserLogin { get; set; }

    public virtual DbSet<UserRole> UserRole { get; set; }

    public virtual DbSet<UserSetting> UserSetting { get; set; }

    public virtual DbSet<Vendor> Vendor { get; set; }

    public virtual DbSet<TenantEnv> TenantEnv { get; set; }

    public virtual DbSet<TenantPage> TenantPage { get; set; }
}
