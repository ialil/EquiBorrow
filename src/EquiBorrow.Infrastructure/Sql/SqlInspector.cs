using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EquiBorrow.Infrastructure.Persistence;

namespace EquiBorrow.Infrastructure.Sql;

public class SqlInspector : ISqlInspector
{
    private readonly EquipmentBorrowingDbContext _db;

    public SqlInspector(EquipmentBorrowingDbContext db)
    {
        _db = db;
    }

    public Task<string> GetGeneratedSqlAsync()
    {
        var sb = new StringBuilder();

        // Students query
        var studentsQ = _db.Students.Where(s => s.IsActive).OrderBy(s => s.Name);
        sb.AppendLine("-- Active students SQL");
        sb.AppendLine(studentsQ.ToQueryString());
        sb.AppendLine();

        // Available equipment
        var equipmentQ = _db.Equipment.Where(e => e.IsAvailable).OrderBy(e => e.Name);
        sb.AppendLine("-- Available equipment SQL");
        sb.AppendLine(equipmentQ.ToQueryString());
        sb.AppendLine();

        // Active borrowings with joins
        var borrowQ = _db.Borrowings
            .Where(b => b.Status == Domain.BorrowingStatus.Active)
            .Join(_db.Students, b => b.StudentId, s => s.Id, (b, s) => new { b, s })
            .Join(_db.Equipment, bs => bs.b.EquipmentId, e => e.Id, (bs, e) => new { bs.b, bs.s, e })
            .Select(x => new { Student = x.s.Name, Equipment = x.e.Name, x.b.BorrowDate, x.b.ExpectedReturnDate });

        sb.AppendLine("-- Active borrowings join SQL");
        sb.AppendLine(borrowQ.ToQueryString());
        sb.AppendLine();

        return Task.FromResult(sb.ToString());
    }
}
