using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EquiBorrow.Domain;

namespace EquiBorrow.Infrastructure.Persistence.Configurations;

public class BorrowingConfiguration : IEntityTypeConfiguration<Borrowing>
{
    public void Configure(EntityTypeBuilder<Borrowing> builder)
    {
        builder.ToTable("Borrowings");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.BorrowDate).IsRequired();
        builder.Property(b => b.ExpectedReturnDate).IsRequired();
        builder.Property(b => b.Status).IsRequired();
        builder.Property(b => b.ReturnDate).IsRequired(false);

        builder.HasOne<Student>().WithMany()
            .HasForeignKey(b => b.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Equipment>().WithMany()
            .HasForeignKey(b => b.EquipmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
