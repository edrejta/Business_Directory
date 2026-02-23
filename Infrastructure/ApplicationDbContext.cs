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
        public DbSet<Promotion> Promotions => Set<Promotion>();
        public DbSet<Report> Reports => Set<Report>();
        public DbSet<NewsletterSubscriber> NewsletterSubscribers => Set<NewsletterSubscriber>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Business>()
                .HasOne(b => b.Owner)
                .WithMany(u => u.OwnedBusinesses)
                .HasForeignKey(b => b.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.EmailVerificationToken)
                .IsUnique()
                .HasFilter("[EmailVerificationToken] IS NOT NULL");

            modelBuilder.Entity<Business>()
                .Property(b => b.Latitude)
                .HasPrecision(10, 7);

            modelBuilder.Entity<Business>()
                .Property(b => b.Longitude)
                .HasPrecision(10, 7);

            modelBuilder.Entity<Business>()
                .HasIndex(b => b.Status);

            modelBuilder.Entity<Business>()
                .HasIndex(b => b.City);

            modelBuilder.Entity<Business>()
                .HasIndex(b => new { b.Latitude, b.Longitude });

            modelBuilder.Entity<Comment>()
                .HasIndex(c => c.BusinessId);

            modelBuilder.Entity<Promotion>()
                .HasOne(p => p.Business)
                .WithMany(b => b.Promotions)
                .HasForeignKey(p => p.BusinessId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Promotion>()
                .HasIndex(p => p.IsActive);

            modelBuilder.Entity<Promotion>()
                .HasIndex(p => p.ExpiresAt);

            modelBuilder.Entity<Promotion>()
                .HasIndex(p => p.BusinessId);

            modelBuilder.Entity<Report>()
                .HasOne(r => r.Business)
                .WithMany(b => b.Reports)
                .HasForeignKey(r => r.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Report>()
                .HasOne(r => r.ReporterUser)
                .WithMany(u => u.Reports)
                .HasForeignKey(r => r.ReporterUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<NewsletterSubscriber>()
                .HasIndex(n => n.Email)
                .IsUnique();

            modelBuilder.Entity<AuditLog>()
                .HasOne(a => a.ActorUser)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(a => a.ActorUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.CreatedAt);
        }
    }
}
