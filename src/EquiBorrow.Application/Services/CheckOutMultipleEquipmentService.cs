using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EquiBorrow.Application.Interfaces;
using EquiBorrow.Domain;

namespace EquiBorrow.Application.Services;

public sealed class CheckOutMultipleEquipmentService
{
    private const string StaleMessage = "One of the items was just borrowed or changed. The list was refreshed.";
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;
    private readonly TimeProvider _timeProvider;

    public CheckOutMultipleEquipmentService(IUnitOfWorkFactory unitOfWorkFactory, TimeProvider timeProvider)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
        _timeProvider = timeProvider;
    }

    public async Task<OperationResult<IReadOnlyList<BorrowingDetails>>> ExecuteAsync(
        int studentId,
        IReadOnlyCollection<int> equipmentIds,
        DateOnly expectedReturnDate,
        CancellationToken cancellationToken = default)
    {
        var requestedIds = equipmentIds.ToArray();
        if (requestedIds.Distinct().Count() != requestedIds.Length)
            return OperationResult<IReadOnlyList<BorrowingDetails>>.Rejected("The same item was selected more than once.");

        if (requestedIds.Length < 2)
            return OperationResult<IReadOnlyList<BorrowingDetails>>.Rejected("Select at least two different items.");

        var today = DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);
        try
        {
            await using var unitOfWork = _unitOfWorkFactory.Create();
            var student = await unitOfWork.Students.GetByIdForUpdateAsync(studentId, cancellationToken);
            if (student is null)
                return OperationResult<IReadOnlyList<BorrowingDetails>>.Rejected("Student was not found.");
            if (!student.IsActive)
                return OperationResult<IReadOnlyList<BorrowingDetails>>.Rejected(
                    $"{student.StudentNumber} — {student.Name} is restricted from borrowing.");

            if (expectedReturnDate <= today)
                return OperationResult<IReadOnlyList<BorrowingDetails>>.Rejected("Expected return date must be later than today.");

            var equipment = await unitOfWork.Equipment.GetByIdsForUpdateAsync(requestedIds, cancellationToken);
            if (equipment.Count != requestedIds.Length)
                return OperationResult<IReadOnlyList<BorrowingDetails>>.Stale(
                    "One or more selected items no longer exist. The list was refreshed.");

            var equipmentById = equipment.ToDictionary(item => item.Id);
            var unavailableIdentifiers = requestedIds
                .Select(id => equipmentById[id])
                .Where(item => !item.IsAvailable)
                .Select(item => item.Identifier)
                .ToArray();
            if (unavailableIdentifiers.Length > 0)
                return OperationResult<IReadOnlyList<BorrowingDetails>>.Rejected(
                    $"{string.Join(", ", unavailableIdentifiers)} are not available.");

            var activeCount = await unitOfWork.Borrowings.GetActiveCountByStudentAsync(studentId, cancellationToken);
            if (activeCount + requestedIds.Length > BorrowingRules.MaxActiveBorrowings)
            {
                var borrowingWord = activeCount == 1 ? "borrowing" : "borrowings";
                return OperationResult<IReadOnlyList<BorrowingDetails>>.Rejected(
                    $"{student.Name} has {activeCount} active {borrowingWord}; checking out {requestedIds.Length} more would exceed the limit of {BorrowingRules.MaxActiveBorrowings}.");
            }

            var borrowings = new List<Borrowing>(requestedIds.Length);
            foreach (var equipmentId in requestedIds)
            {
                var borrowing = new Borrowing(0, studentId, equipmentId, today, expectedReturnDate);
                borrowings.Add(borrowing);
                equipmentById[equipmentId].IsAvailable = false;
            }

            unitOfWork.Borrowings.AddRange(borrowings, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            var details = borrowings.Select(borrowing =>
            {
                var item = equipmentById[borrowing.EquipmentId];
                return new BorrowingDetails(
                    borrowing.Id,
                    student.Id,
                    student.StudentNumber,
                    student.Name,
                    item.Id,
                    item.Identifier,
                    item.Name,
                    borrowing.BorrowDate,
                    borrowing.ExpectedReturnDate,
                    borrowing.ReturnDate,
                    borrowing.Status,
                    borrowing.Version);
            }).ToArray();

            return OperationResult<IReadOnlyList<BorrowingDetails>>.Succeeded(
                details,
                $"{requestedIds.Length} items checked out for {student.StudentNumber} — {student.Name}.");
        }
        catch (CommitOutcomeUnknownException)
        {
            return await VerifyUnknownCheckoutAsync(studentId, requestedIds, today, cancellationToken);
        }
        catch (PersistenceConflictException exception) when (
            exception.Kind == PersistenceConflictKind.Concurrency ||
            exception.ConstraintName == "UX_Borrowings_OneActivePerEquipment")
        {
            return OperationResult<IReadOnlyList<BorrowingDetails>>.Stale(StaleMessage);
        }
        catch (PersistenceConflictException)
        {
            return OperationResult<IReadOnlyList<BorrowingDetails>>.Failed(
                "The checkout could not be saved. No changes were committed.");
        }
        catch (PersistenceFailureException)
        {
            return OperationResult<IReadOnlyList<BorrowingDetails>>.Failed(
                "The checkout could not be saved. No changes were committed.");
        }
    }

    private async Task<OperationResult<IReadOnlyList<BorrowingDetails>>> VerifyUnknownCheckoutAsync(
        int studentId,
        IReadOnlyCollection<int> requestedIds,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var verificationUnit = _unitOfWorkFactory.Create();
            var activeBorrowings = await verificationUnit.Borrowings.QueryDetailsAsync(
                new BorrowingQuery { StudentId = studentId, Status = BorrowingStatus.Active },
                cancellationToken);
            var requestedBorrowings = activeBorrowings
                .Where(borrowing => requestedIds.Contains(borrowing.EquipmentId))
                .ToArray();

            if (requestedBorrowings.Length == 0)
                return OperationResult<IReadOnlyList<BorrowingDetails>>.Failed(
                    "The checkout was verified as not committed. You may refresh and retry.");

            if (requestedBorrowings.Length == requestedIds.Count &&
                requestedBorrowings.All(borrowing => borrowing.BorrowDate == today))
                return OperationResult<IReadOnlyList<BorrowingDetails>>.Succeeded(
                    requestedBorrowings,
                    "The checkout was verified as committed.");

            return OperationResult<IReadOnlyList<BorrowingDetails>>.UnknownOutcome(
                "The result could not be verified. Refresh and check the borrowings before retrying.");
        }
        catch (Exception)
        {
            return OperationResult<IReadOnlyList<BorrowingDetails>>.UnknownOutcome(
                "The result could not be verified. Refresh and check the borrowings before retrying.");
        }
    }
}
