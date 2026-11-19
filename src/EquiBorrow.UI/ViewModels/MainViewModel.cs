using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using EquiBorrow.Application;
using EquiBorrow.Application.Interfaces;
using EquiBorrow.Application.Services;

namespace EquiBorrow.UI.ViewModels;

public sealed class StudentItemViewModel
{
    public int Id { get; init; }
    public string StudentNumber { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Contact { get; init; }
    public bool IsActive { get; init; }
    public string EligibilityStatus => IsActive ? "Eligible" : "Restricted";
    public uint Version { get; init; }
}

public sealed class EquipmentItemViewModel
{
    public int Id { get; init; }
    public string Identifier { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsAvailable { get; init; }
    public uint Version { get; init; }
}

public sealed class BorrowedItemViewModel
{
    public int BorrowingId { get; init; }
    public int EquipmentId { get; init; }
    public string EquipmentName { get; init; } = string.Empty;
    public DateOnly BorrowDate { get; init; }
    public uint Version { get; init; }
}

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly ListStudentsService _listStudentsService;
    private readonly ViewEquipmentCatalogService _viewEquipmentCatalogService;
    private readonly ViewActiveBorrowingsService _viewActiveBorrowingsService;
    private readonly BorrowEquipmentService _borrowEquipmentService;
    private readonly RegisterStudentService _registerStudentService;
    private readonly RegisterEquipmentService _registerEquipmentService;
    private readonly UpdateStudentService _updateStudentService;
    private readonly UpdateEquipmentService _updateEquipmentService;
    private readonly ChangeStudentEligibilityService _changeStudentEligibilityService;
    private readonly IInspectionService _inspectionService;
    private readonly TimeProvider _timeProvider;

    public ObservableCollection<StudentItemViewModel> Students { get; } = new();
    public ObservableCollection<EquipmentItemViewModel> Equipments { get; } = new();
    public ObservableCollection<BorrowedItemViewModel> StudentBorrows { get; } = new();

    private StudentItemViewModel? _selectedStudent;
    public StudentItemViewModel? SelectedStudent
    {
        get => _selectedStudent;
        set
        {
            if (_selectedStudent == value) return;
            _selectedStudent = value;
            OnPropertyChanged(nameof(SelectedStudent));
            UpdateStudentCommand?.RaiseCanExecuteChanged();
            PermitBorrowingCommand?.RaiseCanExecuteChanged();
            RestrictBorrowingCommand?.RaiseCanExecuteChanged();
            OnPropertyChanged(nameof(SelectedStudentEligibility));
            EditStudentNumber = value?.StudentNumber ?? string.Empty;
            EditStudentName = value?.Name ?? string.Empty;
            EditStudentContact = value?.Contact ?? string.Empty;
            StudentBorrows.Clear();
            if (value is not null)
                Observe(LoadStudentBorrowingsAsync(value.Id, updateStatusMessage: !_suppressBorrowingStatusMessage));
        }
    }

    private EquipmentItemViewModel? _selectedEquipment;
    public EquipmentItemViewModel? SelectedEquipment
    {
        get => _selectedEquipment;
        set
        {
            if (_selectedEquipment == value) return;
            _selectedEquipment = value;
            OnPropertyChanged(nameof(SelectedEquipment));
            UpdateEquipmentCommand?.RaiseCanExecuteChanged();
            EditEquipmentIdentifier = value?.Identifier ?? string.Empty;
            EditEquipmentName = value?.Name ?? string.Empty;
            EditEquipmentDescription = value?.Description ?? string.Empty;
        }
    }

    public string SelectedStudentEligibility => SelectedStudent is null
        ? "Select a student to view eligibility."
        : SelectedStudent.IsActive ? "Eligible to borrow" : "Restricted from borrowing";

    private string _newStudentNumber = string.Empty;
    public string NewStudentNumber { get => _newStudentNumber; set { _newStudentNumber = value; OnPropertyChanged(nameof(NewStudentNumber)); } }

