using System;
using System.Collections.Generic;
using System.Text;

using System;

namespace EquiBorrow.Domain;

public class Borrowing
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int EquipmentId { get; set; }
    public DateTime BorrowDate { get; set; }
    public DateTime ExpectedReturnDate { get; set; }
    public BorrowingStatus Status { get; set; }
    public DateTime? ReturnDate { get; set; }

    public Borrowing(int id, int studentId, int equipmentId, DateTime borrowDate, DateTime expectedReturnDate)
    {
        Id = id;
        StudentId = studentId;
        EquipmentId = equipmentId;
        BorrowDate = borrowDate;
        ExpectedReturnDate = expectedReturnDate;
        Status = BorrowingStatus.Active;
        ReturnDate = null;
    }

    public void MarkAsReturned()
    {
        Status = BorrowingStatus.Returned;
        ReturnDate = DateTime.Now;
    }
}
