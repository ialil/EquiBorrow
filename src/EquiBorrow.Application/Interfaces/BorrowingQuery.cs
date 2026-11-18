using EquiBorrow.Domain;

namespace EquiBorrow.Application.Interfaces;

public enum BorrowingSortField
{
    BorrowDate,
    ExpectedReturnDate
}

public enum SortDirection
{
    Ascending,
    Descending
}

public sealed record BorrowingQuery
{
    public string? SearchText { get; init; }
    public BorrowingStatus? Status { get; init; }
    public BorrowingSortField SortField { get; init; } = BorrowingSortField.BorrowDate;
    public SortDirection SortDirection { get; init; } = SortDirection.Descending;
}
