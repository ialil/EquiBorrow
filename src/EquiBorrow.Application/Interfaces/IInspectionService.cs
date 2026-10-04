using System.Threading.Tasks;

namespace EquiBorrow.Application.Interfaces
{
    public interface IInspectionService
    {
        Task<string> GetGeneratedSqlAsync();
    }
}
