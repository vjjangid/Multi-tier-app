using KanbanBoard.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace KanbanService.Data;

public class KanbanDbContext : DbContext
{
    public KanbanDbContext(DbContextOptions<KanbanDbContext> options) : base(options)
    {
    }

    public DbSet<KanbanTask> Tasks { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration (read-only for this service)
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever(); // Users are created in AuthService
            
            entity.Property(e => e.Username)
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255);
            
            entity.Property(e => e.FullName)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.Avatar)
                .HasMaxLength(500);
            
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // KanbanTask configuration
        modelBuilder.Entity<KanbanTask>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            
            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);
            
            entity.Property(e => e.Description)
                .HasMaxLength(1000);
            
            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<int>();
            
            entity.Property(e => e.Order)
                .IsRequired();
            
            entity.Property(e => e.UserId)
                .IsRequired();
            
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasIndex(e => new { e.UserId, e.Status, e.Order });
        });

        // Add table names
        modelBuilder.Entity<User>().ToTable("Users");
        modelBuilder.Entity<KanbanTask>().ToTable("Tasks");
    }
}