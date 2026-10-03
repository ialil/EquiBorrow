using Microsoft.EntityFrameworkCore;
using EquiBorrow.Domain;
using EquiBorrow.Infrastructure.Persistence.Configurations;

namespace EquiBorrow.Infrastructure.Persistence;

public class EquipmentBorrowingDbContext : DbContext
{
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<Borrowing> Borrowings => Set<Borrowing>();

    public EquipmentBorrowingDbContext(DbContextOptions<EquipmentBorrowingDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new StudentConfiguration());
        modelBuilder.ApplyConfiguration(new EquipmentConfiguration());
        modelBuilder.ApplyConfiguration(new BorrowingConfiguration());
        // Seed data using EF Core HasData so it is part of migrations
        modelBuilder.Entity<Student>().HasData(
            new Student { Id = 1, Name = "Ash", IsActive = true },
            new Student { Id = 2, Name = "Billy Jean", IsActive = false },
            new Student { Id = 3, Name = "Charlie Puth", IsActive = true },
            new Student { Id = 4, Name = "Ashley", IsActive = true },
            new Student { Id = 5, Name = "Steven", IsActive = true },
            new Student { Id = 6, Name = "Joanna", IsActive = false },
            new Student { Id = 7, Name = "Fluffy", IsActive = false },
            new Student { Id = 8, Name = "Quifrey", IsActive = true },
            new Student { Id = 9, Name = "Alice", IsActive = true },
            new Student { Id = 10, Name = "Bob", IsActive = false },
            new Student { Id = 11, Name = "Queenie", IsActive = true }
        );

        modelBuilder.Entity<Equipment>().HasData(
            new Equipment { Id = 101, Name = "Laptop Dell XPS", IsAvailable = true },
            new Equipment { Id = 102, Name = "Projector Epson", IsAvailable = true },
            new Equipment { Id = 103, Name = "Arduino Kit", IsAvailable = false },
            new Equipment { Id = 104, Name = "3D Printer", IsAvailable = true },
            new Equipment { Id = 105, Name = "Digital Camera Canon", IsAvailable = false },
            new Equipment { Id = 106, Name = "VR Headset Oculus", IsAvailable = true },
            new Equipment { Id = 107, Name = "Microphone Blue Yeti", IsAvailable = true },
            new Equipment { Id = 108, Name = "Tablet iPad Pro", IsAvailable = false },
            new Equipment { Id = 109, Name = "Smartwatch Apple Watch", IsAvailable = true },
            new Equipment { Id = 110, Name = "External Hard Drive Seagate", IsAvailable = true }
        );

        base.OnModelCreating(modelBuilder);
    }
}