    private string _newStudentName = string.Empty;
    public string NewStudentName { get => _newStudentName; set { _newStudentName = value; OnPropertyChanged(nameof(NewStudentName)); } }

    private string _newStudentContact = string.Empty;
    public string NewStudentContact { get => _newStudentContact; set { _newStudentContact = value; OnPropertyChanged(nameof(NewStudentContact)); } }

    private string _newEquipmentIdentifier = string.Empty;
    public string NewEquipmentIdentifier { get => _newEquipmentIdentifier; set { _newEquipmentIdentifier = value; OnPropertyChanged(nameof(NewEquipmentIdentifier)); } }

    private string _newEquipmentName = string.Empty;
    public string NewEquipmentName { get => _newEquipmentName; set { _newEquipmentName = value; OnPropertyChanged(nameof(NewEquipmentName)); } }

    private string _newEquipmentDescription = string.Empty;
    public string NewEquipmentDescription { get => _newEquipmentDescription; set { _newEquipmentDescription = value; OnPropertyChanged(nameof(NewEquipmentDescription)); } }

    private string _editStudentNumber = string.Empty;
    public string EditStudentNumber { get => _editStudentNumber; set { _editStudentNumber = value; OnPropertyChanged(nameof(EditStudentNumber)); } }

    private string _editStudentName = string.Empty;
    public string EditStudentName { get => _editStudentName; set { _editStudentName = value; OnPropertyChanged(nameof(EditStudentName)); } }

    private string _editStudentContact = string.Empty;
    public string EditStudentContact { get => _editStudentContact; set { _editStudentContact = value; OnPropertyChanged(nameof(EditStudentContact)); } }

    private string _editEquipmentIdentifier = string.Empty;
    public string EditEquipmentIdentifier { get => _editEquipmentIdentifier; set { _editEquipmentIdentifier = value; OnPropertyChanged(nameof(EditEquipmentIdentifier)); } }

    private string _editEquipmentName = string.Empty;
    public string EditEquipmentName { get => _editEquipmentName; set { _editEquipmentName = value; OnPropertyChanged(nameof(EditEquipmentName)); } }

    private string _editEquipmentDescription = string.Empty;
    public string EditEquipmentDescription { get => _editEquipmentDescription; set { _editEquipmentDescription = value; OnPropertyChanged(nameof(EditEquipmentDescription)); } }

