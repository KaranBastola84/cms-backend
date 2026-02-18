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
        public DbSet<FollowUpNote> FollowUpNotes { get; set; } // DbSet for Inquiry Follow-up Notes
        public DbSet<AuditLog> AuditLogs { get; set; } // DbSet for AuditLog entities
        public DbSet<Course> Courses { get; set; } // DbSet for Courses
        public DbSet<Batch> Batches { get; set; } // DbSet for Batches
        public DbSet<Student> Students { get; set; } // DbSet for Student entities
        public DbSet<StudentDocument> StudentDocuments { get; set; } // DbSet for Student Documents
        public DbSet<Receipt> Receipts { get; set; } // DbSet for Receipts
        public DbSet<Attendance> Attendances { get; set; } // DbSet for Attendance
        public DbSet<PaymentPlan> PaymentPlans { get; set; } // DbSet for Payment Plans
        public DbSet<Installment> Installments { get; set; } // DbSet for Installments
        public DbSet<StripePayment> StripePayments { get; set; } // DbSet for Stripe Payments
        public DbSet<FeeStructure> FeeStructures { get; set; } // DbSet for Fee Structures
        public DbSet<Transaction> Transactions { get; set; } // DbSet for Transactions
        public DbSet<UserNotificationRead> UserNotificationReads { get; set; } // DbSet for User Notification Read Status
        public DbSet<Product> Products { get; set; } // DbSet for Products (Inventory)
        public DbSet<ProductCategory> ProductCategories { get; set; } // DbSet for Product Categories
        public DbSet<Order> Orders { get; set; } // DbSet for Orders
        public DbSet<OrderItem> OrderItems { get; set; } // DbSet for Order Items
        public DbSet<ProductReview> ProductReviews { get; set; } // DbSet for Product Reviews

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

            // Configure Course-Batch relationship
            modelBuilder.Entity<Batch>()
                .HasOne(b => b.Course)
                .WithMany(c => c.Batches)
                .HasForeignKey(b => b.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Course-Student relationship
            modelBuilder.Entity<Student>()
                .HasOne<Course>()
                .WithMany(c => c.Students)
                .HasForeignKey(s => s.CourseId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure Batch-Student relationship
            modelBuilder.Entity<Student>()
                .HasOne<Batch>()
                .WithMany(b => b.Students)
                .HasForeignKey(s => s.BatchId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure Attendance indexes
            modelBuilder.Entity<Attendance>()
                .HasIndex(a => a.StudentId);

            modelBuilder.Entity<Attendance>()
                .HasIndex(a => a.BatchId);

            modelBuilder.Entity<Attendance>()
                .HasIndex(a => a.AttendanceDate);

            // Configure unique constraint for student, batch, and date (prevent duplicates)
            modelBuilder.Entity<Attendance>()
                .HasIndex(a => new { a.StudentId, a.BatchId, a.AttendanceDate })
                .IsUnique();

            // Configure Attendance-Student relationship
            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Student)
                .WithMany()
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Attendance-Batch relationship
            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Batch)
                .WithMany()
                .HasForeignKey(a => a.BatchId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure UserNotificationRead indexes
            modelBuilder.Entity<UserNotificationRead>()
                .HasIndex(n => n.UserId);

            modelBuilder.Entity<UserNotificationRead>()
                .HasIndex(n => n.NotificationKey);

            modelBuilder.Entity<UserNotificationRead>()
                .HasIndex(n => new { n.UserId, n.NotificationKey })
                .IsUnique();

            // Configure Product indexes
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Category);

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.IsActive);

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.IsFeatured);

            // Configure Order indexes
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.OrderNumber)
                .IsUnique();

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.CustomerEmail);

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.Status);

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.OrderDate);

            // Configure Order-OrderItem relationship
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Product-OrderItem relationship
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure OrderItem indexes
            modelBuilder.Entity<OrderItem>()
                .HasIndex(oi => oi.OrderId);

            modelBuilder.Entity<OrderItem>()
                .HasIndex(oi => oi.ProductId);

            // Configure ProductReview indexes
            modelBuilder.Entity<ProductReview>()
                .HasIndex(pr => pr.ProductId);

            modelBuilder.Entity<ProductReview>()
                .HasIndex(pr => pr.IsApproved);

            modelBuilder.Entity<ProductReview>()
                .HasIndex(pr => pr.CreatedAt);

            // Configure Product-ProductReview relationship
            modelBuilder.Entity<ProductReview>()
                .HasOne(pr => pr.Product)
                .WithMany()
                .HasForeignKey(pr => pr.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure ProductCategory indexes
            modelBuilder.Entity<ProductCategory>()
                .HasIndex(pc => pc.Name)
                .IsUnique();

            modelBuilder.Entity<ProductCategory>()
                .HasIndex(pc => pc.IsActive);

            modelBuilder.Entity<ProductCategory>()
                .HasIndex(pc => pc.DisplayOrder);
        }
    }
}