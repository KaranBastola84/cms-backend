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
        public DbSet<Student> Students { get; set; } // DbSet for Student entities
        public DbSet<StudentDocument> StudentDocuments { get; set; } // DbSet for Student Documents
        public DbSet<Receipt> Receipts { get; set; } // DbSet for Receipts

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

            // Configure StudentDocument indexes
            modelBuilder.Entity<StudentDocument>()
                .HasIndex(d => d.StudentId);

            modelBuilder.Entity<StudentDocument>()
                .HasIndex(d => d.DocumentType);

            // Configure Student-Document relationship
            modelBuilder.Entity<StudentDocument>()
                .HasOne(d => d.Student)
                .WithMany()
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Receipt indexes
            modelBuilder.Entity<Receipt>()
                .HasIndex(r => r.ReceiptNumber)
                .IsUnique();

            modelBuilder.Entity<Receipt>()
                .HasIndex(r => r.StudentId);

            // Configure Student-Receipt relationship
            modelBuilder.Entity<Receipt>()
                .HasOne(r => r.Student)
                .WithMany()
                .HasForeignKey(r => r.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}