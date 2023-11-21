using Microsoft.EntityFrameworkCore;

namespace Core.Models
{
    public partial class LOGContext : DbContext
    {
        public LOGContext()
        {
        }

        public LOGContext(DbContextOptions<LOGContext> options)
            : base(options)
        {
        }

        public virtual DbSet<RequestLog> RequestLog { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RequestLog>(entity =>
            {
                entity.Property(e => e.HttpMethod).HasMaxLength(250);

                entity.Property(e => e.Path).HasMaxLength(500);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
