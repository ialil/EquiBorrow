using EquiBorrow.Application.Interfaces;
using EquiBorrow.Domain;

namespace EquiBorrow.UI.ViewModels;

public sealed class BorrowingHistoryItemViewModel
{
    public int BorrowingId { get; init; }
    public string RelatedIdentity { get; init; } = string.Empty;
    public DateOnly BorrowDate { get; init; }
    public DateOnly ExpectedReturnDate { get; init; }
    public BorrowingStatus Status { get; init; }
    public DateOnly? ReturnDate { get; init; }
    public string StatusText => Status == BorrowingStatus.Active ? "Active" : "Returned";
    public string ReturnDateText => ReturnDate?.ToString("yyyy-MM-dd") ?? string.Empty;
    public bool HasReturnDate => ReturnDate.HasValue;

    public static BorrowingHistoryItemViewModel ForStudentHistory(BorrowingDetails details) => new()
    {
        BorrowingId = details.BorrowingId,
        RelatedIdentity = $"{details.EquipmentIdentifier} — {details.EquipmentName}",
        BorrowDate = details.BorrowDate,
        ExpectedReturnDate = details.ExpectedReturnDate,
        Status = details.Status,
        ReturnDate = details.ReturnDate
    };

    public static BorrowingHistoryItemViewModel ForEquipmentHistory(BorrowingDetails details) => new()
    {
        BorrowingId = details.BorrowingId,
        RelatedIdentity = $"{details.StudentNumber} — {details.StudentName}",
        BorrowDate = details.BorrowDate,
        ExpectedReturnDate = details.ExpectedReturnDate,
        Status = details.Status,
        ReturnDate = details.ReturnDate
    };
}
