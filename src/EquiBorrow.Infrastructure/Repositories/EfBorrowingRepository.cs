using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EquiBorrow.Application.Interfaces;
using EquiBorrow.Domain;
using EquiBorrow.Infrastructure.Persistence;

namespace EquiBorrow.Infrastructure.Repositories;

public class EfBorrowingRepository : IBorrowingRepository
{
    private readonly EquipmentBorrowingDbContext _db;
    public EfBorrowingRepository(EquipmentBorrowingDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Borrowing borrowing, CancellationToken cancellationToken = default)
    {
        await _db.Borrowings.AddAsync(borrowing, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> GetActiveCountByStudentIdAsync(int studentId, CancellationToken cancellationToken = default)
    {
        return await _db.Borrowings.CountAsync(b => b.StudentId == studentId && b.Status == BorrowingStatus.Active, cancellationToken);
    }

    public async Task<IReadOnlyList<Borrowing>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Borrowings.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<Borrowing?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.Borrowings.FindAsync(new object[] { id }, cancellationToken);
    }
}
