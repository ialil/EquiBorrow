using Microsoft.EntityFrameworkCore;
using EquiBorrow.Domain;

namespace EquiBorrow.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(
        IDbContextFactory<EquipmentBorrowingDbContext> factory,
        CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);

        var empty = !await db.Students.AnyAsync(cancellationToken)
                 && !await db.Equipment.AnyAsync(cancellationToken)
                 && !await db.Borrowings.AnyAsync(cancellationToken);
        if (!empty) return;

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        var students = new[]
        {
            new Student { StudentNumber = "2024-0001", Name = "Ash",          Contact = "ash@example.edu",   IsActive = true  },
            new Student { StudentNumber = "2024-0002", Name = "Billy Jean",   Contact = null,                IsActive = false },
            new Student { StudentNumber = "2024-0003", Name = "Charlie Puth", Contact = "0917-000-0003",     IsActive = true  },
            new Student { StudentNumber = "2024-0004", Name = "Ashley",       Contact = null,                IsActive = true  },
            new Student { StudentNumber = "2024-0005", Name = "Steven",       Contact = null,                IsActive = true  }, // no history
            new Student { StudentNumber = "2024-0006", Name = "Joanna",       Contact = null,                IsActive = false },
            new Student { StudentNumber = "2024-0007", Name = "Fluffy",       Contact = null,                IsActive = false },
            new Student { StudentNumber = "2024-0008", Name = "Quifrey",      Contact = null,                IsActive = true  },
            new Student { StudentNumber = "2024-0009", Name = "Alice",        Contact = "alice@example.edu", IsActive = true  },
            new Student { StudentNumber = "2024-0010", Name = "Bob",          Contact = null,                IsActive = false },
            new Student { StudentNumber = "2024-0011", Name = "Queenie",      Contact = null,                IsActive = true  },
        };

        // EQ-103, 105, 108 are unavailable because they have matching ACTIVE borrowings below.
        var equipment = new[]
        {
            new Equipment { Identifier = "EQ-101", Name = "Laptop Dell XPS",             Description = "15-inch laptop", IsAvailable = true  },
            new Equipment { Identifier = "EQ-102", Name = "Projector Epson",             Description = null,             IsAvailable = true  },
            new Equipment { Identifier = "EQ-103", Name = "Arduino Kit",                 Description = "Starter kit",    IsAvailable = false },
            new Equipment { Identifier = "EQ-104", Name = "3D Printer",                  Description = null,             IsAvailable = true  },
            new Equipment { Identifier = "EQ-105", Name = "Digital Camera Canon",        Description = null,             IsAvailable = false },
            new Equipment { Identifier = "EQ-106", Name = "VR Headset Oculus",           Description = null,             IsAvailable = true  },
            new Equipment { Identifier = "EQ-107", Name = "Microphone Blue Yeti",        Description = null,             IsAvailable = true  }, // no history
            new Equipment { Identifier = "EQ-108", Name = "Tablet iPad Pro",             Description = null,             IsAvailable = false },
            new Equipment { Identifier = "EQ-109", Name = "Smartwatch Apple Watch",      Description = null,             IsAvailable = true  },
            new Equipment { Identifier = "EQ-110", Name = "External Hard Drive Seagate", Description = null,             IsAvailable = true  },
        };

        db.Students.AddRange(students);
        db.Equipment.AddRange(equipment);
        await db.SaveChangesAsync(cancellationToken); // database assigns identity keys

        static DateOnly D(int month, int day) => new(2026, month, day);

        db.Borrowings.AddRange(
            new Borrowing(0, students[0].Id, equipment[2].Id, D(11, 10), D(11, 20)),
            new Borrowing(0, students[2].Id, equipment[4].Id, D(11, 12), D(11, 19)),
            new Borrowing(0, students[3].Id, equipment[7].Id, D(11, 13), D(11, 27)));

        var r1 = new Borrowing(0, students[8].Id, equipment[0].Id, D(11, 1), D(11, 8));
        r1.MarkAsReturned(D(11, 7));
        var r2 = new Borrowing(0, students[0].Id, equipment[1].Id, D(10, 20), D(10, 27));
        r2.MarkAsReturned(D(10, 25));
        db.Borrowings.AddRange(r1, r2);

        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }
}