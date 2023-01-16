using Microsoft.EntityFrameworkCore;

namespace TMS.API.Models
{
    public partial class HistoryContext : DbContext
    {
        public HistoryContext()
        {
        }

        public HistoryContext(DbContextOptions<HistoryContext> options)
            : base(options)
        {
        }

        public virtual DbSet<History> ECOMMERCE_History { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
