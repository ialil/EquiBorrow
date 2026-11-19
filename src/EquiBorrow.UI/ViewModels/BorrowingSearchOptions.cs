using EquiBorrow.Application.Interfaces;
using EquiBorrow.Domain;

namespace EquiBorrow.UI.ViewModels;

public sealed record BorrowingStatusOption(string Label, BorrowingStatus? Value)
{
    public override string ToString() => Label;
}

public sealed record BorrowingSortOption(string Label, BorrowingSortField Value)
{
    public override string ToString() => Label;
}

public sealed record BorrowingDirectionOption(string Label, SortDirection Value)
{
    public override string ToString() => Label;
}
