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
        // Initial data is inserted once by DatabaseInitializer (not HasData).
        base.OnModelCreating(modelBuilder);
    }
}