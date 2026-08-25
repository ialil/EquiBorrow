EquiBorrow - Campus Borrowing System
Authors:
    Dimakuta, Charles Asher C.
    Lumapas, Jhayvine Mae D.
Section Code: BSIT 3Cx
Desktop Application Development, Activity 1

---
This repository contains the architecture for the Campus Equipment Borrowing system.

1. Structure
The program is divided into distinct projects, each with distinct responsibilities.

1.a.   **`EquiBorrow.Domain`: Contains the core business concepts, entities, and rules of the system . This includes models like `Student`, `Equipment`, `Borrowing`, and `BorrowingStatus` . It has no dependencies on external frameworks or other projects in the solution.
1.b.   **`EquiBorrow.Application`**: Contains the application's use cases and coordinates domain objects . It defines the `Services` (e.g., `BorrowEquipServiceA`) that execute business operations and the `Interface` abstractions (e.g., `BorrowingResipositoryA`, `EquipmentRepositoryA`) needed to fetch and store data .
1.c.   **`EquiBorrow.Infrastructure`**: Contains the technical implementations for data access and external services . For this phase, it includes simple in-memory storage implementations like `MemoryBorrowRepos`, `MemoryEquiRepos`, and `MemoryStudentRepos` . 
1.d.   **`EquiBorrowing.Console`**: Acts as the presentation/execution layer that wires the dependencies together (Dependency Injection) and demonstrates the application flow (Successful and Failure cases).
1.e.   **`EquiBorrow.Tests`** *(Planned)*: Reserved for automated tests verifying application and domain behavior.

2. Dependency direction
The program enforced an inward dependency direction, making sure that business logic is completely isolated from the technical implementation details.

EquiBorrowing.Console (Presentation)
        │
        ▼
EquiBorrow.Infrastructure ──────► EquiBorrow.Application
                                          │
                                          ▼
                                  EquiBorrow.Domain
```

2.a.   **Domain** depends on *nothing*.
2.b.   **Application** depends only on **Domain**.
2.c.   **Infrastructure** depends on **Application** (to implement its repository interfaces) and     **Domain** (to store/retrieve domain objects) .
2.d.   **Console/UI** depends on **Application** and **Infrastructure** (to configure dependency injection at startup).

3. Use Case Mapping
Listed below are three (3) use cases for the program.

UC-01 - Borrow Equipment
    Item            |   Description
Use Case            | Borrow Equipment
Primary Actor       | Student
Preconditions       | Student exists and IsActive == true;
                    | Equipment exists and IsAvailable == true;
                    | Student's active borrowings < MaxActiveBorrowings (3)
Main Action         | 1. The Student requests to borrow a piece of equipment by providing their Student ID and the Equipment ID.
                    | 2. The System validates the request details:
                    |   2.a. Verifies that the Student exists and is currently active (IsActive == true).
                    |   2.b. Verifies that the Equipment exists and is available (IsAvailable == true).
                    |   2.c. Verifies that the Student's total active borrowings are below the limit (< MaxActiveBorrowings).
                    | 3. The System creates a new Borrowing record with an active status.
                    | 4. The System updates the status of the requested Equipment to unavailable (IsAvailable = false).
                    | 5. The System persists both the new Borrowing record and the updated Equipment state to the database.
                    | 6. The System confirms the successful transaction by returning the newly created Borrowing details to the Student.
Expected Result     | New Borrowing returned with Status = Active and an assigned Id; equipment is persisted as unavailable
Possible Failure    | Student not found or inactive; equipment not found or unavailable; student already at max active borrowings; persistence/
                    | repository error.
---

UC-02 - Enforcing Borrowing Limit
    Item            |   Description
Use Case            | Enforcing Borrowing Limit
Primary Actor       | Borrowing System
Preconditions       | Student Exists
Main Action         | 1. The Borrowing system initiates the validation process for a student borrowing request.
                    | 2. The Service queries the repository for the student's active borrowings count (IBorrowingRepository.
                    | GetActiveCountByStudentIdAsync(studentId)).
                    | 3. The Service retrieves the count and evaluates it against the system parameter MaxActiveBorrowings.
                    | 4. The System branches based on the comparison result:
                    |   4.a. If count < MaxActiveBorrowings, the system allows the borrow flow to proceed to the next step.
                    |   4.b. If count >= MaxActiveBorrowings, the system halts the process and returns an error rejecting the borrow request.
Expected Result     | If count < MaxActiveBorrowings the borrow flow proceeds; otherwise the service rejects the borrow with an error
Possible Failure    | Repository returns wrong count (stale data), network/persistence failure, or returned borrowings not marked correctly so 
                    | count is inaccurate
---

UC-03 - Check & Update Equipment Availability
    Item            |   Description
Use Case            | Check & Update Equipment Availability
Primary Actor       | Equipment Manager
Preconditions       | Equipment record exists in IEquipmentRepository
Main Action         | 1. The Equipment Manager initiates the process to update equipment availability.
                    | 2. The System fetches the target equipment record from IEquipmentRepository.GetByIdAsync.
                    | 3. The System determines the transaction context and modifies the IsAvailable flag:
                    |   3.a. Sets IsAvailable = false if the item is being borrowed.
                    |   3.b. Sets IsAvailable = true if the item is being returned.
                    | 4. The System persists the updated equipment record using IEquipmentRepository.UpdateAsync.
Expected Result     | Equipment availability status is updated in the repository and subsequent queries reflect the new availability
Possible Failure    | Equipment not found; concurrent updates overwrite availability; repository persistence failure
---

4. Reflection
    1. Why should the application service depend on a repository interface instead of directly depending on a database implementation?
        The application service should depend on a repository interface instead of a direct database implementation to separate the application's business logic from the technical components making up the program. This allows the application service to be easily tested, while also ensuring that errors that occur due to changing anything do not affect the overlying logic.

    2. Which parts of your current solution could remain unchanged if SQLite were added later?
        The projects 'EquiBorrow.Domain' and 'EquiBorrow.Application' would remain intact regardless if SQLite were to be implemented later. Adding SQLite would only require creating new repository classes in the .Infastructure project of the program, as well as a reconfiguration of the startup config.

    3. Which project would eventually contain Avalonia Views?
        A new project, 'Equiborrow.UI' would entirely replace the 'Equiborrow.Console' project to house Avalonia Views, which will serve as the program's entry point.

    4. Should an Avalonia button directly execute database queries? Why or why not?
        No, since making an Avalonia button directly execute database queries would violate separation of concerns by mixing UI logic with data access and business logic. Doing so also directly exposes the database to client-side code.

    5.  What part of your implementation represents the actual business operation requested by the actor?
        The application service (BorrowEquipServiceA) represents the business operations requested by the actor. It handles validating the student, checking equipment availability, enforcing limits, and generating the borrowing record.
---
