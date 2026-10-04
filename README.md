EquiBorrow - Campus Borrowing System
Authors:
    Dimakuta, Charles Asher C.
    Lumapas, Jayvhine Mae D.
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



EquiBorrow.UI is the Avalonia desktop project added in this activity. It is the only new project in
the solution (EquiBorrow.slnx references EquiBorrow.UI, EquiBorrow.Application, EquiBorrow.Domain,
and EquiBorrow.Infrastructure). Its responsibilities are limited to presentation:

Views (MainWindow.axaml): lays out the Students, Equipment, and Borrowing sections using
Grid, StackPanel, and ListBox controls, and binds them to a ViewModel.
ViewModels (ViewModels/MainViewModel.cs): exposes ObservableCollection<T> properties
(Students, Equipments, StudentBorrows), selection state, input fields, a StatusMessage for
feedback, and RelayCommand instances (ViewModels/RelayCommand.cs) that the View binds its
buttons to (BorrowCommand, RefreshCommand, etc.).
Converters (Converters/BoolToBrushConverter.cs, Converters/BoolToTextConverter.cs):
small IValueConverter implementations used purely for display (e.g., turning IsAvailable
into a color or a friendlier "Active/Inactive" label).
Composition (Program.cs, App.axaml.cs, MainWindow.axaml.cs): starts the Avalonia
application and constructs the repository instances and the MainViewModel that the window's
DataContext is set to.

EquiBorrow.UI references only EquiBorrow.Application and EquiBorrow.Infrastructure. It does not
reference anything from EquiBorrow.Domain directly in its project file — domain types flow into the
UI transitively through the interfaces exposed by EquiBorrow.Application. Neither EquiBorrow.Domain
nor EquiBorrow.Application reference Avalonia, so the business rules stay usable outside of a desktop
context (e.g., from a console app or a test project).

