using EquiBorrow.Application.Interfaces;
using EquiBorrow.Domain;

namespace EquiBorrow.UI.ViewModels;

public sealed class BorrowingSearchItemViewModel
{
    public BorrowingSearchItemViewModel(BorrowingDetails details)
    {
        StudentNumber = details.StudentNumber;
        StudentName = details.StudentName;
        EquipmentIdentifier = details.EquipmentIdentifier;
        EquipmentName = details.EquipmentName;
        BorrowDate = details.BorrowDate;
        ExpectedReturnDate = details.ExpectedReturnDate;
        StatusText = details.Status == BorrowingStatus.Active ? "Active" : "Returned";
        ReturnDateText = details.ReturnDate?.ToString("yyyy-MM-dd") ?? string.Empty;
        HasReturnDate = details.ReturnDate.HasValue;
    }

    public string StudentNumber { get; }
    public string StudentName { get; }
    public string EquipmentIdentifier { get; }
    public string EquipmentName { get; }
    public DateOnly BorrowDate { get; }
    public DateOnly ExpectedReturnDate { get; }
    public string StatusText { get; }
    public string ReturnDateText { get; }
    public bool HasReturnDate { get; }
}
