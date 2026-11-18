using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EquiBorrow.Application.Interfaces;
using EquiBorrow.Domain;
using EquiBorrow.Infrastructure.Persistence;

namespace EquiBorrow.Infrastructure.Repositories;

public class EfEquipmentRepository : IEquipmentRepository
{
    private readonly EquipmentBorrowingDbContext _db;

    public EfEquipmentRepository(EquipmentBorrowingDbContext db)
    {
        _db = db;
    }

    public async Task<Equipment?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.Equipment.FindAsync(new object[] { id }, cancellationToken);
    }

    public Task UpdateAsync(Equipment equipment, CancellationToken cancellationToken = default)
    {
        _db.Equipment.Update(equipment);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<Equipment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Equipment.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Equipment equipment, CancellationToken cancellationToken = default)
    {
        await _db.Equipment.AddAsync(equipment, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var equipment = await _db.Equipment.FindAsync(new object[] { id }, cancellationToken);
        if (equipment != null)
            _db.Equipment.Remove(equipment);
    }
}
