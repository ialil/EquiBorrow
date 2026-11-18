using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EquiBorrow.Application.Interfaces;
using EquiBorrow.Domain;

namespace EquiBorrow.Application.Services;

public sealed class ViewEquipmentCatalogService
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public ViewEquipmentCatalogService(IUnitOfWorkFactory unitOfWorkFactory) => _unitOfWorkFactory = unitOfWorkFactory;

    public async Task<OperationResult<IReadOnlyList<Equipment>>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var unitOfWork = _unitOfWorkFactory.Create();
            var equipment = await unitOfWork.Equipment.GetAllAsync(cancellationToken);
            return OperationResult<IReadOnlyList<Equipment>>.Succeeded(equipment, $"Loaded {equipment.Count} equipment items.");
        }
        catch (PersistenceFailureException)
        {
            return OperationResult<IReadOnlyList<Equipment>>.Failed("Could not load equipment. Check the database connection and retry.");
        }
    }
}
