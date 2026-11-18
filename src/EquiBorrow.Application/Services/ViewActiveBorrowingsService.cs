using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EquiBorrow.Application.Interfaces;
using EquiBorrow.Domain;

namespace EquiBorrow.Application.Services;

public sealed class ViewActiveBorrowingsService
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public ViewActiveBorrowingsService(IUnitOfWorkFactory unitOfWorkFactory) => _unitOfWorkFactory = unitOfWorkFactory;

    public async Task<OperationResult<IReadOnlyList<BorrowingDetails>>> ExecuteAsync(
        int? studentId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var unitOfWork = _unitOfWorkFactory.Create();
            var result = await unitOfWork.Borrowings.QueryDetailsAsync(
                new BorrowingQuery
                {
                    StudentId = studentId,
                    Status = BorrowingStatus.Active
                },
                cancellationToken);
            return OperationResult<IReadOnlyList<BorrowingDetails>>.Succeeded(result, $"Loaded {result.Count} active borrowings.");
        }
        catch (PersistenceFailureException)
        {
            return OperationResult<IReadOnlyList<BorrowingDetails>>.Failed("Could not load active borrowings. Check the database connection and retry.");
        }
    }
}