Note on repository leftovers: the repository also contains an unrelated EquiBorow.Desktop
folder (an F# Avalonia cross-platform template for Android/iOS/Browser/Desktop) and stray
EquiBorrowing.Console / EquiBorrow.Console folders. None of these are referenced by
EquiBorrow.slnx and none of them are part of the working application — they are earlier
experiments that should be deleted before final submission so the project tree matches what is
actually built and graded.

6. Updated Architecture
Avalonia View (MainWindow.axaml)
        │
        │ Data Binding / Command
        ▼
MainViewModel (EquiBorrow.UI.ViewModels)
        │
        │ Application Operation
        ▼
Application Service (BorrowEquipmentService / ReturnEquipmentServices)
        │
        ├──────────► Domain (Student, Equipment, Borrowing, BorrowingStatus)
        │
        ▼
Repository Interface (IStudentRepository, IEquipmentRepository, IBorrowingRepository)
        ▲
        │
Infrastructure Implementation (InMemoryStudentRepository, InMemoryEquipmentRepository,
                                InMemoryBorrowingRepository)

The View never talks to a repository or a service directly — it only raises commands defined on the
ViewModel. The ViewModel never constructs domain rules itself; when it correctly follows the intended
pattern, it forwards the request to an Application service and reports back whatever the service
returns or throws.

7. Borrow Equipment Flow
The user selects a student in the Students list and an equipment item in the Equipment
list, then presses Borrow →.
The button's Command binding invokes MainViewModel.BorrowCommand, which runs the
BorrowAsync() method on the ViewModel.
The ViewModel checks that a student and an equipment item are selected and that the equipment
is marked available; if not, it sets StatusMessage so the user sees why the action did not
proceed. This is presentation-level validation (a required selection is missing).
On success, the equipment's repository record is fetched and updated, a new Borrowing is
created, and it is added through IBorrowingRepository.AddAsync.
RefreshStudentBorrowsAsync() reloads the selected student's active borrowings from the
repository into the StudentBorrows collection, and StatusMessage is updated to confirm the
transaction (e.g., "<Student> borrowed <Equipment>.").
Because Students, Equipments, and StudentBorrows are ObservableCollection<T> bound to the
View, the UI updates automatically once the ViewModel's data changes — no manual UI refresh code
is needed in the View.

Current implementation detail worth revisiting: BorrowAsync() in MainViewModel currently
re-checks IsAvailable and creates the Borrowing record itself rather than calling
BorrowEquipmentService.ExecuteAsync(...). The Application layer already implements the full rule
set (student existence/active check, equipment existence/availability check, and the
MaxActiveBorrowings limit) in BorrowEquipmentService, so per the "Required Rule" in Part E of
the activity, BorrowAsync() should be changed to call that service instead of re-implementing
availability/limit logic locally. Doing so is what actually exercises the limit-of-3 rule and the
inactive-student rule from the UI.

8. Return Equipment Flow

The business logic for returning equipment already exists in the Application layer:
ReturnEquipmentServices.ExecuteAsync(borrowingId) locates the borrowing, rejects it if it has
already been returned, marks it as Returned, and flips the related equipment's IsAvailable
back to true through IEquipmentRepository.UpdateAsync.

This is not yet wired into EquiBorrow.UI. MainWindow.axaml shows a student's currently
borrowed items ("Borrowed Items (Memory)") but does not expose a "Return" button, and
MainViewModel does not construct or call ReturnEquipmentServices. To satisfy Part F/Part J of
the activity, the remaining work is:

Add a Return button next to the borrowed-items list (or an Active Borrowings section as
described in Part C/Part J), bound to a new ReturnCommand.
Add ReturnCommand to MainViewModel, taking the selected Borrowing's id.
Have the command call _returnEquipmentService.ExecuteAsync(borrowingId) (the service should
be supplied through the ViewModel's constructor, not created with new inside the ViewModel).
On success, refresh StudentBorrows and Equipments so the returned item shows as available
again, and set StatusMessage accordingly; on failure (already returned, not found), show the
exception message in StatusMessage instead of throwing it into the UI.
9. Dependency Injection Status

The project references Microsoft.Extensions.DependencyInjection, but there is currently no
composition root that uses it. MainWindow.axaml.cs constructs the three in-memory repositories
and the MainViewModel directly with new:

csharp
var studentRepository = new InMemoryStudentRepository();
var equipmentRepository = new InMemoryEquipmentRepository();
var borrowingRepository = new InMemoryBorrowingRepository();
var viewModel = new MainViewModel(studentRepository, equipmentRepository, borrowingRepository);

This keeps the singleton lifetime of the in-memory repositories correct for a single window (state
is not lost because one MainWindow instance owns one set of repository instances), and it does
follow constructor injection into the ViewModel. To match Part H of the activity more closely
before submission, this wiring should move into a small composition root (e.g., a ServiceCollection
configured in Program.cs or App.axaml.cs) that registers the repositories as singletons and the
application services / ViewModels as transient, then resolves MainViewModel for the window instead
of hand-assembling it in code-behind.

10. Architectural Reflection
Why should the View not call a repository directly?
The View's only job is to display state and forward user actions. If it called a repository
directly, presentation code would need to know about persistence and business rules (e.g., what
makes equipment "available"), which breaks the separation the whole architecture is built around
and makes the rules impossible to reuse or test outside the UI.
Why should business rules not be implemented in the ViewModel?
The ViewModel's job is to hold presentation state and translate user actions into calls on the
Application layer. If it re-implements rules like the borrowing limit or availability checks, the
same rule now exists in two places that can drift apart, and the rule can no longer be tested or
reused independently of Avalonia.
What is the responsibility of the ViewModel?
To hold the data a View binds to (selected items, input fields, observable collections), expose
commands the View can invoke, call the appropriate Application service when a command executes,
and translate the result — success or a thrown exception — into user-facing state such as
StatusMessage.
Why can the existing Application layer work without knowing that Avalonia is being used?
EquiBorrow.Application and EquiBorrow.Domain depend only on abstractions (IStudentRepository,
IEquipmentRepository, IBorrowingRepository) and plain .NET types. Nothing in those projects
references Avalonia, so they have no idea whether the caller is a desktop window, a console app,
or a test runner — they just receive method calls and return results or throw exceptions.
What advantage is gained from registering dependencies in one composition point?
A single composition root is the only place that needs to know which concrete repository or
service implementation is in use. Swapping InMemoryEquipmentRepository for a future
SqliteEquipmentRepository, for example, becomes a one-line change in that one place instead of
a search through every ViewModel that currently constructs repositories with new.
If the in-memory repository were replaced by SQLite later, which parts of the current interface
should remain largely unchanged?
EquiBorrow.Domain, EquiBorrow.Application (interfaces and services), and the Avalonia Views/
ViewModels in EquiBorrow.UI should remain unchanged, since they only depend on the repository
interfaces. Only EquiBorrow.Infrastructure (new Sqlite*Repository classes) and the composition
root's registrations would need to change.
11. Known Gaps Before Final Submission

For transparency, these are the differences between the current EquiBorrow.UI implementation and
the full Laboratory Activity 2 requirements, based on the current state of the code in this archive:

CommunityToolkit.Mvvm is not used. MainViewModel implements INotifyPropertyChanged by hand
and uses a hand-written RelayCommand rather than ObservableObject, [ObservableProperty], and
[RelayCommand] from CommunityToolkit.Mvvm, which Part G asks for.
Borrow logic is duplicated in the ViewModel instead of delegating to BorrowEquipmentService
(see Section 7 above), so the borrowing-limit and inactive-student rules are not actually enforced
from the UI yet.
Return Equipment is not wired into the UI even though ReturnEquipmentServices already exists
in the Application layer (see Section 8 above).
No composition root / DI container is used even though the package is referenced (see Section 9
above); the ViewModel and repositories are still constructed with new in MainWindow.axaml.cs.
No separate EquipmentView / BorrowingsView, so there is no navigation between sections as
described in Part C/Part J — both areas are shown together on one window instead.
Add/Update/Remove Student and Equipment exist in the UI but only mutate the in-memory
ObservableCollections shown on screen; they do not call back into the repositories, so those
changes do not persist the same way borrowing does.
Stray, unreferenced project folders (EquiBorow.Desktop F# template, EquiBorrowing.Console,
EquiBorrow.Console) remain in the repository and should be removed so the submitted structure
matches EquiBorrow.slnx.
EquiBorrow.Tests is still only planned; no automated tests exist yet.

---

1A. Relational Database Design

Student
│
│ 1
│
│ *
Borrowing
*
│
│ 1
│
Equipment

STUDENTS
Id               PK
Name
IsActive

EQUIPMENT
Id               PK
Name
IsAvailable

BORROWINGS
Id               PK
StudentId        FK
EquipmentId      FK
BorrowDate
ExpectedReturnDate
ReturnDate
Status

1B. Relational Database Design
Student
Column      |CLR Type   |SQL Type   |Null?  |Key/Constraint     |Notes
Id          |int        |INTEGER    |no     |PK(PK_STUDENTS)    |Primary Identifier
Name        |string     |TEXT       |no     |MaxLength(200)     |Human name; indexed recommended for lookups
IsActive    |bool       |INTEGER    |no     |NOT NULL           |controls borrow eligibility

Equipment
Column      |CLR Type   |SQL Type   |Null?  |Key/Constraint     |Notes
Id          |int        |INTEGER    |no     |PK(PK_EQUIPMENT)   |Primary Identifier
Name        |string     |TEXT       |no     |MaxLength(200)     |Equipment name
IsAvailable |bool       |INTEGER    |no     |NOT NULL           |availability flag; set false when borrowed

Borrowings
Column              |CLR Type       |SQL Type   |Null?  |Key/Constraint                    |Notes
Id                  |int            |INTEGER    |no     |PK(PK_BORROWING),AUTOINCREMENT    |Primary Identifier
StudentId           |int            |INTEGER    |no     |FK->STUDENTS(ID)                  |ON DELETE RESTRICT. References borrowing student
EquipmentId         |int            |INTEGER    |no     |FK->EQUIPMENT(ID)                 |ON DELETE RESTRICT. References borrowed equipment
BorrowDate          |DateTime       |TEXT       |no     |NOT NULL                          |When Borrowed
ExpectedReturnDate  |DateTime       |TEXT       |no     |NOT NULL                          |Planned return date
Status              |BorrowingStatus|INTEGER    |no     |NOT NULL                          |Enum stored as int (e.g., Active=0, Returned=1)
ReturnDate          |DateTime?      |TEXT       |YES    |NULLABLE                          |Actual Return date; optional

Relationships & Integrity Implementation
One-to-Many Relationships

Students (1) ➝ (M) Borrowings: Linked via Borrowings.StudentId.

Equipment (1) ➝ (M) Borrowings: Linked via Borrowings.EquipmentId.

Data Normalization: Borrowing records store StudentId and EquipmentId only. Student.Name and Equipment.Name are not duplicated inside the Borrowings table, utilizing JOINs to present names and adhering to the requirement to avoid storing unnecessary duplicate information.

Indexes & Constraints

Applied Indexes: IX_Borrowings_StudentId and IX_Borrowings_EquipmentId are present in the migration to optimize foreign key lookups.

Deletion Semantics: ON DELETE RESTRICT is applied to both foreign keys, preventing the deletion of students or equipment that have an existing borrowing history and preserving historical data.

Unique Partial Constraint (Recommended): A unique partial index on Borrowings(EquipmentId) WHERE Status = Active ensures a piece of equipment cannot have more than one active borrowing. (If SQLite portability is a concern, this must be enforced via application logic and transactions).

Lookup Indexes (Recommended): Non-unique indexes on Students.Name and Equipment.Name speed up searches.

Identity Constraints (Recommended): If student identity beyond a numeric Id is required, a unique StudentNumber column with a unique constraint should be added. Students.Id and Equipment.Id can be set to AUTOINCREMENT for database-assigned identity.

Transaction & Atomicity Guidance

A borrow operation must validate both Student.IsActive and Equipment.IsAvailable.

The system must check the student's active-borrow count (IBorrowingRepository.GetActiveCountByStudentIdAsync).

Creating the Borrowing row and setting Equipment.IsAvailable = false must occur inside a single transaction (DbContext transaction or single SaveChanges call in EF Core) to prevent race conditions.

2. SQLite and EF Core

Package Installation: Added Microsoft.EntityFrameworkCore, Microsoft.EntityFrameworkCore.Sqlite, and Microsoft.EntityFrameworkCore.Design to the Infrastructure project using the .NET CLI.

DbContext & Configuration: Created EquipmentBorrowingDbContext with DbSet properties for Student, Equipment, and Borrowing. Entity rules, relationships, and initial seed data (HasData) were defined using IEntityTypeConfiguration within OnModelCreating.

Repository Implementation: Created EF-backed repositories (EfStudentRepository, EfEquipmentRepository, EfBorrowingRepository) to execute asynchronous database queries while adhering to the existing application repository interfaces.

Dependency Injection & Startup: Configured the SQLite provider in Program.cs via services.AddDbContext using options.UseSqlite("Data Source=EquiBorrow.db") and registered the EF repositories as scoped services.

Database Migrations: Generated the InitialCreate migration (dotnet ef migrations add InitialCreate) and configured the application to automatically apply pending migrations and seed data on startup using db.Database.Migrate().

SQL Logging: Implemented EfCommandLoggingInterceptor and an IInspectionService (utilizing DbContext.ToQueryString()) to monitor and log the SQL statements generated by Entity Framework.

3. DBContext

The `EquipmentBorrowingDbContext` acts as the application's EF Core unit of work, mapping domain entities to a relational SQLite database. It exposes `DbSet` properties for Students, Equipment, and Borrowings, and uses `OnModelCreating` to configure schema constraints, relationships, and seed data to drive automated migrations. At runtime, it manages LINQ queries, state tracking, and atomic transactions (via `SaveChanges`).

Architecturally, the context must be registered as a scoped dependency. To maintain clean boundaries, it should strictly be accessed by infrastructure repositories—ensuring that EF Core-specific logic and types never leak into the application's UI or service layers.

4. Repository Transition
The application transitioned from ephemeral in-memory collections to persistent storage by implementing the existing repository interfaces with EF Core-backed repositories connected to a local SQLite database. This architectural shift ensures data survives application restarts, enables powerful LINQ querying, and leverages database-level constraints and transactions for data integrity, all while requiring zero changes to the higher-level application and UI layers.

To support this, the infrastructure layer was expanded to include the `EquipmentBorrowingDbContext`, specific entity configurations, and an `InitialCreate` migration that establishes the schema and initial seed data. Within the application's startup configuration, the DbContext and repositories are now registered as scoped services, and pending migrations are automatically applied on launch. Consequently, multi-entity operations are now executed atomically through a single `SaveChangesAsync` call, database constraints actively reinforce domain rules, and custom SQL logging interceptors capture the query evidence required for lab deliverables.

4. Migration Process
Using the .NET CLI (from the repository root)

To create the migration:
dotnet ef migrations add InitialCreate --project src/EquiBorrow.Infrastructure --startup-project src/EquiBorrow.UI --context EquipmentBorrowingDbContext

To update the database:
dotnet ef database update --project src/EquiBorrow.Infrastructure --startup-project src/EquiBorrow.UI --context EquipmentBorrowingDbContext

Using the Visual Studio Package Manager Console

(Ensure the Default project is set to EquiBorrow.Infrastructure and the Startup project is set to EquiBorrow.UI)

To create the migration:
Add-Migration InitialCreate -Context EquipmentBorrowingDbContext

To update the database:
Update-Database -Context EquipmentBorrowingDbContext

5. Persistence Demonstration

The pair verified database persistence through the following core steps:

Application Test: They performed a write operation in the UI (e.g., borrowing an item), completely closed the application, and restarted it to confirm the changes were still visible and accurately reloaded.

Database Inspection: They located the local EquiBorrow.db file and used a SQLite viewer to manually verify that the tables, new records, and __EFMigrationsHistory were correctly written to the disk.

Log Verification: They reviewed the generated output files (database-queries.sql and ef-commands.log) to confirm that the executed SQL statements were successfully captured during the session.

6. Architectural Reflection

1.	Because the app was layered and depended on repository interfaces; swapping the in-memory implementation for an EF/SQLite implementation only changed the infrastructure layer and DI registration — application and UI code stayed the same.
2.	To maintain separation of concerns and testability: DbContext is an infrastructure/unit-of-work concern with a scoped lifetime and EF-specific behavior that should be hidden behind repository/service abstractions, not leaked into presentation code.
3.	The repository maps domain operations to EF calls: it issues queries and updates via DbContext, persists changes (SaveChanges), enforces data access rules, and encapsulates transaction/CRUD details for the rest of the app.
4.	A migration records model/schema changes as code so EF can generate and apply the correct DDL to the database, keep schema versioning, and include seed data reliably across environments.
5.	Foreign keys enforce referential integrity (prevent orphaned borrow records), enable correct JOINs for queries, and let the database enforce delete/update rules (restrict/cascade) to keep data consistent.
6.	AsNoTracking skips EF change-tracking for the returned entities, reducing CPU and memory overhead for read-only queries and improving query performance.
7.	If you replace SQLite with another provider, only the infrastructure/DbContext provider configuration and possibly migrations/dialect-specific SQL need changes; application and UI that use repository interfaces should remain unchanged (aside from any provider-specific features you used).
