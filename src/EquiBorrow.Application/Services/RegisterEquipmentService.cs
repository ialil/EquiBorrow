using System;
using System.Threading;
using System.Threading.Tasks;
using EquiBorrow.Application.Interfaces;
using EquiBorrow.Domain;

namespace EquiBorrow.Application.Services;

public sealed class RegisterEquipmentService
{
    private const string UniqueConstraint = "UX_Equipment_IdentifierKey";
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public RegisterEquipmentService(IUnitOfWorkFactory unitOfWorkFactory) => _unitOfWorkFactory = unitOfWorkFactory;

    public async Task<OperationResult<Equipment>> ExecuteAsync(
        string? identifier,
        string? name,
        string? description,
        CancellationToken cancellationToken = default)
    {
        var normalizedIdentifier = identifier?.Trim() ?? string.Empty;
        var normalizedName = name?.Trim() ?? string.Empty;
        var normalizedDescription = NormalizeOptional(description);

        if (normalizedIdentifier.Length == 0)
            return OperationResult<Equipment>.Rejected("Equipment identifier is required.");
        if (normalizedIdentifier.Length > 50)
            return OperationResult<Equipment>.Rejected("Equipment identifier must be 50 characters or fewer.");
        if (normalizedName.Length == 0)
            return OperationResult<Equipment>.Rejected("Name is required.");
        if (normalizedName.Length > 200)
            return OperationResult<Equipment>.Rejected("Name must be 200 characters or fewer.");
        if (normalizedDescription?.Length > 500)
            return OperationResult<Equipment>.Rejected("Description must be 500 characters or fewer.");

        var duplicateMessage = $"Equipment with identifier {normalizedIdentifier} already exists.";
        try
        {
            await using var unitOfWork = _unitOfWorkFactory.Create();
            if (await unitOfWork.Equipment.ExistsByIdentifierAsync(normalizedIdentifier, cancellationToken: cancellationToken))
                return OperationResult<Equipment>.Rejected(duplicateMessage);

            var equipment = new Equipment
            {
                Identifier = normalizedIdentifier,
                Name = normalizedName,
                Description = normalizedDescription,
                IsAvailable = true
            };
            unitOfWork.Equipment.Add(equipment, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
            return OperationResult<Equipment>.Succeeded(equipment,
                $"Equipment {equipment.Identifier} — {equipment.Name} registered successfully.");
        }
        catch (CommitOutcomeUnknownException)
        {
            return await VerifyUnknownOutcomeAsync(normalizedIdentifier, normalizedName, cancellationToken);
        }
        catch (PersistenceConflictException exception) when (
            exception.Kind == PersistenceConflictKind.Duplicate && exception.ConstraintName == UniqueConstraint)
        {
            return OperationResult<Equipment>.Rejected(duplicateMessage);
        }
        catch (PersistenceConflictException exception) when (exception.Kind == PersistenceConflictKind.Concurrency)
        {
            return OperationResult<Equipment>.Stale("Equipment data changed during registration. Refresh and retry.");
        }
        catch (PersistenceConflictException)
        {
            return OperationResult<Equipment>.Failed("The equipment could not be registered because of a database conflict.");
        }
        catch (PersistenceFailureException)
        {
            return OperationResult<Equipment>.Failed("The equipment could not be registered. No changes were saved.");
        }
    }

    private async Task<OperationResult<Equipment>> VerifyUnknownOutcomeAsync(
        string normalizedIdentifier,
        string normalizedName,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var verificationUnit = _unitOfWorkFactory.Create();
            var equipmentItems = await verificationUnit.Equipment.GetAllAsync(cancellationToken);
            foreach (var equipment in equipmentItems)
            {
                if (string.Equals(equipment.Identifier.Trim(), normalizedIdentifier, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(equipment.Name, normalizedName, StringComparison.Ordinal))
                {
                    return OperationResult<Equipment>.Succeeded(equipment,
                        $"Equipment {equipment.Identifier} — {equipment.Name} was verified as registered.");
                }
            }

            return OperationResult<Equipment>.Failed("The equipment registration was verified as not saved. You may refresh and retry.");
        }
        catch (Exception)
        {
            return OperationResult<Equipment>.UnknownOutcome(
                "The registration result could not be verified. Refresh the equipment list before retrying.");
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }
}
