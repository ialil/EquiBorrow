using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EquiBorrow.Application.Interfaces;

namespace EquiBorrow.Application.Services;

public sealed class ViewEquipmentBorrowingHistoryService
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public ViewEquipmentBorrowingHistoryService(IUnitOfWorkFactory unitOfWorkFactory) => _unitOfWorkFactory = unitOfWorkFactory;

    public async Task<OperationResult<IReadOnlyList<BorrowingDetails>>> ExecuteAsync(
        int equipmentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var unitOfWork = _unitOfWorkFactory.Create();
            if (await unitOfWork.Equipment.GetByIdAsync(equipmentId, cancellationToken) is null)
                return OperationResult<IReadOnlyList<BorrowingDetails>>.Stale("That equipment item no longer exists. Select another item.");

            var history = await unitOfWork.Borrowings.GetHistoryByEquipmentAsync(equipmentId, cancellationToken);
            return OperationResult<IReadOnlyList<BorrowingDetails>>.Succeeded(history, $"Loaded {history.Count} borrowing history records.");
        }
        catch (PersistenceFailureException)
        {
            return OperationResult<IReadOnlyList<BorrowingDetails>>.Failed(
                "Could not load the borrowing history. Check the database connection and retry.");
        }
    }
}