    private string _statusMessage = "Available in a later step";
    private string _studentsMessage = string.Empty;
    private string _equipmentMessage = string.Empty;
    public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(nameof(StatusMessage)); } }

    public RelayCommand AddStudentCommand { get; }
    public RelayCommand UpdateStudentCommand { get; }
    public RelayCommand PermitBorrowingCommand { get; }
    public RelayCommand RestrictBorrowingCommand { get; }
    public RelayCommand AddEquipmentCommand { get; }
    public RelayCommand UpdateEquipmentCommand { get; }
    public RelayCommand BorrowCommand { get; }
    public RelayCommand RefreshCommand { get; }
    public RelayCommand InspectSqlCommand { get; }

    private bool _isBusy;
    private bool _suppressBorrowingStatusMessage;
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy == value) return;
            _isBusy = value;
            OnPropertyChanged(nameof(IsBusy));
            AddStudentCommand?.RaiseCanExecuteChanged();
            AddEquipmentCommand?.RaiseCanExecuteChanged();
            UpdateStudentCommand?.RaiseCanExecuteChanged();
            PermitBorrowingCommand?.RaiseCanExecuteChanged();
            RestrictBorrowingCommand?.RaiseCanExecuteChanged();
            UpdateEquipmentCommand?.RaiseCanExecuteChanged();
            BorrowCommand?.RaiseCanExecuteChanged();
            RefreshCommand?.RaiseCanExecuteChanged();
            InspectSqlCommand?.RaiseCanExecuteChanged();
        }
    }

    public MainViewModel(
        ListStudentsService listStudentsService,
        ViewEquipmentCatalogService viewEquipmentCatalogService,
        ViewActiveBorrowingsService viewActiveBorrowingsService,
        BorrowEquipmentService borrowEquipmentService,
        RegisterStudentService registerStudentService,
        RegisterEquipmentService registerEquipmentService,
        UpdateStudentService updateStudentService,
        UpdateEquipmentService updateEquipmentService,
        ChangeStudentEligibilityService changeStudentEligibilityService,
        IInspectionService inspectionService,
        TimeProvider timeProvider)
    {
        _listStudentsService = listStudentsService ?? throw new ArgumentNullException(nameof(listStudentsService));
        _viewEquipmentCatalogService = viewEquipmentCatalogService ?? throw new ArgumentNullException(nameof(viewEquipmentCatalogService));
        _viewActiveBorrowingsService = viewActiveBorrowingsService ?? throw new ArgumentNullException(nameof(viewActiveBorrowingsService));
        _borrowEquipmentService = borrowEquipmentService ?? throw new ArgumentNullException(nameof(borrowEquipmentService));
        _registerStudentService = registerStudentService ?? throw new ArgumentNullException(nameof(registerStudentService));
        _registerEquipmentService = registerEquipmentService ?? throw new ArgumentNullException(nameof(registerEquipmentService));
        _updateStudentService = updateStudentService ?? throw new ArgumentNullException(nameof(updateStudentService));
        _updateEquipmentService = updateEquipmentService ?? throw new ArgumentNullException(nameof(updateEquipmentService));
        _changeStudentEligibilityService = changeStudentEligibilityService ?? throw new ArgumentNullException(nameof(changeStudentEligibilityService));
        _inspectionService = inspectionService ?? throw new ArgumentNullException(nameof(inspectionService));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

        AddStudentCommand = new RelayCommand(_ => RunCommandAsync(RegisterStudentAsync), _ => !IsBusy);
        UpdateStudentCommand = new RelayCommand(_ => RunCommandAsync(UpdateStudentAsync), _ => SelectedStudent is not null && !IsBusy);
        PermitBorrowingCommand = new RelayCommand(_ => RunCommandAsync(() => ChangeStudentEligibilityAsync(true)), _ => SelectedStudent is not null && !IsBusy);
        RestrictBorrowingCommand = new RelayCommand(_ => RunCommandAsync(() => ChangeStudentEligibilityAsync(false)), _ => SelectedStudent is not null && !IsBusy);
        AddEquipmentCommand = new RelayCommand(_ => RunCommandAsync(RegisterEquipmentAsync), _ => !IsBusy);
        UpdateEquipmentCommand = new RelayCommand(_ => RunCommandAsync(UpdateEquipmentAsync), _ => SelectedEquipment is not null && !IsBusy);
        BorrowCommand = new RelayCommand(_ => RunCommandAsync(BorrowAsync), _ => !IsBusy);
        RefreshCommand = new RelayCommand(_ => RunCommandAsync(RefreshAsync), _ => !IsBusy);
        InspectSqlCommand = new RelayCommand(_ => RunCommandAsync(InspectSqlAsync), _ => !IsBusy);

        RunCommandAsync(LoadDataAsync);
    }

    private async Task LoadDataAsync()
    {
        await LoadStudentsAsync();
        await LoadEquipmentAsync();
        StatusMessage = $"{_studentsMessage} {_equipmentMessage} Available in a later step";
    }

    private async Task LoadStudentsAsync()
    {
        var result = await _listStudentsService.ExecuteAsync();
        _studentsMessage = result.Message;
        if (result.Outcome != OperationOutcome.Success || result.Value is null)
        {
            StatusMessage = result.Message;
            return;
        }

        Students.Clear();
        foreach (var student in result.Value)
        {
            Students.Add(new StudentItemViewModel
            {
                Id = student.Id,
                StudentNumber = student.StudentNumber,
                Name = student.Name,
                Contact = student.Contact,
                IsActive = student.IsActive,
                Version = student.Version
            });
        }
        StatusMessage = result.Message;
    }

    private async Task LoadEquipmentAsync()
    {
        var result = await _viewEquipmentCatalogService.ExecuteAsync();
        _equipmentMessage = result.Message;
        if (result.Outcome != OperationOutcome.Success || result.Value is null)
        {
            StatusMessage = result.Message;
            return;
        }

        Equipments.Clear();
        foreach (var equipment in result.Value)
        {
            Equipments.Add(new EquipmentItemViewModel
            {
                Id = equipment.Id,
                Identifier = equipment.Identifier,
                Name = equipment.Name,
                Description = equipment.Description,
                IsAvailable = equipment.IsAvailable,
                Version = equipment.Version
            });
        }
        StatusMessage = result.Message;
    }

    private async Task LoadStudentBorrowingsAsync(int studentId, bool updateStatusMessage = true)
    {
        var result = await _viewActiveBorrowingsService.ExecuteAsync(studentId);
        if (result.Outcome != OperationOutcome.Success || result.Value is null)
        {
            if (updateStatusMessage)
                StatusMessage = result.Message;
            return;
        }

        StudentBorrows.Clear();
        foreach (var borrowing in result.Value)
        {
            StudentBorrows.Add(new BorrowedItemViewModel
            {
                BorrowingId = borrowing.BorrowingId,
                EquipmentId = borrowing.EquipmentId,
                EquipmentName = borrowing.EquipmentName,
                BorrowDate = borrowing.BorrowDate,
                Version = borrowing.Version
            });
        }
        if (updateStatusMessage)
            StatusMessage = result.Message;
    }

    private async Task RegisterStudentAsync()
    {
        var result = await _registerStudentService.ExecuteAsync(NewStudentNumber, NewStudentName, NewStudentContact);
        if (result.Outcome == OperationOutcome.Success && result.Value is not null)
        {
            NewStudentNumber = string.Empty;
            NewStudentName = string.Empty;
            NewStudentContact = string.Empty;
            await ReloadStudentsAndReselectAsync(result.Value.Id);
            StatusMessage = result.Message;
        }
        else
        {
            if (result.Outcome == OperationOutcome.UnknownOutcome)
                await LoadStudentsAsync();
            StatusMessage = result.Message;
        }
    }

    private async Task RegisterEquipmentAsync()
    {
        var result = await _registerEquipmentService.ExecuteAsync(NewEquipmentIdentifier, NewEquipmentName, NewEquipmentDescription);
        if (result.Outcome == OperationOutcome.Success && result.Value is not null)
        {
            NewEquipmentIdentifier = string.Empty;
            NewEquipmentName = string.Empty;
            NewEquipmentDescription = string.Empty;
            await LoadEquipmentAsync();
            SelectedEquipment = FindEquipment(result.Value.Id);
            StatusMessage = result.Message;
        }
        else
        {
            if (result.Outcome == OperationOutcome.UnknownOutcome)
                await LoadEquipmentAsync();
            StatusMessage = result.Message;
        }
    }

    private StudentItemViewModel? FindStudent(int id)
    {
        foreach (var student in Students)
        {
            if (student.Id == id)
                return student;
        }
        return null;
    }

    private EquipmentItemViewModel? FindEquipment(int id)
    {
        foreach (var equipment in Equipments)
        {
            if (equipment.Id == id)
                return equipment;
        }
        return null;
    }

    private async Task ReloadStudentsAndReselectAsync(int studentId)
    {
        _suppressBorrowingStatusMessage = true;
        try
        {
            SelectedStudent = null;
            await LoadStudentsAsync();
            SelectedStudent = FindStudent(studentId);
        }
        finally
        {
            _suppressBorrowingStatusMessage = false;
        }
    }

    private async Task UpdateStudentAsync()
    {
        var selected = SelectedStudent;
        if (selected is null)
            return;

        var result = await _updateStudentService.ExecuteAsync(
            selected.Id, selected.Version, EditStudentNumber, EditStudentName, EditStudentContact);
        if (result.Outcome is OperationOutcome.Success or OperationOutcome.Stale or OperationOutcome.UnknownOutcome)
            await ReloadStudentsAndReselectAsync(selected.Id);
        StatusMessage = result.Message;
    }

    private async Task UpdateEquipmentAsync()
    {
        var selected = SelectedEquipment;
        if (selected is null)
            return;

        var result = await _updateEquipmentService.ExecuteAsync(
            selected.Id, selected.Version, EditEquipmentIdentifier, EditEquipmentName, EditEquipmentDescription);
        if (result.Outcome is OperationOutcome.Success or OperationOutcome.Stale or OperationOutcome.UnknownOutcome)
        {
            SelectedEquipment = null;
            await LoadEquipmentAsync();
            SelectedEquipment = FindEquipment(selected.Id);
        }
        StatusMessage = result.Message;
    }

    private async Task ChangeStudentEligibilityAsync(bool isEligible)
    {
        var selected = SelectedStudent;
        if (selected is null)
            return;

        var result = await _changeStudentEligibilityService.ExecuteAsync(selected.Id, selected.Version, isEligible);
        if (result.Outcome is OperationOutcome.Success or OperationOutcome.Stale or OperationOutcome.UnknownOutcome)
            await ReloadStudentsAndReselectAsync(selected.Id);
        StatusMessage = result.Message;
    }

    private async Task BorrowAsync()
    {
        if (SelectedStudent is null) { StatusMessage = "Select a student to borrow."; return; }
        if (SelectedEquipment is null) { StatusMessage = "Select equipment to borrow."; return; }

        var studentId = SelectedStudent.Id;
        var equipmentId = SelectedEquipment.Id;
        // TODO: Replace this temporary date with the UC-03 date picker.
        var expectedReturnDate = DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime).AddDays(7);
        var result = await _borrowEquipmentService.ExecuteAsync(studentId, equipmentId, expectedReturnDate);
        if (result.Outcome is OperationOutcome.Success or OperationOutcome.Stale)
        {
            await LoadEquipmentAsync();
            await LoadStudentBorrowingsAsync(studentId);
        }
        StatusMessage = result.Message;
    }

    private async Task RefreshAsync()
    {
        await LoadDataAsync();
        if (SelectedStudent is not null)
            await LoadStudentBorrowingsAsync(SelectedStudent.Id);
    }

    private async Task InspectSqlAsync()
    {
        var sql = await _inspectionService.GetGeneratedSqlAsync();
        var outputPath = Path.Combine("logs", "generated-sql.sql");
        await File.WriteAllTextAsync(outputPath, sql);
        StatusMessage = $"Generated SQL written to {outputPath}";
    }

    private void RunCommandAsync(Func<Task> operation)
    {
        if (IsBusy) return;
        IsBusy = true;
        Observe(ExecuteCommandAsync(operation));
    }

    private async Task ExecuteCommandAsync(Func<Task> operation)
    {
        try
        {
            await operation();
        }
        catch (Exception exception)
        {
            ReportUnexpectedException(exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Observe(Task task) => _ = ObserveAsync(task);

    private async Task ObserveAsync(Task task)
    {
        try
        {
            await task;
        }
        catch (Exception exception)
        {
            ReportUnexpectedException(exception);
        }
    }

    private void ReportUnexpectedException(Exception exception)
    {
        try
        {
            Directory.CreateDirectory("logs");
            File.AppendAllText(
                Path.Combine("logs", "app.log"),
                $"{DateTimeOffset.Now:O}{Environment.NewLine}{exception}{Environment.NewLine}{Environment.NewLine}");
            StatusMessage = "An unexpected error occurred. Details were written to logs/app.log.";
        }
        catch (IOException)
        {
            StatusMessage = "An unexpected error occurred. Details could not be written to logs/app.log.";
        }
        catch (UnauthorizedAccessException)
        {
            StatusMessage = "An unexpected error occurred. Details could not be written to logs/app.log.";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
