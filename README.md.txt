EquiBorrow - Campus Equipment Borrowing System
1. Solution Structure
- Domain: Contains core business entities (Student, Equipment, Borrowing) and enums (BorrowingStatus). No external dependencies.
- Application: Contains use-case services (BorrowEquipmentService) and repository interfaces (IStudentRepository, IEquipmentRepository, IBorrowingRepository). Depends only on Domain.
- Infrastructure: Contains concrete implementations of repositories (InMemory...). Depends on Application and Domain.
- Console: Simple executable to demonstrate the flow. Depends on Application and Infrastructure.
 2. Dependency Direction
Console
   │
   ▼
Application
   │
   ▼
Domain
   ▲
   │
Infrastructure