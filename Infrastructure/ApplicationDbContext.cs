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
        public DbSet<City> Cities => Set<City>();
        public DbSet<Promotion> Promotions => Set<Promotion>();
        public DbSet<NewsletterSubscriber> NewsletterSubscribers => Set<NewsletterSubscriber>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Business>()
                .HasOne(b => b.Owner)
                .WithMany(u => u.OwnedBusinesses)
                .HasForeignKey(b => b.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Business>()
                .HasIndex(b => new { b.Status, b.CreatedAt });

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Comment>()
                .HasIndex(c => new { c.BusinessId, c.CreatedAt });

            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasOne(a => a.ActorUser)
                    .WithMany()
                    .HasForeignKey(a => a.ActorUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.TargetUser)
                    .WithMany()
                    .HasForeignKey(a => a.TargetUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(u => u.Email).HasMaxLength(256);
                entity.HasIndex(u => u.Email).IsUnique();
            });

            modelBuilder.Entity<City>(entity =>
            {
                entity.Property(c => c.Name).HasMaxLength(128);
                entity.HasIndex(c => c.Name).IsUnique();
            });

            modelBuilder.Entity<Promotion>(entity =>
            {
                entity.Property(p => p.Title).HasMaxLength(120);
                entity.Property(p => p.Category).HasMaxLength(32);
                entity.Property(p => p.OriginalPrice).HasPrecision(18, 2);
                entity.Property(p => p.DiscountedPrice).HasPrecision(18, 2);
                entity.HasIndex(p => new { p.BusinessId, p.IsActive, p.ExpiresAt, p.CreatedAt });

                entity.HasOne(p => p.Business)
                    .WithMany(b => b.Promotions)
                    .HasForeignKey(p => p.BusinessId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<NewsletterSubscriber>(entity =>
            {
                entity.ToTable("NewsletterSubscribers");
                entity.Property(n => n.Email)
                    .IsRequired()
                    .HasMaxLength(256);
                entity.Property(n => n.CreatedAt)
                    .IsRequired();
                entity.HasIndex(n => n.Email)
                    .HasDatabaseName("IX_NewsletterSubscribers_Email")
                    .IsUnique();
            });
        }
    }
}
