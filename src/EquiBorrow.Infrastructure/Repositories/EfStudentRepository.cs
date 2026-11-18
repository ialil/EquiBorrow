using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EquiBorrow.Application.Interfaces;
using EquiBorrow.Domain;
using EquiBorrow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EquiBorrow.Infrastructure.Repositories;

public sealed class EfStudentRepository : IStudentRepository
{
    private readonly EquipmentBorrowingDbContext _db;

    public EfStudentRepository(EquipmentBorrowingDbContext db) => _db = db;

    public async Task<IReadOnlyList<Student>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _db.Students.AsNoTracking().OrderBy(student => student.Name).ToListAsync(cancellationToken);

    public Task<Student?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _db.Students.AsNoTracking().SingleOrDefaultAsync(student => student.Id == id, cancellationToken);

    public Task<Student?> GetByIdForUpdateAsync(int id, CancellationToken cancellationToken = default) =>
        _db.Students.SingleOrDefaultAsync(student => student.Id == id, cancellationToken);

    public Task<bool> ExistsByNumberAsync(string number, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(number);
        var normalizedNumber = number.Trim().ToLowerInvariant();
        var students = _db.Students.AsNoTracking().Where(student => student.StudentNumber.Trim().ToLower() == normalizedNumber);
        if (excludeId.HasValue)
            students = students.Where(student => student.Id != excludeId.Value);
        return students.AnyAsync(cancellationToken);
    }

    public void Add(Student student, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _db.Students.Add(student);
    }

    public async Task AddAsync(Student student, CancellationToken cancellationToken = default) =>
        await _db.Students.AddAsync(student, cancellationToken);

    public Task UpdateAsync(Student student, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _db.Students.Update(student);
        return Task.CompletedTask;
    }
}
