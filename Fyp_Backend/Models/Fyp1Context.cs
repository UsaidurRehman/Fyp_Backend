using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Fyp_Backend.Models;

public partial class Fyp1Context : DbContext
{
    public Fyp1Context()
    {
    }

    public Fyp1Context(DbContextOptions<Fyp1Context> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }
    public virtual DbSet<Client> Clients { get; set; }
    public virtual DbSet<Experience> Experiences { get; set; }
    public virtual DbSet<Interview> Interviews { get; set; }
    public virtual DbSet<Resignation> Resignations { get; set; }
    public virtual DbSet<Review> Reviews { get; set; }
    public virtual DbSet<Skill> Skills { get; set; }
    public virtual DbSet<Termination> Terminations { get; set; }
    public virtual DbSet<Worker> Workers { get; set; }

    // Updated naming to match standard conventions
    public virtual DbSet<WorkerCategory> WorkerCategories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Category__6DB38D4E65D868F5");
            entity.ToTable("Category");
            entity.Property(e => e.CategoryId).HasColumnName("Category_ID");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Category_Name");
        });

        // Junction Table Configuration (Many-to-Many)
        modelBuilder.Entity<WorkerCategory>(entity =>
        {
            entity.ToTable("Worker_Category");

            // Define Composite Primary Key (Worker + Category + Skill)
            entity.HasKey(e => new { e.WorkerId, e.CategoryId, e.SkillsId });

            entity.Property(e => e.WorkerId).HasColumnName("Worker_ID");
            entity.Property(e => e.CategoryId).HasColumnName("Category_ID");
            entity.Property(e => e.SkillsId).HasColumnName("Skills_ID");

        });

        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(e => e.ClientId).HasName("PK__Client__75A5D7185A7AEB78");
            entity.ToTable("Client");
            entity.HasIndex(e => e.Email, "UQ__Client__A9D105344DD91BD0").IsUnique();
            entity.Property(e => e.ClientId).HasColumnName("Client_ID");
            entity.Property(e => e.Address).HasColumnType("text");
            entity.Property(e => e.Email).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.Name).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.Password).HasMaxLength(255).IsUnicode(false);
            entity.Property(e => e.Phone).HasMaxLength(20).IsUnicode(false);
            entity.Property(e => e.Picture).HasMaxLength(255).IsUnicode(false);
        });

        modelBuilder.Entity<Experience>(entity =>
        {
            entity.HasKey(e => e.ExperienceId).HasName("PK__Experien__177FAF2EB1CE0EEA");
            entity.ToTable("Experience");
            entity.Property(e => e.ExperienceId).HasColumnName("Experience_ID");
            entity.Property(e => e.Duration).HasMaxLength(50).IsUnicode(false);
            entity.Property(e => e.ExpDetail).HasColumnType("text").HasColumnName("Exp_Detail");
            entity.Property(e => e.WorkAt).HasMaxLength(150).IsUnicode(false).HasColumnName("Work_At");
            entity.Property(e => e.WorkerId).HasColumnName("Worker_ID");

            entity.HasOne(d => d.Worker).WithMany(p => p.Experiences)
                .HasForeignKey(d => d.WorkerId)
                .HasConstraintName("FK__Experienc__Worke__571DF1D5");
        });

        modelBuilder.Entity<Interview>(entity =>
        {
            entity.HasKey(e => e.InterviewId).HasName("PK__Intervie__536D7219E4B71E80");
            entity.ToTable("Interview");
            entity.Property(e => e.InterviewId).HasColumnName("Interview_ID");
            entity.Property(e => e.Address).HasColumnType("text");
            entity.Property(e => e.ClientId).HasColumnName("Client_ID");
            entity.Property(e => e.HiringDecision).HasMaxLength(50).IsUnicode(false).HasColumnName("Hiring_Decision");
            entity.Property(e => e.InterviewDate).HasColumnType("datetime").HasColumnName("Interview_Date");
            entity.Property(e => e.Status).HasMaxLength(50).IsUnicode(false);
            entity.Property(e => e.WorkerId).HasColumnName("Worker_ID");

            entity.HasOne(d => d.Client).WithMany(p => p.Interviews)
                .HasForeignKey(d => d.ClientId)
                .HasConstraintName("FK__Interview__Clien__59FA5E80");

            entity.HasOne(d => d.Worker).WithMany(p => p.Interviews)
                .HasForeignKey(d => d.WorkerId)
                .HasConstraintName("FK__Interview__Worke__5AEE82B9");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.ReviewId).HasName("PK__Reviews__F85DA7EB6071CC75");
            entity.Property(e => e.ReviewId).HasColumnName("Review_ID");
            entity.Property(e => e.Comment).HasColumnType("text");
            entity.Property(e => e.InterviewId).HasColumnName("Interview_ID");
            entity.Property(e => e.ReviewDate).HasDefaultValueSql("(getdate())").HasColumnType("datetime");

            entity.HasOne(d => d.Interview).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.InterviewId)
                .HasConstraintName("FK__Reviews__Intervi__5FB337D6");
        });

        modelBuilder.Entity<Skill>(entity =>
        {
            entity.HasKey(e => e.SkillsId).HasName("PK__Skills__7569047CFE2BC2EA");
            entity.Property(e => e.SkillsId).HasColumnName("Skills_ID");
            entity.Property(e => e.CategoryId).HasColumnName("Category_ID");
            entity.Property(e => e.SkillName).HasMaxLength(100).IsUnicode(false).HasColumnName("Skill_Name");

            entity.HasOne(d => d.Category).WithMany(p => p.Skills)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK__Skills__Category__4F7CD00D");
        });

        modelBuilder.Entity<Worker>(entity =>
        {
            entity.HasKey(e => e.WorkerId).HasName("PK__Worker__F35E9FF469467C94");
            entity.ToTable("Worker");
            entity.HasIndex(e => e.Cnic, "UQ__Worker__A29801FA2512666E").IsUnique();

            entity.Property(e => e.WorkerId).HasColumnName("Worker_ID");
            entity.Property(e => e.Address).HasColumnType("text");
            entity.Property(e => e.AvailableStatus).HasDefaultValue(true).HasColumnName("Available_Status");
            entity.Property(e => e.CategoryId).HasColumnName("Category_ID");

            entity.Property(e => e.Cnic).HasMaxLength(20).IsUnicode(false);
            entity.Property(e => e.Gender).HasMaxLength(10);
            entity.Property(e => e.Name).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.Password).HasMaxLength(255).IsUnicode(false);
            entity.Property(e => e.Phone).HasMaxLength(20).IsUnicode(false);
            entity.Property(e => e.Picture).HasMaxLength(255).IsUnicode(false);
            entity.Property(e => e.Salary).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Category).WithMany(p => p.Workers)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK__Worker__Category__52593CB8");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}