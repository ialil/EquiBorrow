using System;

namespace EquiBorrow.Domain;

public class Borrowing
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int EquipmentId { get; set; }
    public DateOnly BorrowDate { get; set; }
    public DateOnly ExpectedReturnDate { get; set; }
    public BorrowingStatus Status { get; set; }
    public DateOnly? ReturnDate { get; set; }

    public Borrowing() { }

    public Borrowing(int id, int studentId, int equipmentId, DateOnly borrowDate, DateOnly expectedReturnDate)
    {
        if (expectedReturnDate <= borrowDate)
            throw new ArgumentException("Expected return date must be later than the checkout date.", nameof(expectedReturnDate));

        Id = id;
        StudentId = studentId;
        EquipmentId = equipmentId;
        BorrowDate = borrowDate;
        ExpectedReturnDate = expectedReturnDate;
        Status = BorrowingStatus.Active;
        ReturnDate = null;
    }

    public void MarkAsReturned(DateOnly returnDate)
    {
        if (Status != BorrowingStatus.Active)
            throw new InvalidOperationException("Only an active borrowing can be returned.");
        if (returnDate < BorrowDate)
            throw new InvalidOperationException("Return date cannot precede the checkout date.");

        Status = BorrowingStatus.Returned;
        ReturnDate = returnDate;
    }
}