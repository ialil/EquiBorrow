using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EquiBorrow.Application.Interfaces;
using EquiBorrow.Domain;

namespace EquiBorrow.Application.Services;

public sealed class BorrowEquipmentService
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;
    private readonly TimeProvider _timeProvider;

    public BorrowEquipmentService(IUnitOfWorkFactory unitOfWorkFactory, TimeProvider timeProvider)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
        _timeProvider = timeProvider;
    }

    public async Task<OperationResult<BorrowingDetails>> ExecuteAsync(
        int studentId,
        int equipmentId,
        DateOnly expectedReturnDate,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);

        try
        {
            await using var unitOfWork = _unitOfWorkFactory.Create();
            var student = await unitOfWork.Students.GetByIdForUpdateAsync(studentId, cancellationToken);
            if (student is null)
                return OperationResult<BorrowingDetails>.Rejected("Student was not found.");
            if (!student.IsActive)
                return OperationResult<BorrowingDetails>.Rejected($"{student.StudentNumber} — {student.Name} is restricted from borrowing.");

            var equipment = await unitOfWork.Equipment.GetByIdForUpdateAsync(equipmentId, cancellationToken);
            if (equipment is null)
                return OperationResult<BorrowingDetails>.Rejected("Equipment was not found.");
            if (!equipment.IsAvailable)
                return OperationResult<BorrowingDetails>.Rejected("Equipment is currently unavailable.");

            if (expectedReturnDate <= today)
                return OperationResult<BorrowingDetails>.Rejected("Expected return date must be later than today.");

            var activeCount = await unitOfWork.Borrowings.GetActiveCountByStudentAsync(studentId, cancellationToken);
            if (activeCount >= BorrowingRules.MaxActiveBorrowings)
                return OperationResult<BorrowingDetails>.Rejected($"Student already has the maximum of {BorrowingRules.MaxActiveBorrowings} active borrowings.");

            var borrowing = new Borrowing(0, studentId, equipmentId, today, expectedReturnDate);
            equipment.IsAvailable = false;
            unitOfWork.Borrowings.Add(borrowing, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            var details = new BorrowingDetails(
                borrowing.Id,
                student.Id,
                student.StudentNumber,
                student.Name,
                equipment.Id,
                equipment.Identifier,
                equipment.Name,
                borrowing.BorrowDate,
                borrowing.ExpectedReturnDate,
                borrowing.ReturnDate,
                borrowing.Status,
                borrowing.Version);
            return OperationResult<BorrowingDetails>.Succeeded(details, "Equipment borrowed successfully.");
        }
        catch (CommitOutcomeUnknownException)
        {
            return await VerifyUnknownBorrowAsync(studentId, equipmentId, today, cancellationToken);
        }
        catch (PersistenceConflictException exception) when (
            exception.Kind == PersistenceConflictKind.Concurrency ||
            exception.ConstraintName == "UX_Borrowings_OneActivePerEquipment")
        {
            return OperationResult<BorrowingDetails>.Stale("The item was just borrowed or changed. Refresh the data before retrying.");
        }
        catch (PersistenceConflictException)
        {
            return OperationResult<BorrowingDetails>.Failed("The borrow could not be saved because a conflicting record exists.");
        }
        catch (PersistenceFailureException)
        {
            return OperationResult<BorrowingDetails>.Failed("The borrow could not be saved. No changes were committed.");
        }
    }

    private async Task<OperationResult<BorrowingDetails>> VerifyUnknownBorrowAsync(
        int studentId,
        int equipmentId,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var verificationUnit = _unitOfWorkFactory.Create();
            var activeBorrowing = (await verificationUnit.Borrowings.GetActiveDetailsAsync(cancellationToken))
                .FirstOrDefault(borrowing =>
                    borrowing.StudentId == studentId &&
                    borrowing.EquipmentId == equipmentId &&
                    borrowing.BorrowDate == today);
            if (activeBorrowing is not null)
                return OperationResult<BorrowingDetails>.Succeeded(activeBorrowing, "The borrow was verified as committed.");

            return OperationResult<BorrowingDetails>.Failed("The borrow was verified as not committed. You may refresh and retry.");
        }
        catch (Exception)
        {
            return OperationResult<BorrowingDetails>.UnknownOutcome(
                "The commit result could not be verified. Refresh and check the borrowing before retrying.");
        }
    }
}
