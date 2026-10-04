using System.Threading.Tasks;
using EquiBorrow.Application.Interfaces;

namespace EquiBorrow.Infrastructure.Sql;

public class NoOpSqlInspector : IInspectionService
{
    public Task<string> GetGeneratedSqlAsync()
    {
        return Task.FromResult("SQL inspector not available in fallback mode.");
    }
}
