using System;
using System.Threading;
using System.Threading.Tasks;
using EquiBorrow.Application.Interfaces;
using EquiBorrow.Domain;

namespace EquiBorrow.Application.Services;

public sealed class UpdateStudentService
{
    private const string UniqueConstraint = "UX_Students_StudentNumberKey";
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public UpdateStudentService(IUnitOfWorkFactory unitOfWorkFactory) => _unitOfWorkFactory = unitOfWorkFactory;

    public async Task<OperationResult<Student>> ExecuteAsync(
        int studentId,
        uint expectedVersion,
        string? studentNumber,
        string? fullName,
        string? contact,
        CancellationToken cancellationToken = default)
    {
        var normalizedNumber = string.Empty;
        var normalizedName = string.Empty;
        string? normalizedContact = null;

        try
        {
            await using var unitOfWork = _unitOfWorkFactory.Create();
            var student = await unitOfWork.Students.GetByIdForUpdateAsync(studentId, cancellationToken);
            if (student is null)
                return OperationResult<Student>.Stale("Student no longer exists. The list was refreshed.");
            if (student.Version != expectedVersion)
                return OperationResult<Student>.Stale("Student was changed by someone else. The list was refreshed.");

            normalizedNumber = studentNumber?.Trim() ?? string.Empty;
            normalizedName = fullName?.Trim() ?? string.Empty;
            normalizedContact = NormalizeOptional(contact);

            if (normalizedNumber.Length == 0)
                return OperationResult<Student>.Rejected("Student number is required.");
            if (normalizedNumber.Length > 50)
                return OperationResult<Student>.Rejected("Student number must be 50 characters or fewer.");
            if (normalizedName.Length == 0)
                return OperationResult<Student>.Rejected("Full name is required.");
            if (normalizedName.Length > 200)
                return OperationResult<Student>.Rejected("Full name must be 200 characters or fewer.");
            if (normalizedContact?.Length > 200)
                return OperationResult<Student>.Rejected("Contact must be 200 characters or fewer.");

            var duplicateMessage = $"A student with number {normalizedNumber} already exists.";
            if (await unitOfWork.Students.ExistsByNumberAsync(normalizedNumber, student.Id, cancellationToken))
                return OperationResult<Student>.Rejected(duplicateMessage);

            if (student.StudentNumber == normalizedNumber && student.Name == normalizedName && student.Contact == normalizedContact)
                return OperationResult<Student>.Succeeded(student, "No changes to save.");

            student.StudentNumber = normalizedNumber;
            student.Name = normalizedName;
            student.Contact = normalizedContact;
            await unitOfWork.CommitAsync(cancellationToken);
            return OperationResult<Student>.Succeeded(student,
                $"Student {student.StudentNumber} — {student.Name} updated successfully.");
        }
        catch (CommitOutcomeUnknownException)
        {
            return await VerifyUnknownOutcomeAsync(studentId, normalizedNumber, normalizedName, normalizedContact, cancellationToken);
        }
        catch (PersistenceConflictException exception) when (
            exception.Kind == PersistenceConflictKind.Duplicate && exception.ConstraintName == UniqueConstraint)
        {
            return OperationResult<Student>.Rejected($"A student with number {normalizedNumber} already exists.");
        }
        catch (PersistenceConflictException exception) when (exception.Kind == PersistenceConflictKind.Concurrency)
        {
            return OperationResult<Student>.Stale("Student was changed by someone else. The list was refreshed.");
        }
        catch (PersistenceConflictException)
        {
            return OperationResult<Student>.Failed("The student could not be updated because of a database conflict. No changes were saved.");
        }
        catch (PersistenceFailureException)
        {
            return OperationResult<Student>.Failed("The student could not be updated. No changes were saved.");
        }
    }

    private async Task<OperationResult<Student>> VerifyUnknownOutcomeAsync(
        int studentId,
        string studentNumber,
        string name,
        string? contact,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var verificationUnit = _unitOfWorkFactory.Create();
            var student = await verificationUnit.Students.GetByIdAsync(studentId, cancellationToken);
            if (student is not null && student.StudentNumber == studentNumber && student.Name == name && student.Contact == contact)
                return OperationResult<Student>.Succeeded(student,
                    $"Student {student.StudentNumber} — {student.Name} was verified as updated.");

            return OperationResult<Student>.Failed("The student update was verified as not saved. Refresh and retry.");
        }
        catch (Exception)
        {
            return OperationResult<Student>.UnknownOutcome(
                "The update result could not be verified. Refresh the student list before retrying.");
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }
}
