using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Core.SMSModels
{
    public partial class SMSContext : DbContext
    {
        public SMSContext()
        {
        }

        public SMSContext(DbContextOptions<SMSContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Migration> Migration { get; set; }
        public virtual DbSet<System> System { get; set; }
        public virtual DbSet<Tenant> Tenant { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<System>(entity =>
            {
                entity.Property(e => e.Desc).HasMaxLength(500);

                entity.Property(e => e.Name)
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Tenant>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.ClientID)
                    .HasMaxLength(1500)
                    .IsUnicode(false);

                entity.Property(e => e.ClientInbox)
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.ClientInboxDebug)
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.ConnDebug)
                    .HasMaxLength(1500)
                    .IsUnicode(false);

                entity.Property(e => e.ConnProd)
                    .HasMaxLength(1500)
                    .IsUnicode(false);

                entity.Property(e => e.Description).HasMaxLength(1000);

                entity.Property(e => e.FtpPwd)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.FtpUid)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.FtpUrl)
                    .HasMaxLength(500)
                    .IsUnicode(false);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(250);

                entity.Property(e => e.Outbox)
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.OutboxDebug)
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.Password)
                    .HasMaxLength(1500)
                    .IsUnicode(false);

                entity.Property(e => e.SystemName)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.TaxCode)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Username)
                    .HasMaxLength(1500)
                    .IsUnicode(false);

                entity.HasOne(d => d.SystemNavigation)
                    .WithMany(p => p.Tenant)
                    .HasForeignKey(d => d.System);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
