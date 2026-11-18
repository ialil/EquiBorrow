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
                .Where(details => details.Status == BorrowingStatus.Active)
                .OrderByDescending(details => details.BorrowDate)
                .ThenBy(details => details.BorrowingId),
            cancellationToken);

    public Task<IReadOnlyList<BorrowingDetails>> GetHistoryByStudentAsync(int studentId, CancellationToken cancellationToken = default) =>
        MaterializeDetailsAsync(
            GetDetailsQuery()
                .Where(details => details.StudentId == studentId)
                .OrderByDescending(details => details.BorrowDate)
                .ThenBy(details => details.BorrowingId),
            cancellationToken);

    public Task<IReadOnlyList<BorrowingDetails>> GetHistoryByEquipmentAsync(int equipmentId, CancellationToken cancellationToken = default) =>
        MaterializeDetailsAsync(
            GetDetailsQuery()
                .Where(details => details.EquipmentId == equipmentId)
                .OrderByDescending(details => details.BorrowDate)
                .ThenBy(details => details.BorrowingId),
            cancellationToken);

    public async Task<IReadOnlyList<BorrowingDetails>> QueryDetailsAsync(BorrowingQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var detailsQuery = GetDetailsQuery();
        if (query.Status.HasValue)
            detailsQuery = detailsQuery.Where(details => details.Status == query.Status.Value);

        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            var escapedText = query.SearchText.Trim()
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_");
            var pattern = $"%{escapedText}%";
            detailsQuery = detailsQuery.Where(details =>
                EF.Functions.ILike(details.StudentNumber, pattern, "\\") ||
                EF.Functions.ILike(details.StudentName, pattern, "\\") ||
                EF.Functions.ILike(details.EquipmentIdentifier, pattern, "\\") ||
                EF.Functions.ILike(details.EquipmentName, pattern, "\\"));
        }

        IOrderedQueryable<BorrowingDetails> orderedQuery = (query.SortField, query.SortDirection) switch
        {
            (BorrowingSortField.BorrowDate, SortDirection.Ascending) => detailsQuery.OrderBy(details => details.BorrowDate).ThenBy(details => details.BorrowingId),
            (BorrowingSortField.BorrowDate, SortDirection.Descending) => detailsQuery.OrderByDescending(details => details.BorrowDate).ThenBy(details => details.BorrowingId),
            (BorrowingSortField.ExpectedReturnDate, SortDirection.Ascending) => detailsQuery.OrderBy(details => details.ExpectedReturnDate).ThenBy(details => details.BorrowingId),
            (BorrowingSortField.ExpectedReturnDate, SortDirection.Descending) => detailsQuery.OrderByDescending(details => details.ExpectedReturnDate).ThenBy(details => details.BorrowingId),
            _ => throw new ArgumentOutOfRangeException(nameof(query), "Unsupported borrowing sort option.")
        };

        return await EfPersistenceExecutor.ExecuteAsync<IReadOnlyList<BorrowingDetails>>(async () =>
            await orderedQuery.ToListAsync(cancellationToken));
    }

    public async Task AddAsync(Borrowing borrowing, CancellationToken cancellationToken = default) =>
        await _db.Borrowings.AddAsync(borrowing, cancellationToken);

    public Task<int> GetActiveCountByStudentIdAsync(int studentId, CancellationToken cancellationToken = default) =>
        GetActiveCountByStudentAsync(studentId, cancellationToken);

    public Task<IReadOnlyList<Borrowing>> GetAllAsync(CancellationToken cancellationToken = default) =>
        GetBorrowingsAsync(_db.Borrowings.AsNoTracking(), cancellationToken);

    public Task<Borrowing?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        EfPersistenceExecutor.ExecuteAsync(() =>
            _db.Borrowings.AsNoTracking().SingleOrDefaultAsync(borrowing => borrowing.Id == id, cancellationToken));

    private IQueryable<BorrowingDetails> GetDetailsQuery() =>
        from borrowing in _db.Borrowings.AsNoTracking()
        join student in _db.Students.AsNoTracking() on borrowing.StudentId equals student.Id
        join equipment in _db.Equipment.AsNoTracking() on borrowing.EquipmentId equals equipment.Id
        select new BorrowingDetails(
            borrowing.Id,
            student.Id,
            student.StudentNumber,
            student.Name,
            equipment.Id,
            equipment.Identifier,
            equipment.Name,
            borrowing.BorrowDate,
            borrowing.ExpectedReturnDate,
            borrowing.ReturnDate,
            borrowing.Status,
            borrowing.Version);

    private static Task<IReadOnlyList<BorrowingDetails>> MaterializeDetailsAsync(
        IQueryable<BorrowingDetails> query,
        CancellationToken cancellationToken) =>
        EfPersistenceExecutor.ExecuteAsync<IReadOnlyList<BorrowingDetails>>(async () =>
            await query.ToListAsync(cancellationToken));

    private static Task<IReadOnlyList<Borrowing>> GetBorrowingsAsync(
        IQueryable<Borrowing> query,
        CancellationToken cancellationToken) =>
        EfPersistenceExecutor.ExecuteAsync<IReadOnlyList<Borrowing>>(async () =>
            await query.ToListAsync(cancellationToken));
}
