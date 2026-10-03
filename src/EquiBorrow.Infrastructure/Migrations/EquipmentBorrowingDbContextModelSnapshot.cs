using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using EquiBorrow.Infrastructure.Persistence;

#nullable disable

namespace EquiBorrow.Infrastructure.Migrations
{
    [DbContext(typeof(EquipmentBorrowingDbContext))]
    partial class EquipmentBorrowingDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.0");

            modelBuilder.Entity("EquiBorrow.Domain.Equipment", b =>
            {
                b.Property<int>("Id");
                b.Property<string>("Name").IsRequired().HasMaxLength(200);
                b.Property<bool>("IsAvailable");
                b.HasKey("Id");
                b.ToTable("Equipment");
            });

            modelBuilder.Entity("EquiBorrow.Domain.Student", b =>
            {
                b.Property<int>("Id");
                b.Property<string>("Name").IsRequired().HasMaxLength(200);
                b.Property<bool>("IsActive");
                b.HasKey("Id");
                b.ToTable("Students");
            });

            modelBuilder.Entity("EquiBorrow.Domain.Borrowing", b =>
            {
                b.Property<int>("Id");
                b.Property<int>("StudentId");
                b.Property<int>("EquipmentId");
                b.Property<DateTime>("BorrowDate");
                b.Property<DateTime>("ExpectedReturnDate");
                b.Property<int>("Status");
                b.Property<DateTime?>("ReturnDate");
                b.HasKey("Id");
                b.HasIndex("EquipmentId");
                b.HasIndex("StudentId");
                b.ToTable("Borrowings");
            });

            // Seed data matching InitialCreate migration
            modelBuilder.Entity("EquiBorrow.Domain.Student").HasData(
                new { Id = 1, Name = "Ash", IsActive = true },
                new { Id = 2, Name = "Billy Jean", IsActive = false },
                new { Id = 3, Name = "Charlie Puth", IsActive = true },
                new { Id = 4, Name = "Ashley", IsActive = true },
                new { Id = 5, Name = "Steven", IsActive = true },
                new { Id = 6, Name = "Joanna", IsActive = false },
                new { Id = 7, Name = "Fluffy", IsActive = false },
                new { Id = 8, Name = "Quifrey", IsActive = true },
                new { Id = 9, Name = "Alice", IsActive = true },
                new { Id = 10, Name = "Bob", IsActive = false },
                new { Id = 11, Name = "Queenie", IsActive = true }
            );

            modelBuilder.Entity("EquiBorrow.Domain.Equipment").HasData(
                new { Id = 101, Name = "Laptop Dell XPS", IsAvailable = true },
                new { Id = 102, Name = "Projector Epson", IsAvailable = true },
                new { Id = 103, Name = "Arduino Kit", IsAvailable = false },
                new { Id = 104, Name = "3D Printer", IsAvailable = true },
                new { Id = 105, Name = "Digital Camera Canon", IsAvailable = false },
                new { Id = 106, Name = "VR Headset Oculus", IsAvailable = true },
                new { Id = 107, Name = "Microphone Blue Yeti", IsAvailable = true },
                new { Id = 108, Name = "Tablet iPad Pro", IsAvailable = false },
                new { Id = 109, Name = "Smartwatch Apple Watch", IsAvailable = true },
                new { Id = 110, Name = "External Hard Drive Seagate", IsAvailable = true }
            );
        }
    }
}
