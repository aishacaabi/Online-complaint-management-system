using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OnlineComplaintManagementSystem.Models;

namespace OnlineComplaintManagementSystem.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<ComplaintCategory> ComplaintCategories => Set<ComplaintCategory>();
    public DbSet<Complaint> Complaints => Set<Complaint>();
    public DbSet<ComplaintAttachment> ComplaintAttachments => Set<ComplaintAttachment>();
    public DbSet<ComplaintAssignment> ComplaintAssignments => Set<ComplaintAssignment>();
    public DbSet<ComplaintStatusHistory> ComplaintStatusHistories => Set<ComplaintStatusHistory>();
    public DbSet<ComplaintResponse> ComplaintResponses => Set<ComplaintResponse>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.HasOne(u => u.Department)
                .WithMany(d => d.Officers)
                .HasForeignKey(u => u.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Complaint>(entity =>
        {
            entity.HasIndex(c => c.ReferenceNumber).IsUnique();

            entity.HasOne(c => c.Category)
                .WithMany(cat => cat.Complaints)
                .HasForeignKey(c => c.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.Department)
                .WithMany(d => d.Complaints)
                .HasForeignKey(c => c.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.Complainant)
                .WithMany(u => u.SubmittedComplaints)
                .HasForeignKey(c => c.ComplainantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(c => c.Status);
            entity.HasIndex(c => c.Priority);
            entity.HasIndex(c => c.SubmittedDate);

            if (Database.IsNpgsql())
            {
                // PostgreSQL has no automatic rowversion column; keep it as an ordinary nullable column.
                entity.Property(c => c.RowVersion).IsConcurrencyToken(false).ValueGeneratedNever();
            }
        });

        builder.Entity<ComplaintAttachment>(entity =>
        {
            // Restrict (not Cascade) to avoid a second cascade path to Complaints alongside
            // ComplaintResponse -> Complaint, which SQL Server disallows.
            entity.HasOne(a => a.Complaint)
                .WithMany(c => c.Attachments)
                .HasForeignKey(a => a.ComplaintId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.UploadedBy)
                .WithMany()
                .HasForeignKey(a => a.UploadedById)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.ComplaintResponse)
                .WithMany(r => r.Attachments)
                .HasForeignKey(a => a.ComplaintResponseId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<ComplaintAssignment>(entity =>
        {
            entity.HasOne(a => a.Complaint)
                .WithMany(c => c.Assignments)
                .HasForeignKey(a => a.ComplaintId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.Officer)
                .WithMany(u => u.Assignments)
                .HasForeignKey(a => a.OfficerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.AssignedBy)
                .WithMany()
                .HasForeignKey(a => a.AssignedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ComplaintStatusHistory>(entity =>
        {
            entity.HasOne(h => h.Complaint)
                .WithMany(c => c.StatusHistory)
                .HasForeignKey(h => h.ComplaintId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(h => h.ChangedBy)
                .WithMany()
                .HasForeignKey(h => h.ChangedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ComplaintResponse>(entity =>
        {
            entity.HasOne(r => r.Complaint)
                .WithMany(c => c.Responses)
                .HasForeignKey(r => r.ComplaintId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Author)
                .WithMany()
                .HasForeignKey(r => r.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Notification>(entity =>
        {
            entity.HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(n => n.Complaint)
                .WithMany()
                .HasForeignKey(n => n.ComplaintId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(n => new { n.UserId, n.IsRead });
        });

        builder.Entity<Feedback>(entity =>
        {
            entity.HasIndex(f => f.ComplaintId).IsUnique();

            entity.HasOne(f => f.Complaint)
                .WithOne(c => c.Feedback)
                .HasForeignKey<Feedback>(f => f.ComplaintId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(f => f.SubmittedBy)
                .WithMany()
                .HasForeignKey(f => f.SubmittedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AuditLog>(entity =>
        {
            entity.HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(a => a.Timestamp);
        });

        builder.Entity<Department>().HasIndex(d => d.Name).IsUnique();
        builder.Entity<ComplaintCategory>().HasIndex(c => c.Name).IsUnique();
    }
}
