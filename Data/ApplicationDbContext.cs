using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using JWTAuthAPI.Models;

namespace JWTAuthAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ApplicationUser> ApplicationUsers { get; set; } // DbSet for ApplicationUser entities
        public DbSet<Inquiry> Inquiries { get; set; } // DbSet for Inquiry entities
        public DbSet<AuditLog> AuditLogs { get; set; } // DbSet for AuditLog entities

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure AuditLog primary key
            modelBuilder.Entity<AuditLog>()
                .HasKey(a => a.LogId);

            // Configure indexes for better query performance
            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.UserId);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.Module);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.Timestamp);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.ActionType);
        }
    }
}