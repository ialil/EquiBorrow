using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EquiBorrow.Application.Interfaces;
using EquiBorrow.Domain;

namespace EquiBorrow.Application.Services;

public sealed class ListStudentsService
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public ListStudentsService(IUnitOfWorkFactory unitOfWorkFactory) => _unitOfWorkFactory = unitOfWorkFactory;

    public async Task<OperationResult<IReadOnlyList<Student>>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var unitOfWork = _unitOfWorkFactory.Create();
            var students = await unitOfWork.Students.GetAllAsync(cancellationToken);
            return OperationResult<IReadOnlyList<Student>>.Succeeded(students, $"Loaded {students.Count} students.");
        }
        catch (PersistenceFailureException)
        {
            return OperationResult<IReadOnlyList<Student>>.Failed("Could not load students. Check the database connection and retry.");
        }
    }
}
