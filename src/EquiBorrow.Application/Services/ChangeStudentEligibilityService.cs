using System.Threading;
using System.Threading.Tasks;
using EquiBorrow.Application.Interfaces;
using EquiBorrow.Domain;

namespace EquiBorrow.Application.Services;

public sealed class ChangeStudentEligibilityService
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public ChangeStudentEligibilityService(IUnitOfWorkFactory unitOfWorkFactory) => _unitOfWorkFactory = unitOfWorkFactory;

    public async Task<OperationResult<Student>> ExecuteAsync(
        int studentId,
        uint expectedVersion,
        bool isEligible,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var unitOfWork = _unitOfWorkFactory.Create();
            var student = await unitOfWork.Students.GetByIdForUpdateAsync(studentId, cancellationToken);
            if (student is null)
                return OperationResult<Student>.Stale("Student no longer exists. The list was refreshed.");
            if (student.Version != expectedVersion)
                return OperationResult<Student>.Stale("Student was changed by someone else. The list was refreshed.");

            var requestedState = isEligible ? "eligible to borrow" : "restricted from borrowing";
            if (student.IsActive == isEligible)
            {
                var alreadyState = isEligible ? "already eligible to borrow" : "already restricted";
                return OperationResult<Student>.Succeeded(student,
                    $"{student.StudentNumber} — {student.Name} is {alreadyState}. No changes made.");
            }

            student.IsActive = isEligible;
            await unitOfWork.CommitAsync(cancellationToken);
            return OperationResult<Student>.Succeeded(student,
                $"{student.StudentNumber} — {student.Name} is now {requestedState}.");
        }
        catch (CommitOutcomeUnknownException)
        {
            return await VerifyUnknownOutcomeAsync(studentId, isEligible, cancellationToken);
        }
        catch (PersistenceConflictException exception) when (exception.Kind == PersistenceConflictKind.Concurrency)
        {
            return OperationResult<Student>.Stale("Student was changed by someone else. The list was refreshed.");
        }
        catch (PersistenceConflictException)
        {
            return OperationResult<Student>.Failed("The student eligibility could not be changed because of a database conflict. No changes were saved.");
        }
        catch (PersistenceFailureException)
        {
            return OperationResult<Student>.Failed("The student eligibility could not be changed. No changes were saved.");
        }
    }

    private async Task<OperationResult<Student>> VerifyUnknownOutcomeAsync(
        int studentId,
        bool requestedEligibility,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var verificationUnit = _unitOfWorkFactory.Create();
            var student = await verificationUnit.Students.GetByIdAsync(studentId, cancellationToken);
            if (student is not null && student.IsActive == requestedEligibility)
            {
                var verifiedState = requestedEligibility ? "eligible to borrow" : "restricted from borrowing";
                return OperationResult<Student>.Succeeded(student,
                    $"{student.StudentNumber} — {student.Name} was verified as {verifiedState}.");
            }

            return OperationResult<Student>.Failed("The student eligibility change was verified as not saved. Refresh and retry.");
        }
        catch (System.Exception)
        {
            return OperationResult<Student>.UnknownOutcome(
                "The change result could not be verified. Refresh the student list before retrying.");
        }
    }
}
