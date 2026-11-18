using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EquiBorrow.Application.Interfaces;
using EquiBorrow.Domain;
using EquiBorrow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EquiBorrow.Infrastructure.Repositories;

public sealed class EfBorrowingRepository : IBorrowingRepository
{
    private readonly EquipmentBorrowingDbContext _db;

    public EfBorrowingRepository(EquipmentBorrowingDbContext db) => _db = db;

    public void Add(Borrowing borrowing, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _db.Borrowings.Add(borrowing);
    }

    public void AddRange(IEnumerable<Borrowing> borrowings, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _db.Borrowings.AddRange(borrowings);
    }

    public Task<Borrowing?> GetByIdForUpdateAsync(int id, CancellationToken cancellationToken = default) =>
        EfPersistenceExecutor.ExecuteAsync(() =>
            _db.Borrowings.SingleOrDefaultAsync(borrowing => borrowing.Id == id, cancellationToken));

    public Task<int> GetActiveCountByStudentAsync(int studentId, CancellationToken cancellationToken = default) =>
        EfPersistenceExecutor.ExecuteAsync(() =>
            _db.Borrowings.CountAsync(
                borrowing => borrowing.StudentId == studentId && borrowing.Status == BorrowingStatus.Active,
                cancellationToken));

    public Task<IReadOnlyList<BorrowingDetails>> GetActiveDetailsAsync(CancellationToken cancellationToken = default) =>
        MaterializeDetailsAsync(
            GetDetailsQuery()
                .Where(row => row.Borrowing.Status == BorrowingStatus.Active)
                .OrderByDescending(row => row.Borrowing.BorrowDate)
                .ThenBy(row => row.Borrowing.Id),
            cancellationToken);

    public Task<IReadOnlyList<BorrowingDetails>> GetHistoryByStudentAsync(int studentId, CancellationToken cancellationToken = default) =>
        MaterializeDetailsAsync(
            GetDetailsQuery()
                .Where(row => row.Student.Id == studentId)
                .OrderByDescending(row => row.Borrowing.BorrowDate)
                .ThenBy(row => row.Borrowing.Id),
            cancellationToken);

    public Task<IReadOnlyList<BorrowingDetails>> GetHistoryByEquipmentAsync(int equipmentId, CancellationToken cancellationToken = default) =>
        MaterializeDetailsAsync(
            GetDetailsQuery()
                .Where(row => row.Equipment.Id == equipmentId)
                .OrderByDescending(row => row.Borrowing.BorrowDate)
                .ThenBy(row => row.Borrowing.Id),
            cancellationToken);

    public async Task<IReadOnlyList<BorrowingDetails>> QueryDetailsAsync(BorrowingQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var detailsQuery = GetDetailsQuery();
        if (query.StudentId.HasValue)
            detailsQuery = detailsQuery.Where(row => row.Student.Id == query.StudentId.Value);
        if (query.Status.HasValue)
            detailsQuery = detailsQuery.Where(row => row.Borrowing.Status == query.Status.Value);

        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            var escapedText = query.SearchText.Trim()
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_");
            var pattern = $"%{escapedText}%";
            detailsQuery = detailsQuery.Where(row =>
                EF.Functions.ILike(row.Student.StudentNumber, pattern, "\\") ||
                EF.Functions.ILike(row.Student.Name, pattern, "\\") ||
                EF.Functions.ILike(row.Equipment.Identifier, pattern, "\\") ||
                EF.Functions.ILike(row.Equipment.Name, pattern, "\\"));
        }

        IOrderedQueryable<BorrowingRow> orderedQuery = (query.SortField, query.SortDirection) switch
        {
            (BorrowingSortField.BorrowDate, SortDirection.Ascending) => detailsQuery.OrderBy(row => row.Borrowing.BorrowDate).ThenBy(row => row.Borrowing.Id),
            (BorrowingSortField.BorrowDate, SortDirection.Descending) => detailsQuery.OrderByDescending(row => row.Borrowing.BorrowDate).ThenBy(row => row.Borrowing.Id),
            (BorrowingSortField.ExpectedReturnDate, SortDirection.Ascending) => detailsQuery.OrderBy(row => row.Borrowing.ExpectedReturnDate).ThenBy(row => row.Borrowing.Id),
            (BorrowingSortField.ExpectedReturnDate, SortDirection.Descending) => detailsQuery.OrderByDescending(row => row.Borrowing.ExpectedReturnDate).ThenBy(row => row.Borrowing.Id),
            _ => throw new ArgumentOutOfRangeException(nameof(query), "Unsupported borrowing sort option.")
        };

        return await EfPersistenceExecutor.ExecuteAsync<IReadOnlyList<BorrowingDetails>>(async () =>
            await ProjectDetails(orderedQuery).ToListAsync(cancellationToken));
    }

    private IQueryable<BorrowingRow> GetDetailsQuery() =>
        from borrowing in _db.Borrowings.AsNoTracking()
        join student in _db.Students.AsNoTracking() on borrowing.StudentId equals student.Id
        join equipment in _db.Equipment.AsNoTracking() on borrowing.EquipmentId equals equipment.Id
        select new BorrowingRow
        {
            Borrowing = borrowing,
            Student = student,
            Equipment = equipment
        };

    private static IQueryable<BorrowingDetails> ProjectDetails(IQueryable<BorrowingRow> query) =>
        query.Select(row => new BorrowingDetails(
            row.Borrowing.Id,
            row.Student.Id,
            row.Student.StudentNumber,
            row.Student.Name,
            row.Equipment.Id,
            row.Equipment.Identifier,
            row.Equipment.Name,
            row.Borrowing.BorrowDate,
            row.Borrowing.ExpectedReturnDate,
            row.Borrowing.ReturnDate,
            row.Borrowing.Status,
            row.Borrowing.Version));

    private static Task<IReadOnlyList<BorrowingDetails>> MaterializeDetailsAsync(
        IQueryable<BorrowingRow> query,
        CancellationToken cancellationToken) =>
        EfPersistenceExecutor.ExecuteAsync<IReadOnlyList<BorrowingDetails>>(async () =>
            await ProjectDetails(query).ToListAsync(cancellationToken));

    private sealed class BorrowingRow
    {
        public Borrowing Borrowing { get; init; } = null!;
        public Student Student { get; init; } = null!;
        public Equipment Equipment { get; init; } = null!;
    }

}
