using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EquiBorrow.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<EquipmentBorrowingDbContext>
{
    public EquipmentBorrowingDbContext CreateDbContext(string[] args)
    {
        string cs;
        try { cs = ConnectionStringResolver.Resolve(Directory.GetCurrentDirectory()); }
        catch (InvalidOperationException) { cs = "Host=localhost;Database=equiborrow_design_time"; }

        var options = new DbContextOptionsBuilder<EquipmentBorrowingDbContext>()
            .UseNpgsql(cs)
            .Options;
        return new EquipmentBorrowingDbContext(options);
    }
}