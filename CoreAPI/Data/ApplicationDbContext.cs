using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace CoreAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // DbSets
        public DbSet<User> Users { get; set; }
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Feature> Features { get; set; }
        public DbSet<FeaturePolicy> FeaturePolicies { get; set; }
        public DbSet<Entity> Entities { get; set; }
        public DbSet<Component> Components { get; set; }
        public DbSet<Partner> Partners { get; set; }
        public DbSet<Resource> Resources { get; set; }
        public DbSet<UserSetting> UserSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User Configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("User");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(50);
                entity.Property(e => e.UserName).HasMaxLength(255);
                entity.Property(e => e.Email).HasMaxLength(255);
                entity.Property(e => e.Code).HasMaxLength(50);
                entity.Property(e => e.PhoneNumber).HasMaxLength(50);
                entity.HasIndex(e => e.UserName);
                entity.HasIndex(e => e.Email);
                entity.Ignore(e => e.Company);
            });

            // Tenant Configuration
            modelBuilder.Entity<Tenant>(entity =>
            {
                entity.ToTable("Tenant");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(50);
                entity.Property(e => e.TenantCode).HasMaxLength(50).IsRequired();
                entity.Property(e => e.SubTenant).HasMaxLength(50);
                entity.Property(e => e.Env).HasMaxLength(50);
                entity.Property(e => e.ConnKey).HasMaxLength(255);
                entity.HasIndex(e => e.TenantCode).IsUnique();
            });

            // Role Configuration
            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("Role");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(50);
                entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
            });

            // UserRole Configuration
            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.ToTable("UserRole");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(50);
                entity.Property(e => e.UserId).HasMaxLength(50);
                entity.Property(e => e.RoleId).HasMaxLength(50);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.RoleId);
                entity.Ignore(e => e.Role);
                entity.Ignore(e => e.User);
            });

            // Feature Configuration
            modelBuilder.Entity<Feature>(entity =>
            {
                entity.ToTable("Feature");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(50);
                entity.Property(e => e.Name).HasMaxLength(255);
                entity.Property(e => e.Label).HasMaxLength(255);
                entity.Property(e => e.ParentId).HasMaxLength(50);
                entity.Property(e => e.EntityId).HasMaxLength(50);
                entity.HasIndex(e => e.ParentId);
                entity.Ignore(e => e.ComponentGroup);
                entity.Ignore(e => e.GridPolicies);
                entity.Ignore(e => e.Components);
                entity.Ignore(e => e.FeaturePolicies);
                entity.Ignore(e => e.UserSettings);
            });

            // FeaturePolicy Configuration
            modelBuilder.Entity<FeaturePolicy>(entity =>
            {
                entity.ToTable("FeaturePolicy");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(50);
                entity.Property(e => e.RoleId).HasMaxLength(50);
                entity.Property(e => e.FeatureId).HasMaxLength(50);
            });

            // Entity Configuration
            modelBuilder.Entity<Entity>(entity =>
            {
                entity.ToTable("Entity");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(50);
                entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
                entity.Property(e => e.TenantCode).HasMaxLength(50);
                entity.Ignore(e => e.Component);
                entity.Ignore(e => e.Feature);
                entity.Ignore(e => e.TaskNotification);
            });

            // Component Configuration
            modelBuilder.Entity<Component>(entity =>
            {
                entity.ToTable("Component");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(50);
                entity.Property(e => e.FieldName).HasMaxLength(255);
                entity.Property(e => e.Label).HasMaxLength(255);
                entity.Property(e => e.FeatureId).HasMaxLength(50);
                entity.Property(e => e.EntityId).HasMaxLength(50);
                entity.Property(e => e.ParentId).HasMaxLength(50);
                entity.Ignore(e => e.Children);
                entity.Ignore(e => e.Components);
                entity.Ignore(e => e.Parent);
            });

            // Partner Configuration
            modelBuilder.Entity<Partner>(entity =>
            {
                entity.ToTable("Partner");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(50);
                entity.Property(e => e.Name).HasMaxLength(255);
                entity.Property(e => e.Code).HasMaxLength(50);
                entity.Property(e => e.Email).HasMaxLength(255);
            });

            // Resource Configuration
            modelBuilder.Entity<Resource>(entity =>
            {
                entity.ToTable("Resource");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(50);
                entity.Property(e => e.TenantCode).HasMaxLength(50);
                entity.Property(e => e.Path).HasMaxLength(500);
                entity.Property(e => e.ContentType).HasMaxLength(100);
            });

            // UserSetting Configuration
            modelBuilder.Entity<UserSetting>(entity =>
            {
                entity.ToTable("UserSetting");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(50);
            });
        }
    }
}
