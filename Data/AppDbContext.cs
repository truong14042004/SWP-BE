using Microsoft.EntityFrameworkCore;
using SWP_BE.Models;

namespace SWP_BE.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Skill> Skills => Set<Skill>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Skill>(entity =>
        {
            entity.ToTable("skills");

            entity.HasIndex(e => e.Category, "IX_skills_Category");

            entity.HasIndex(e => e.IsActive, "IX_skills_IsActive");

            entity.HasIndex(e => new { e.Name, e.Category }, "IX_skills_Name_Category").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Category).HasMaxLength(80);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(150);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");

            entity.HasKey(user => user.Id);

            entity.Property(user => user.Username)
                .HasMaxLength(100);

            entity.HasIndex(user => user.Username)
                .IsUnique();

            entity.Property(user => user.Email)
                .HasMaxLength(256)
                .IsRequired();

            entity.HasIndex(user => user.Email)
                .IsUnique();

            entity.Property(user => user.FullName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(user => user.AvatarUrl)
                .HasMaxLength(1024);

            entity.Property(user => user.GoogleSubject)
                .HasMaxLength(128);

            entity.HasIndex(user => user.GoogleSubject)
                .IsUnique();

            entity.Property(user => user.PasswordHash)
                .HasMaxLength(512);

            entity.Property(user => user.EmailVerificationOtpHash)
                .HasMaxLength(512);

            entity.Property(user => user.Role)
                .HasMaxLength(50)
                .IsRequired();
        });
    }
}
