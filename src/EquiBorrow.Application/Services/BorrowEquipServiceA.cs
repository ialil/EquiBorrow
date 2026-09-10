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
        // validate student
        var student = await _studentRepository.GetByIdAsync(studentId, cancellationToken);
        if (student == null)
            throw new InvalidOperationException("Student does not exist.");
        if (!student.IsActive)
            throw new InvalidOperationException("Student is not allowed to borrow equipment.");

        // validate equipment
        var equipment = await _equipmentRepository.GetByIdAsync(equipmentId, cancellationToken);
        if (equipment == null)
            throw new InvalidOperationException("Equipment does not exist.");
        if (!equipment.IsAvailable)
            throw new InvalidOperationException("Equipment is currently unavailable.");

        // check limit
        var activeCount = await _borrowingRepository.GetActiveCountByStudentIdAsync(studentId, cancellationToken);
        if (activeCount >= MaxActiveBorrowings)
            throw new InvalidOperationException($"Student already has max {MaxActiveBorrowings} active borrowings.");

        // create borrowing
        var borrowing = new Borrowing(
            id: 0,
            studentId: studentId,
            equipmentId: equipmentId,
            borrowDate: DateTime.Now,
            expectedReturnDate: DateTime.Now.AddDays(7)
        );

        // mark equipment unavailable
        equipment.IsAvailable = false;

        // save
        await _borrowingRepository.AddAsync(borrowing, cancellationToken);
        await _equipmentRepository.UpdateAsync(equipment, cancellationToken);

        return borrowing;
    }
}