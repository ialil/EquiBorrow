using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EquiBorrow.Domain;

namespace EquiBorrow.Application.Interfaces;

public interface IBorrowingRepository
{
    void Add(Borrowing borrowing, CancellationToken cancellationToken = default);
    void AddRange(IEnumerable<Borrowing> borrowings, CancellationToken cancellationToken = default);
    Task<Borrowing?> GetByIdForUpdateAsync(int id, CancellationToken cancellationToken = default);
    Task<int> GetActiveCountByStudentAsync(int studentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BorrowingDetails>> GetActiveDetailsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BorrowingDetails>> GetHistoryByStudentAsync(int studentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BorrowingDetails>> GetHistoryByEquipmentAsync(int equipmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BorrowingDetails>> QueryDetailsAsync(BorrowingQuery query, CancellationToken cancellationToken = default);
}
