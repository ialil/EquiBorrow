using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EquiBorrow.Application.Interfaces;
using EquiBorrow.Domain;
using EquiBorrow.Infrastructure.Persistence;

namespace EquiBorrow.Infrastructure.Repositories;

public class EfStudentRepository : IStudentRepository
{
    private readonly EquipmentBorrowingDbContext _db;

    public EfStudentRepository(EquipmentBorrowingDbContext db)
    {
        _db = db;
    }

    public async Task<Student?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.Students.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IReadOnlyList<Student>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Students.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Student student, CancellationToken cancellationToken = default)
    {
        await _db.Students.AddAsync(student, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Student student, CancellationToken cancellationToken = default)
    {
        _db.Students.Update(student);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var s = await _db.Students.FindAsync(new object[] { id }, cancellationToken);
        if (s != null)
        {
            _db.Students.Remove(s);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
