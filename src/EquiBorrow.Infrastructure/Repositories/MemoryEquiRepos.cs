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
        [103] = new Equipment(103, "Arduino Kit", false),
        [104] = new Equipment(104, "3D Printer", true),
        [105] = new Equipment(105, "Digital Camera Canon", false),
        [106] = new Equipment(106, "VR Headset Oculus", true),
        [107] = new Equipment(107, "Microphone Blue Yeti", true),
        [108] = new Equipment(108, "Tablet iPad Pro", false),
        [109] = new Equipment(109, "Smartwatch Apple Watch", true),
        [110] = new Equipment(110, "External Hard Drive Seagate", true)
    };
    private int _nextId = 111;

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

    public Task AddAsync(Equipment equipment, CancellationToken cancellationToken = default)
    {
        equipment.Id = _nextId++;
        _equipments[equipment.Id] = equipment;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(int id, CancellationToken cancellationToken = default)
    {
        _equipments.Remove(id);
        return Task.CompletedTask;
    }
}