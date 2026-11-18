using System.Threading;
using System.Threading.Tasks;

namespace EquiBorrow.Application.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{
    IStudentRepository Students { get; }
    IEquipmentRepository Equipment { get; }
    IBorrowingRepository Borrowings { get; }
    Task CommitAsync(CancellationToken cancellationToken = default);
}

public interface IUnitOfWorkFactory
{
    IUnitOfWork Create();
}
