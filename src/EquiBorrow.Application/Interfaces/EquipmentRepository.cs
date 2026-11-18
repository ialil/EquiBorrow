using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EquiBorrow.Domain;

namespace EquiBorrow.Application.Interfaces;

public interface IEquipmentRepository
{
    Task<IReadOnlyList<Equipment>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Equipment>> GetAvailableAsync(CancellationToken cancellationToken = default);
    Task<Equipment?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Equipment?> GetByIdForUpdateAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Equipment>> GetByIdsForUpdateAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);
    Task<bool> ExistsByIdentifierAsync(string identifier, int? excludeId = null, CancellationToken cancellationToken = default);
    void Add(Equipment equipment, CancellationToken cancellationToken = default);
}
