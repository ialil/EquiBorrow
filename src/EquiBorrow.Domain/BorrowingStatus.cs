namespace EquiBorrow.Domain;

// Stored as an integer. The partial unique index depends on Active = 0.
public enum BorrowingStatus
{
    Active = 0,
    Returned = 1
}