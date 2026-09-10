using System.Threading;
using System.Threading.Tasks;
using EquiBorrow.Domain;

namespace EquiBorrow.Application.Interfaces;

public interface IEquipmentRepository
{
    Task<Equipment?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task UpdateAsync(Equipment equipment, CancellationToken cancellationToken = default);
    Task AddAsync(Equipment equipment, CancellationToken cancellationToken = default);
    Task RemoveAsync(int id, CancellationToken cancellationToken = default);
}