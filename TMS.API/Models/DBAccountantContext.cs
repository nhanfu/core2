using Microsoft.EntityFrameworkCore;

namespace TMS.API.Models
{
    public partial class DBAccountantContext : DbContext
    {
        public DBAccountantContext()
        {
        }

        public DBAccountantContext(DbContextOptions<DBAccountantContext> options)
            : base(options)
        {
        }

        public virtual DbSet<SaleAC> Sale { get; set; }
        public virtual DbSet<Partner> Vendor { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
