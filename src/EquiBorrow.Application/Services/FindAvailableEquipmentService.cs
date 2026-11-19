using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EquiBorrow.Application.Interfaces;
using EquiBorrow.Domain;

namespace EquiBorrow.Application.Services;

public sealed class FindAvailableEquipmentService
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public FindAvailableEquipmentService(IUnitOfWorkFactory unitOfWorkFactory) => _unitOfWorkFactory = unitOfWorkFactory;

    public async Task<OperationResult<IReadOnlyList<Equipment>>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var unitOfWork = _unitOfWorkFactory.Create();
            var equipment = await unitOfWork.Equipment.GetAvailableAsync(cancellationToken);
            return OperationResult<IReadOnlyList<Equipment>>.Succeeded(equipment, $"Loaded {equipment.Count} available equipment items.");
        }
        catch (PersistenceFailureException)
        {
            return OperationResult<IReadOnlyList<Equipment>>.Failed(
                "Could not load available equipment. Check the database connection and retry.");
        }
    }
}
