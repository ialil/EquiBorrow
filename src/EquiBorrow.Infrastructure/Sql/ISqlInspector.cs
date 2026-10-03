using System.Threading.Tasks;

namespace EquiBorrow.Infrastructure.Sql;

public interface ISqlInspector
{
    Task<string> GetGeneratedSqlAsync();
}
