

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
