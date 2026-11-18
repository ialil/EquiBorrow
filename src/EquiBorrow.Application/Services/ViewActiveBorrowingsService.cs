using System.Collections.Generic;
using System.Linq;
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
            var activeBorrowings = await unitOfWork.Borrowings.GetActiveDetailsAsync(cancellationToken);
            var result = studentId.HasValue
                ? activeBorrowings.Where(borrowing => borrowing.StudentId == studentId.Value).ToArray()
                : activeBorrowings;
            return OperationResult<IReadOnlyList<BorrowingDetails>>.Succeeded(result, $"Loaded {result.Count} active borrowings.");
        }
        catch (PersistenceFailureException)
        {
            return OperationResult<IReadOnlyList<BorrowingDetails>>.Failed("Could not load active borrowings. Check the database connection and retry.");
        }
    }
}
