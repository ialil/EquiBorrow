using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EquiBorrow.Domain;

namespace EquiBorrow.Infrastructure.Persistence.Configurations;

public class BorrowingConfiguration : IEntityTypeConfiguration<Borrowing>
{
    public void Configure(EntityTypeBuilder<Borrowing> builder)
    {
        builder.ToTable("Borrowings", t =>
        {
            t.HasCheckConstraint("CK_Borrowings_ExpectedAfterBorrow", "\"ExpectedReturnDate\" > \"BorrowDate\"");
            t.HasCheckConstraint("CK_Borrowings_ReturnState",
                "(\"Status\" = 0 AND \"ReturnDate\" IS NULL) OR (\"Status\" = 1 AND \"ReturnDate\" IS NOT NULL AND \"ReturnDate\" >= \"BorrowDate\")");
        });

        builder.HasKey(b => b.Id);

        builder.Property(b => b.BorrowDate).HasColumnType("date").IsRequired();
        builder.Property(b => b.ExpectedReturnDate).HasColumnType("date").IsRequired();
        builder.Property(b => b.ReturnDate).HasColumnType("date").IsRequired(false);
        builder.Property(b => b.Status).IsRequired();

        builder.HasOne<Student>().WithMany()
            .HasForeignKey(b => b.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Equipment>().WithMany()
            .HasForeignKey(b => b.EquipmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // At most ONE active borrowing per item, enforced by PostgreSQL (Status = 0 is Active).
        builder.HasIndex(b => b.EquipmentId)
            .IsUnique()
            .HasFilter("\"Status\" = 0")
            .HasDatabaseName("UX_Borrowings_OneActivePerEquipment");

        builder.HasIndex(b => new { b.StudentId, b.Status }).HasDatabaseName("IX_Borrowings_Student_Status");
        builder.HasIndex(b => new { b.EquipmentId, b.BorrowDate }).HasDatabaseName("IX_Borrowings_Equipment_BorrowDate");
    }
}