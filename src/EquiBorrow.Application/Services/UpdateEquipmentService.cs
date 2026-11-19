using System;
using System.Threading;
using System.Threading.Tasks;
using EquiBorrow.Application.Interfaces;
using EquiBorrow.Domain;

namespace EquiBorrow.Application.Services;

public sealed class UpdateEquipmentService
{
    private const string UniqueConstraint = "UX_Equipment_IdentifierKey";
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public UpdateEquipmentService(IUnitOfWorkFactory unitOfWorkFactory) => _unitOfWorkFactory = unitOfWorkFactory;

    public async Task<OperationResult<Equipment>> ExecuteAsync(
        int equipmentId,
        uint expectedVersion,
        string? identifier,
        string? name,
        string? description,
        CancellationToken cancellationToken = default)
    {
        var normalizedIdentifier = string.Empty;
        var normalizedName = string.Empty;
        string? normalizedDescription = null;

        try
        {
            await using var unitOfWork = _unitOfWorkFactory.Create();
            var equipment = await unitOfWork.Equipment.GetByIdForUpdateAsync(equipmentId, cancellationToken);
            if (equipment is null)
                return OperationResult<Equipment>.Stale("Equipment no longer exists. The list was refreshed.");
            if (equipment.Version != expectedVersion)
                return OperationResult<Equipment>.Stale("Equipment was changed by someone else. The list was refreshed.");

            normalizedIdentifier = identifier?.Trim() ?? string.Empty;
            normalizedName = name?.Trim() ?? string.Empty;
            normalizedDescription = NormalizeOptional(description);

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
            if (await unitOfWork.Equipment.ExistsByIdentifierAsync(normalizedIdentifier, equipment.Id, cancellationToken))
                return OperationResult<Equipment>.Rejected(duplicateMessage);

            if (equipment.Identifier == normalizedIdentifier && equipment.Name == normalizedName && equipment.Description == normalizedDescription)
                return OperationResult<Equipment>.Succeeded(equipment, "No changes to save.");

            equipment.Identifier = normalizedIdentifier;
            equipment.Name = normalizedName;
            equipment.Description = normalizedDescription;
            await unitOfWork.CommitAsync(cancellationToken);
            return OperationResult<Equipment>.Succeeded(equipment,
                $"Equipment {equipment.Identifier} — {equipment.Name} updated successfully.");
        }
        catch (CommitOutcomeUnknownException)
        {
            return await VerifyUnknownOutcomeAsync(equipmentId, normalizedIdentifier, normalizedName, normalizedDescription, cancellationToken);
        }
        catch (PersistenceConflictException exception) when (
            exception.Kind == PersistenceConflictKind.Duplicate && exception.ConstraintName == UniqueConstraint)
        {
            return OperationResult<Equipment>.Rejected($"Equipment with identifier {normalizedIdentifier} already exists.");
        }
        catch (PersistenceConflictException exception) when (exception.Kind == PersistenceConflictKind.Concurrency)
        {
            return OperationResult<Equipment>.Stale("Equipment was changed by someone else. The list was refreshed.");
        }
        catch (PersistenceConflictException)
        {
            return OperationResult<Equipment>.Failed("The equipment could not be updated because of a database conflict. No changes were saved.");
        }
        catch (PersistenceFailureException)
        {
            return OperationResult<Equipment>.Failed("The equipment could not be updated. No changes were saved.");
        }
    }

    private async Task<OperationResult<Equipment>> VerifyUnknownOutcomeAsync(
        int equipmentId,
        string identifier,
        string name,
        string? description,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var verificationUnit = _unitOfWorkFactory.Create();
            var equipment = await verificationUnit.Equipment.GetByIdAsync(equipmentId, cancellationToken);
            if (equipment is not null && equipment.Identifier == identifier && equipment.Name == name && equipment.Description == description)
                return OperationResult<Equipment>.Succeeded(equipment,
                    $"Equipment {equipment.Identifier} — {equipment.Name} was verified as updated.");

            return OperationResult<Equipment>.Failed("The equipment update was verified as not saved. Refresh and retry.");
        }
        catch (Exception)
        {
            return OperationResult<Equipment>.UnknownOutcome(
                "The update result could not be verified. Refresh the equipment list before retrying.");
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }
}
