using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EquiBorrow.Domain;

namespace EquiBorrow.Application.Interfaces;

public interface IStudentRepository
{
    Task<IReadOnlyList<Student>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Student?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Student?> GetByIdForUpdateAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNumberAsync(string number, int? excludeId = null, CancellationToken cancellationToken = default);
    void Add(Student student, CancellationToken cancellationToken = default);
}
