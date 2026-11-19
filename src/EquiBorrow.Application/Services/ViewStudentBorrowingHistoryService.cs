using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EquiBorrow.Application.Interfaces;

namespace EquiBorrow.Application.Services;

public sealed class ViewStudentBorrowingHistoryService
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public ViewStudentBorrowingHistoryService(IUnitOfWorkFactory unitOfWorkFactory) => _unitOfWorkFactory = unitOfWorkFactory;

    public async Task<OperationResult<IReadOnlyList<BorrowingDetails>>> ExecuteAsync(
        int studentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var unitOfWork = _unitOfWorkFactory.Create();
            if (await unitOfWork.Students.GetByIdAsync(studentId, cancellationToken) is null)
                return OperationResult<IReadOnlyList<BorrowingDetails>>.Stale("That student no longer exists. Select another student.");

            var history = await unitOfWork.Borrowings.GetHistoryByStudentAsync(studentId, cancellationToken);
            return OperationResult<IReadOnlyList<BorrowingDetails>>.Succeeded(history, $"Loaded {history.Count} borrowing history records.");
        }
        catch (PersistenceFailureException)
        {
            return OperationResult<IReadOnlyList<BorrowingDetails>>.Failed(
                "Could not load the borrowing history. Check the database connection and retry.");
        }
    }
}
