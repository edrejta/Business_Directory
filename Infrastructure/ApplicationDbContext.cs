using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessDirectory.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }   

        public DbSet<User> Users => Set<User>();
        public DbSet<Business> Businesses => Set<Business>();
        public DbSet<Comment> Comments => Set<Comment>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Business>()
                .HasOne(b => b.Owner)
                .WithMany(u => u.OwnedBusinesses)
                .HasForeignKey(b => b.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Business>()
                .Property(b => b.SuspensionReason)
                .HasMaxLength(500);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(u => u.Email).HasMaxLength(256);
                entity.HasIndex(u => u.Email).IsUnique();
            });

            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.Property(a => a.Action).HasMaxLength(100);
                entity.Property(a => a.OldValue).HasMaxLength(100);
                entity.Property(a => a.NewValue).HasMaxLength(100);
                entity.Property(a => a.Reason).HasMaxLength(500);
                entity.Property(a => a.IpAddress).HasMaxLength(100);
                entity.Property(a => a.UserAgent).HasMaxLength(512);
                entity.HasIndex(a => a.CreatedAt);

                entity.HasOne(a => a.ActorUser)
                    .WithMany()
                    .HasForeignKey(a => a.ActorUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.TargetUser)
                    .WithMany()
                    .HasForeignKey(a => a.TargetUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
