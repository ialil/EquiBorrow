using System;
using System.Threading.Tasks;
using EquiBorrow.Application.Services;
using EquiBorrow.Infrastructure.Repositories;

namespace EquiBorrowing.ConsoleApp;

class Program
{
    static async Task Main(string[] args)
    {
        var studentRepo = new InMemoryStudentRepository();
        var equipmentRepo = new InMemoryEquipmentRepository();
        var borrowingRepo = new InMemoryBorrowingRepository();

        var service = new BorrowEquipmentService(studentRepo, equipmentRepo, borrowingRepo);

        Console.WriteLine("=== Campus Equipment Borrowing Demo ===\n");

        // ----- SUCCESS CASE -----
        try
        {
            Console.WriteLine("1. Alice (ID 1) borrows Laptop (ID 101)");
            var result = await service.ExecuteAsync(1, 101);
            Console.WriteLine($"   ✅ SUCCESS! Borrowing ID: {result.Id}, Status: {result.Status}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Failed: {ex.Message}\n");
        }

        // ----- FAILURE 1: Equipment Unavailable -----
        try
        {
            Console.WriteLine("2. Alice (ID 1) borrows Arduino Kit (ID 103) [Already Unavailable]");
            await service.ExecuteAsync(1, 103);
            Console.WriteLine("   ✅ SUCCESS (unexpected)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Expected Failure: {ex.Message}\n");
        }

        // ----- FAILURE 2: Student Inactive -----
        try
        {
            Console.WriteLine("3. Bob (ID 2) borrows Projector (ID 102) [Bob is inactive]");
            await service.ExecuteAsync(2, 102);
            Console.WriteLine("   ✅ SUCCESS (unexpected)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Expected Failure: {ex.Message}\n");
        }

        // ----- Show Max Limit Logic -----
        var count = await borrowingRepo.GetActiveCountByStudentIdAsync(1);
        Console.WriteLine($"4. Alice currently has {count} active borrowing(s). Max is 3.");
        Console.WriteLine("   (Borrowing 2nd and 3rd would succeed, 4th would hit the limit)");

        Console.WriteLine("\nDemo complete. Press any key to exit...");
        Console.ReadKey();
    }
}