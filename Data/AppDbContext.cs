using Microsoft.EntityFrameworkCore;
using SWP_BE.Models;

namespace SWP_BE.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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
