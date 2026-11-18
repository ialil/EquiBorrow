using System;
using System.Threading;
using System.Threading.Tasks;
using EquiBorrow.Application.Interfaces;
using EquiBorrow.Domain;

namespace EquiBorrow.Application.Services;

public sealed class RegisterStudentService
{
    private const string UniqueConstraint = "UX_Students_StudentNumberKey";
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public RegisterStudentService(IUnitOfWorkFactory unitOfWorkFactory) => _unitOfWorkFactory = unitOfWorkFactory;

    public async Task<OperationResult<Student>> ExecuteAsync(
        string? studentNumber,
        string? fullName,
        string? contact,
        CancellationToken cancellationToken = default)
    {
        var normalizedNumber = studentNumber?.Trim() ?? string.Empty;
        var normalizedName = fullName?.Trim() ?? string.Empty;
        var normalizedContact = NormalizeOptional(contact);

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
        try
        {
            await using var unitOfWork = _unitOfWorkFactory.Create();
            if (await unitOfWork.Students.ExistsByNumberAsync(normalizedNumber, cancellationToken: cancellationToken))
                return OperationResult<Student>.Rejected(duplicateMessage);

            var student = new Student
            {
                StudentNumber = normalizedNumber,
                Name = normalizedName,
                Contact = normalizedContact,
                IsActive = true
            };
            unitOfWork.Students.Add(student, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
            return OperationResult<Student>.Succeeded(student, $"Student {student.StudentNumber} — {student.Name} registered successfully.");
        }
        catch (CommitOutcomeUnknownException)
        {
            return await VerifyUnknownOutcomeAsync(normalizedNumber, normalizedName, cancellationToken);
        }
        catch (PersistenceConflictException exception) when (
            exception.Kind == PersistenceConflictKind.Duplicate && exception.ConstraintName == UniqueConstraint)
        {
            return OperationResult<Student>.Rejected(duplicateMessage);
        }
        catch (PersistenceConflictException exception) when (exception.Kind == PersistenceConflictKind.Concurrency)
        {
            return OperationResult<Student>.Stale("Student data changed during registration. Refresh and retry.");
        }
        catch (PersistenceConflictException)
        {
            return OperationResult<Student>.Failed("The student could not be registered because of a database conflict.");
        }
        catch (PersistenceFailureException)
        {
            return OperationResult<Student>.Failed("The student could not be registered. No changes were saved.");
        }
    }

    private async Task<OperationResult<Student>> VerifyUnknownOutcomeAsync(
        string normalizedNumber,
        string normalizedName,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var verificationUnit = _unitOfWorkFactory.Create();
            var students = await verificationUnit.Students.GetAllAsync(cancellationToken);
            foreach (var student in students)
            {
                if (string.Equals(student.StudentNumber.Trim(), normalizedNumber, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(student.Name, normalizedName, StringComparison.Ordinal))
                {
                    return OperationResult<Student>.Succeeded(student,
                        $"Student {student.StudentNumber} — {student.Name} was verified as registered.");
                }
            }

            return OperationResult<Student>.Failed("The student registration was verified as not saved. You may refresh and retry.");
        }
        catch (Exception)
        {
            return OperationResult<Student>.UnknownOutcome(
                "The registration result could not be verified. Refresh the student list before retrying.");
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }
}
