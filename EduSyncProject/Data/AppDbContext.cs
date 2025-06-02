using EduSyncProject.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
namespace EduSyncProject.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
        Database.EnsureCreated(); // This ensures the database exists
    }

    public virtual DbSet<Assessment> Assessments { get; set; }
    public virtual DbSet<Course> Courses { get; set; }
    public virtual DbSet<Result> Results { get; set; }
    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=tcp:edusyncprojectsqlserver.database.windows.net,1433;Initial Catalog=ritikadatabase;Persist Security Info=False;User ID=ritikaadmin0412;Password=Shinchan123;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Tell EF Core these tables already exist
        modelBuilder.Entity<User>().ToTable("User", t => t.ExcludeFromMigrations());
        modelBuilder.Entity<Course>().ToTable("Course", t => t.ExcludeFromMigrations());
        modelBuilder.Entity<Assessment>().ToTable("Assessment", t => t.ExcludeFromMigrations());
        modelBuilder.Entity<Result>().ToTable("Result", t => t.ExcludeFromMigrations());

        modelBuilder.Entity<Assessment>(entity =>
        {
            entity.ToTable("Assessment");

            entity.Property(e => e.AssessmentId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Title)
                .HasMaxLength(2000)
                .IsUnicode(false);

            entity.HasOne(d => d.Course)
                .WithMany(p => p.Assessments)
                .HasForeignKey(d => d.CourseId)
                .HasConstraintName("FK_Assessment_Course")
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.ToTable("Course");

            entity.Property(e => e.CourseId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Description)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.MediaUrl).HasMaxLength(200);
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .IsUnicode(false);

            entity.HasOne(d => d.Instructor)
                .WithMany(p => p.Courses)
                .HasForeignKey(d => d.InstructorId)
                .HasConstraintName("FK_Course_User")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasCheckConstraint(
                "CK_Course_InstructorRole",
                "InstructorId IN (SELECT UserId FROM [User] WHERE Role = 'Instructor')");

            entity.Property(e => e.InstructorId)
                .Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Throw);

            // Configure many-to-many relationship with students
            entity.HasMany(c => c.Students)
                .WithMany(u => u.EnrolledCourses)
                .UsingEntity(j => j.ToTable("StudentCourses"));
        });

        modelBuilder.Entity<Result>(entity =>
        {
            entity.ToTable("Result");

            entity.Property(e => e.ResultId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.AttemptDate).HasColumnType("datetime");

            entity.HasOne(d => d.Assessment)
                .WithMany(p => p.Results)
                .HasForeignKey(d => d.AssessmentId)
                .HasConstraintName("FK_Result_Assessment")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.User)
                .WithMany(p => p.Results)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Result_User")
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User");

            entity.HasIndex(e => e.Email, "IX_User").IsUnique();

            entity.Property(e => e.UserId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Email).HasMaxLength(300);
            entity.Property(e => e.Name)
                .HasMaxLength(2000)
                .IsUnicode(false);
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(2000)
                .IsUnicode(false);
            entity.Property(e => e.Role)
                .HasMaxLength(200)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
