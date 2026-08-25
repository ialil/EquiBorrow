using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EquiBorrow.Application.Interfaces;
using EquiBorrow.Domain;

namespace EquiBorrow.Infrastructure.Repositories;

public class InMemoryStudentRepository : IStudentRepository
{
    private readonly Dictionary<int, Student> _students = new()
    {
        [1] = new Student(1, "Ash", true),
        [2] = new Student(2, "Billy Jean", false),
        [3] = new Student(3, "Charlie Puth", true),
        [4] = new Student(4, "Ashley", true),
        [5] = new Student(5, "Steven", true),
        [6] = new Student(6, "Joanna", false),
        [7] = new Student(7, "Fluffy", false),
        [8] = new Student(8, "Quifrey", true),
        [9] = new Student(9, "Alice", true),
        [10] = new Student(10, "Bob", false),
    };

    public Task<Student?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        _students.TryGetValue(id, out var student);
        return Task.FromResult(student);
    }
}