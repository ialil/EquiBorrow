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
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public uint Version { get; init; }
}

public sealed class EquipmentItemViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
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
            StudentBorrows.Clear();
            if (value is not null)
                Observe(LoadStudentBorrowingsAsync(value.Id));
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
        }
    }

    private string _newStudentName = string.Empty;
    public string NewStudentName { get => _newStudentName; set { _newStudentName = value; OnPropertyChanged(nameof(NewStudentName)); } }

    private bool _newStudentIsActive;
    public bool NewStudentIsActive { get => _newStudentIsActive; set { _newStudentIsActive = value; OnPropertyChanged(nameof(NewStudentIsActive)); } }

    private string _newEquipmentName = string.Empty;
    public string NewEquipmentName { get => _newEquipmentName; set { _newEquipmentName = value; OnPropertyChanged(nameof(NewEquipmentName)); } }

    private bool _newEquipmentIsAvailable;
    public bool NewEquipmentIsAvailable { get => _newEquipmentIsAvailable; set { _newEquipmentIsAvailable = value; OnPropertyChanged(nameof(NewEquipmentIsAvailable)); } }

    private string _statusMessage = "Available in a later step";
    private string _studentsMessage = string.Empty;
    private string _equipmentMessage = string.Empty;
    public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(nameof(StatusMessage)); } }

    public RelayCommand AddStudentCommand { get; }
    public RelayCommand UpdateStudentCommand { get; }
    public RelayCommand AddEquipmentCommand { get; }
    public RelayCommand UpdateEquipmentCommand { get; }
    public RelayCommand BorrowCommand { get; }
    public RelayCommand RefreshCommand { get; }
    public RelayCommand InspectSqlCommand { get; }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy == value) return;
            _isBusy = value;
            OnPropertyChanged(nameof(IsBusy));
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
        IInspectionService inspectionService,
        TimeProvider timeProvider)
    {
        _listStudentsService = listStudentsService ?? throw new ArgumentNullException(nameof(listStudentsService));
        _viewEquipmentCatalogService = viewEquipmentCatalogService ?? throw new ArgumentNullException(nameof(viewEquipmentCatalogService));
        _viewActiveBorrowingsService = viewActiveBorrowingsService ?? throw new ArgumentNullException(nameof(viewActiveBorrowingsService));
        _borrowEquipmentService = borrowEquipmentService ?? throw new ArgumentNullException(nameof(borrowEquipmentService));
        _inspectionService = inspectionService ?? throw new ArgumentNullException(nameof(inspectionService));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

        AddStudentCommand = new RelayCommand(_ => StatusMessage = "Available in a later step", _ => false);
        UpdateStudentCommand = new RelayCommand(_ => StatusMessage = "Available in a later step", _ => false);
        AddEquipmentCommand = new RelayCommand(_ => StatusMessage = "Available in a later step", _ => false);
        UpdateEquipmentCommand = new RelayCommand(_ => StatusMessage = "Available in a later step", _ => false);
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
                Name = student.Name,
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
                Name = equipment.Name,
                IsAvailable = equipment.IsAvailable,
                Version = equipment.Version
            });
        }
        StatusMessage = result.Message;
    }

    private async Task LoadStudentBorrowingsAsync(int studentId)
    {
        var result = await _viewActiveBorrowingsService.ExecuteAsync(studentId);
        if (result.Outcome != OperationOutcome.Success || result.Value is null)
        {
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
        var baseDirectory = AppContext.BaseDirectory;
        var outputPath = Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", "docs", "database-queries.sql"));
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? "docs");
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
