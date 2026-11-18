using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EquiBorrow.Application.Interfaces;
using EquiBorrow.Domain;
using EquiBorrow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EquiBorrow.Infrastructure.Repositories;

public sealed class EfEquipmentRepository : IEquipmentRepository
{
    private readonly EquipmentBorrowingDbContext _db;

    public EfEquipmentRepository(EquipmentBorrowingDbContext db) => _db = db;

    public Task<IReadOnlyList<Equipment>> GetAllAsync(CancellationToken cancellationToken = default) =>
        GetListAsync(_db.Equipment.AsNoTracking(), cancellationToken);

    public Task<IReadOnlyList<Equipment>> GetAvailableAsync(CancellationToken cancellationToken = default) =>
        GetListAsync(_db.Equipment.AsNoTracking().Where(equipment => equipment.IsAvailable), cancellationToken);

    public Task<Equipment?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _db.Equipment.AsNoTracking().SingleOrDefaultAsync(equipment => equipment.Id == id, cancellationToken);

    public Task<Equipment?> GetByIdForUpdateAsync(int id, CancellationToken cancellationToken = default) =>
        _db.Equipment.SingleOrDefaultAsync(equipment => equipment.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Equipment>> GetByIdsForUpdateAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToArray();
        if (idList.Length == 0)
            return Array.Empty<Equipment>();
        return await _db.Equipment.Where(equipment => idList.Contains(equipment.Id)).ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByIdentifierAsync(string identifier, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalizedIdentifier = identifier.Trim().ToLowerInvariant();
        var equipmentQuery = _db.Equipment.AsNoTracking().Where(equipment => equipment.Identifier.Trim().ToLower() == normalizedIdentifier);
        if (excludeId.HasValue)
            equipmentQuery = equipmentQuery.Where(equipment => equipment.Id != excludeId.Value);
        return equipmentQuery.AnyAsync(cancellationToken);
    }

    public void Add(Equipment equipment, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _db.Equipment.Add(equipment);
    }

    public async Task AddAsync(Equipment equipment, CancellationToken cancellationToken = default) =>
        await _db.Equipment.AddAsync(equipment, cancellationToken);

    public Task UpdateAsync(Equipment equipment, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _db.Equipment.Update(equipment);
        return Task.CompletedTask;
    }

    private static async Task<IReadOnlyList<Equipment>> GetListAsync(
        IQueryable<Equipment> query,
        CancellationToken cancellationToken) =>
        await query.OrderBy(equipment => equipment.Name).ToListAsync(cancellationToken);
}
