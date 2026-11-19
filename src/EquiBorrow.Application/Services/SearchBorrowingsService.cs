using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EquiBorrow.Application.Interfaces;

namespace EquiBorrow.Application.Services;

public sealed class SearchBorrowingsService
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public SearchBorrowingsService(IUnitOfWorkFactory unitOfWorkFactory) => _unitOfWorkFactory = unitOfWorkFactory;

    public async Task<OperationResult<IReadOnlyList<BorrowingDetails>>> ExecuteAsync(
        BorrowingQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var normalizedQuery = query with
        {
            SearchText = string.IsNullOrWhiteSpace(query.SearchText) ? null : query.SearchText.Trim()
        };

        try
        {
            await using var unitOfWork = _unitOfWorkFactory.Create();
            var borrowings = await unitOfWork.Borrowings.QueryDetailsAsync(normalizedQuery, cancellationToken);
            return OperationResult<IReadOnlyList<BorrowingDetails>>.Succeeded(
                borrowings,
                $"Loaded {borrowings.Count} borrowings.");
        }
        catch (PersistenceFailureException)
        {
            return OperationResult<IReadOnlyList<BorrowingDetails>>.Failed(
                "Could not load the borrowings. Check the database connection and retry.");
        }
    }
}
