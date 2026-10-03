using System.Threading.Tasks;

namespace EquiBorrow.Infrastructure.Sql;

public class NoOpSqlInspector : ISqlInspector
{
    public Task<string> GetGeneratedSqlAsync()
    {
        return Task.FromResult("SQL inspector not available in fallback mode.");
    }
}
