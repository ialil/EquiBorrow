using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EquiBorrow.Application.Interfaces;
using EquiBorrow.Domain;

namespace EquiBorrow.Infrastructure.Repositories;

public class InMemoryEquipmentRepository : IEquipmentRepository
{
    private readonly Dictionary<int, Equipment> _equipments = new()
    {
        [101] = new Equipment(101, "Laptop Dell XPS", true),
        [102] = new Equipment(102, "Projector Epson", true),
        [103] = new Equipment(103, "Arduino Kit", false)
    };

    public Task<Equipment?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        _equipments.TryGetValue(id, out var equipment);
        return Task.FromResult(equipment);
    }

    public Task UpdateAsync(Equipment equipment, CancellationToken cancellationToken = default)
    {
        if (_equipments.ContainsKey(equipment.Id))
            _equipments[equipment.Id] = equipment;
        return Task.CompletedTask;
    }
}