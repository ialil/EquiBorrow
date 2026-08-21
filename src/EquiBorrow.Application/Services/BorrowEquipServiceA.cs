using System;
using System.Threading;
using System.Threading.Tasks;
using EquiBorrow.Application.Interfaces;
using EquiBorrow.Domain;

namespace EquiBorrow.Application.Services;

public class BorrowEquipmentService
{
    private readonly IStudentRepository _studentRepository;
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly IBorrowingRepository _borrowingRepository;
    private const int MaxActiveBorrowings = 3;

    public BorrowEquipmentService(
        IStudentRepository studentRepository,
        IEquipmentRepository equipmentRepository,
        IBorrowingRepository borrowingRepository)
    {
        _studentRepository = studentRepository;
        _equipmentRepository = equipmentRepository;
        _borrowingRepository = borrowingRepository;
    }

    public async Task<Borrowing> ExecuteAsync(int studentId, int equipmentId, CancellationToken cancellationToken = default)
    {
        // 1. Validate Student
        var student = await _studentRepository.GetByIdAsync(studentId, cancellationToken);
        if (student == null)
            throw new InvalidOperationException("Student does not exist.");
        if (!student.IsActive)
            throw new InvalidOperationException("Student is not allowed to borrow equipment.");

        // 2. Validate Equipment
        var equipment = await _equipmentRepository.GetByIdAsync(equipmentId, cancellationToken);
        if (equipment == null)
            throw new InvalidOperationException("Equipment does not exist.");
        if (!equipment.IsAvailable)
            throw new InvalidOperationException("Equipment is currently unavailable.");

        // 3. Check Borrowing Limit
        var activeCount = await _borrowingRepository.GetActiveCountByStudentIdAsync(studentId, cancellationToken);
        if (activeCount >= MaxActiveBorrowings)
            throw new InvalidOperationException($"Student already has max {MaxActiveBorrowings} active borrowings.");

        // 4. Create Borrowing Record
        var borrowing = new Borrowing(
            id: 0,
            studentId: studentId,
            equipmentId: equipmentId,
            borrowDate: DateTime.Now,
            expectedReturnDate: DateTime.Now.AddDays(7)
        );

        // 5. Mark Equipment as Unavailable
        equipment.IsAvailable = false;

        // 6. Save Changes
        await _borrowingRepository.AddAsync(borrowing, cancellationToken);
        await _equipmentRepository.UpdateAsync(equipment, cancellationToken);

        return borrowing;
    }
}