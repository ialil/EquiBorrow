using System;
using System.Threading;
using System.Threading.Tasks;
using EquiBorrow.Application.Interfaces;
using EquiBorrow.Domain;

namespace EquiBorrow.Application.Services;

public sealed class ReturnEquipmentService
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;
    private readonly TimeProvider _timeProvider;

    public ReturnEquipmentService(IUnitOfWorkFactory unitOfWorkFactory, TimeProvider timeProvider)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
        _timeProvider = timeProvider;
    }

    public async Task<OperationResult> ExecuteAsync(int borrowingId, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);
        try
        {
            await using var unitOfWork = _unitOfWorkFactory.Create();
            var borrowing = await unitOfWork.Borrowings.GetByIdForUpdateAsync(borrowingId, cancellationToken);
            if (borrowing is null)
                return OperationResult.Stale("The borrowing no longer exists. Refresh the data.");
            if (borrowing.Status == BorrowingStatus.Returned)
                return OperationResult.Rejected("This borrowing has already been returned.");
            if (today < borrowing.BorrowDate)
                return OperationResult.Rejected("Return date cannot precede the borrow date.");

            var equipment = await unitOfWork.Equipment.GetByIdForUpdateAsync(borrowing.EquipmentId, cancellationToken);
            if (equipment is null)
                return OperationResult.Stale("The equipment record no longer exists. Refresh the data.");

            borrowing.MarkAsReturned(today);
            equipment.IsAvailable = true;
            await unitOfWork.CommitAsync(cancellationToken);
            return OperationResult.Succeeded("Equipment returned successfully.");
        }
        catch (CommitOutcomeUnknownException)
        {
            return await VerifyUnknownReturnAsync(borrowingId, cancellationToken);
        }
        catch (PersistenceConflictException exception) when (exception.Kind == PersistenceConflictKind.Concurrency)
        {
            return OperationResult.Stale("The borrowing or equipment changed. Refresh the data before retrying.");
        }
        catch (PersistenceConflictException)
        {
            return OperationResult.Failed("The return could not be saved because a conflicting record exists.");
        }
        catch (PersistenceFailureException)
        {
            return OperationResult.Failed("The return could not be saved. No changes were committed.");
        }
    }

    private async Task<OperationResult> VerifyUnknownReturnAsync(int borrowingId, CancellationToken cancellationToken)
    {
        try
        {
            await using var verificationUnit = _unitOfWorkFactory.Create();
            var borrowing = await verificationUnit.Borrowings.GetByIdForUpdateAsync(borrowingId, cancellationToken);
            return borrowing?.Status == BorrowingStatus.Returned
                ? OperationResult.Succeeded("The return was verified as committed.")
                : OperationResult.Failed("The return was verified as not committed. You may refresh and retry.");
        }
        catch (Exception)
        {
            return OperationResult.UnknownOutcome(
                "The commit result could not be verified. Refresh and check the borrowing before retrying.");
        }
    }
}
