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

        Console.WriteLine("=== CAMPUS EQUIPMENT BORROWING SYSTEM ===\n");

        // ----- DISPLAY ALL STUDENTS -----
        Console.WriteLine("ALL STUDENTS:");
        for (int id = 1; id <= 10; id++)
        {
            var student = await studentRepo.GetByIdAsync(id);
            if (student != null)
            {
                Console.WriteLine($"   - ID: {student.Id}, Name: {student.Name}, Active: {student.IsActive}");
            }
        }

        Console.WriteLine();

        // ----- DISPLAY ALL EQUIPMENT -----
        Console.WriteLine("ALL EQUIPMENT:");
        for (int id = 101; id <= 110; id++)
        {
            var equip = await equipmentRepo.GetByIdAsync(id);
            if (equip != null)
            {
                Console.WriteLine($"   - ID: {equip.Id}, Name: {equip.Name}, Available: {equip.IsAvailable}");
            }
        }

        Console.WriteLine("\n----------------------------------------\n");

        // ============================================================
        // BORROWING DEMO
        // ============================================================
        Console.WriteLine("BORROWING DEMO:\n");

        int caseNumber = 1;

        // ----- CASE 1: Charlie Puth borrows Laptop -----
        try
        {
            Console.WriteLine($"{caseNumber}. Charlie Puth (ID 3) borrows Laptop (ID 101)");
            var result = await service.ExecuteAsync(3, 101);
            Console.WriteLine($"   SUCCESS! Borrowing ID: {result.Id}, Status: {result.Status}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   FAILED: {ex.Message}\n");
        }
        caseNumber++;

        // ----- CASE 2: Quifrey borrows Projector -----
        try
        {
            Console.WriteLine($"{caseNumber}. Quifrey (ID 8) borrows Projector (ID 102)");
            var result = await service.ExecuteAsync(8, 102);
            Console.WriteLine($"   SUCCESS! Borrowing ID: {result.Id}, Status: {result.Status}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   FAILED: {ex.Message}\n");
        }
        caseNumber++;

        // ----- CASE 3: Alice borrows 3D Printer -----
        try
        {
            Console.WriteLine($"{caseNumber}. Alice (ID 9) borrows 3D Printer (ID 104)");
            var result = await service.ExecuteAsync(9, 104);
            Console.WriteLine($"   SUCCESS! Borrowing ID: {result.Id}, Status: {result.Status}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   FAILED: {ex.Message}\n");
        }
        caseNumber++;

        // ----- CASE 4: Charlie Puth borrows VR Headset (2nd item) -----
        try
        {
            Console.WriteLine($"{caseNumber}. Charlie Puth (ID 3) borrows VR Headset (ID 106)");
            var result = await service.ExecuteAsync(3, 106);
            Console.WriteLine($"   SUCCESS! Borrowing ID: {result.Id}, Status: {result.Status}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   FAILED: {ex.Message}\n");
        }
        caseNumber++;

        // ----- CASE 5: Charlie Puth borrows Microphone (3rd item - HITS MAX) -----
        try
        {
            Console.WriteLine($"{caseNumber}. Charlie Puth (ID 3) borrows Microphone (ID 107)");
            var result = await service.ExecuteAsync(3, 107);
            Console.WriteLine($"   SUCCESS! Borrowing ID: {result.Id}, Status: {result.Status}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   FAILED: {ex.Message}\n");
        }
        caseNumber++;

        // ----- CASE 6: FAILURE - Charlie Puth tries 4th borrow (Max limit) -----
        try
        {
            Console.WriteLine($"{caseNumber}. Charlie Puth (ID 3) borrows Smartwatch (ID 109) [Should FAIL - Max limit 3]");
            await service.ExecuteAsync(3, 109);
            Console.WriteLine("   SUCCESS (unexpected - should have failed!)\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   Expected Failure: {ex.Message}\n");
        }
        caseNumber++;

        // ----- CASE 7: FAILURE - Equipment unavailable (Arduino) -----
        try
        {
            Console.WriteLine($"{caseNumber}. Alice (ID 9) borrows Arduino Kit (ID 103) [Unavailable]");
            await service.ExecuteAsync(9, 103);
            Console.WriteLine("   SUCCESS (unexpected)\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   Expected Failure: {ex.Message}\n");
        }
        caseNumber++;

        // ----- CASE 8: FAILURE - Billy Jean (ID 2) is inactive -----
        try
        {
            Console.WriteLine($"{caseNumber}. Billy Jean (ID 2) borrows Microphone (ID 107) [Inactive]");
            await service.ExecuteAsync(2, 107);
            Console.WriteLine("   SUCCESS (unexpected)\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   Expected Failure: {ex.Message}\n");
        }
        caseNumber++;

        // ----- CASE 9: FAILURE - Steven (ID 5) is inactive -----
        try
        {
            Console.WriteLine($"{caseNumber}. Steven (ID 5) borrows Smartwatch (ID 109) [Inactive]");
            await service.ExecuteAsync(5, 109);
            Console.WriteLine("   SUCCESS (unexpected)\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   Expected Failure: {ex.Message}\n");
        }
        caseNumber++;

        // ----- CASE 10: FAILURE - Try to borrow already borrowed equipment -----
        try
        {
            Console.WriteLine($"{caseNumber}. Quifrey (ID 8) borrows Laptop (ID 101) [Already borrowed by Charlie]");
            await service.ExecuteAsync(8, 101);
            Console.WriteLine("   SUCCESS (unexpected)\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   Expected Failure: {ex.Message}\n");
        }
        caseNumber++;

        Console.WriteLine("----------------------------------------\n");

        // ============================================================//
        // SUMMARY                                                     //
        // ============================================================//

        Console.WriteLine("ACTIVE BORROWINGS SUMMARY:");

        // Show active borrowings for ALL active students
        for (int id = 1; id <= 10; id++)
        {
            var student = await studentRepo.GetByIdAsync(id);
            if (student != null && student.IsActive)
            {
                var count = await borrowingRepo.GetActiveCountByStudentIdAsync(id);
                Console.WriteLine($"   - {student.Name}: {count} active borrowing(s)");
            }
        }

        // Show Charlie's borrow count specifically
        var charlieCount = await borrowingRepo.GetActiveCountByStudentIdAsync(3);
        Console.WriteLine($"\nCharlie Puth has {charlieCount} active borrowings. (Max is 3)");
        if (charlieCount == 3)
        {
            Console.WriteLine("Charlie has reached the maximum limit!");
        }

        Console.WriteLine("\nDemo complete. Press any key to exit...");
        Console.ReadKey();
    }
}