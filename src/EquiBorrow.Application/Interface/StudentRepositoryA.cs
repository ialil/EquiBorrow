using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EquiBorrow.Domain;

namespace EquiBorrow.Application.Interfaces;

public interface IStudentRepository
{
    Task<Student?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Student>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Student student, CancellationToken cancellationToken = default);
    Task UpdateAsync(Student student, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}