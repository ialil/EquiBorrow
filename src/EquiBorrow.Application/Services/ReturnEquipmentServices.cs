using System;
using System.Threading;
using System.Threading.Tasks;
using EquiBorrow.Application.Interfaces;
using EquiBorrow.Domain;
namespace EquiBorrow.Application.Services;
public class ReturnEquipmentServices
{
    private readonly IBorrowingRepository _borrowingRepo;
    private readonly IEquipmentRepository _equipmentRepo;
    public ReturnEquipmentServices(
        IBorrowingRepository borrowingRepo,
        IEquipmentRepository equipmentRepo)
    {
        _borrowingRepo = borrowingRepo;
        _equipmentRepo = equipmentRepo;
    }
    public async Task<Borrowing> ExecuteAsync(int borrowingId, CancellationToken cancellationToken = default)
    {
        var borrowing = await _borrowingRepo.GetByIdAsync(borrowingId, cancellationToken);
        if (borrowing == null)
            throw new InvalidOperationException("Borrowing not found.");
        if (borrowing.Status == BorrowingStatus.Returned)
            throw new InvalidOperationException("Already returned.");
        var equipment = await _equipmentRepo.GetByIdAsync(borrowing.EquipmentId, cancellationToken);
        if (equipment == null)
            throw new InvalidOperationException("Equipment not found.");
        borrowing.MarkAsReturned();
        equipment.IsAvailable = true;
        await _equipmentRepo.UpdateAsync(equipment, cancellationToken);
        return borrowing;
    }
}