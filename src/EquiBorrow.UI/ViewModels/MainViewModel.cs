using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using EquiBorrow.Application.Interfaces;
using EquiBorrow.Domain;

namespace EquiBorrow.UI.ViewModels;

public class StudentItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class EquipmentItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
}

public class BorrowedItemViewModel
{
    public int EquipmentId { get; set; }
    public string EquipmentName { get; set; } = string.Empty;
    public DateTime BorrowDate { get; set; }
}

public class MainViewModel : INotifyPropertyChanged
{
    private readonly IStudentRepository _studentRepository;
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly IBorrowingRepository _borrowingRepository;
    private readonly IInspectionService _inspectionService;

    public ObservableCollection<StudentItemViewModel> Students { get; } = new();
    public ObservableCollection<EquipmentItemViewModel> Equipments { get; } = new();
    public ObservableCollection<BorrowedItemViewModel> StudentBorrows { get; } = new();

    private StudentItemViewModel? _selectedStudent;
    public StudentItemViewModel? SelectedStudent 
    { 
        get => _selectedStudent; 
        set 
        { 
            _selectedStudent = value; 
            OnPropertyChanged(nameof(SelectedStudent));
            _ = RefreshStudentBorrowsAsync();
        } 
    }

    private EquipmentItemViewModel? _selectedEquipment;
    public EquipmentItemViewModel? SelectedEquipment { get => _selectedEquipment; set { _selectedEquipment = value; OnPropertyChanged(nameof(SelectedEquipment)); } }

    private string _newStudentName = string.Empty;
    public string NewStudentName { get => _newStudentName; set { _newStudentName = value; OnPropertyChanged(nameof(NewStudentName)); } }

    private bool _newStudentIsActive;
    public bool NewStudentIsActive { get => _newStudentIsActive; set { _newStudentIsActive = value; OnPropertyChanged(nameof(NewStudentIsActive)); } }

    private string _newEquipmentName = string.Empty;
    public string NewEquipmentName { get => _newEquipmentName; set { _newEquipmentName = value; OnPropertyChanged(nameof(NewEquipmentName)); } }

    private bool _newEquipmentIsAvailable;
    public bool NewEquipmentIsAvailable { get => _newEquipmentIsAvailable; set { _newEquipmentIsAvailable = value; OnPropertyChanged(nameof(NewEquipmentIsAvailable)); } }

    private string _statusMessage = string.Empty;
    public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(nameof(StatusMessage)); } }

    public RelayCommand AddStudentCommand { get; }
    public RelayCommand UpdateStudentCommand { get; }
    public RelayCommand RemoveStudentCommand { get; }
    public RelayCommand AddEquipmentCommand { get; }
    public RelayCommand UpdateEquipmentCommand { get; }
    public RelayCommand RemoveEquipmentCommand { get; }
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
            // Update command availability
            BorrowCommand?.RaiseCanExecuteChanged();
            AddStudentCommand?.RaiseCanExecuteChanged();
            UpdateStudentCommand?.RaiseCanExecuteChanged();
            RemoveStudentCommand?.RaiseCanExecuteChanged();
            AddEquipmentCommand?.RaiseCanExecuteChanged();
            UpdateEquipmentCommand?.RaiseCanExecuteChanged();
            RemoveEquipmentCommand?.RaiseCanExecuteChanged();
            InspectSqlCommand?.RaiseCanExecuteChanged();
        }
    }

    public MainViewModel(IStudentRepository studentRepository, IEquipmentRepository equipmentRepository, IBorrowingRepository borrowingRepository, IInspectionService inspectionService)
    {
        _studentRepository = studentRepository ?? throw new ArgumentNullException(nameof(studentRepository));
        _equipmentRepository = equipmentRepository ?? throw new ArgumentNullException(nameof(equipmentRepository));
        _borrowingRepository = borrowingRepository ?? throw new ArgumentNullException(nameof(borrowingRepository));
        _inspectionService = inspectionService ?? throw new ArgumentNullException(nameof(inspectionService));

        AddStudentCommand = new RelayCommand(_ => AddStudentAsync());
        UpdateStudentCommand = new RelayCommand(_ => UpdateStudentAsync());
        RemoveStudentCommand = new RelayCommand(_ => RemoveStudentAsync());

        AddEquipmentCommand = new RelayCommand(_ => AddEquipment(), _ => !IsBusy);
        UpdateEquipmentCommand = new RelayCommand(_ => UpdateEquipment(), _ => !IsBusy);
        RemoveEquipmentCommand = new RelayCommand(_ => RemoveEquipment(), _ => !IsBusy);

        InspectSqlCommand = new RelayCommand(_ => InspectSqlAsync(), _ => !IsBusy);

        BorrowCommand = new RelayCommand(_ => BorrowAsync(), _ => !IsBusy);
        RefreshCommand = new RelayCommand(_ => Refresh());

        // InspectSqlCommand already assigned with CanExecute predicate above

        // Load data from repositories
        LoadDataAsync();
    }

    private async void LoadDataAsync()
    {
        await LoadStudentsAsync();
        await LoadEquipmentAsync();
    }

    private async Task LoadStudentsAsync()
    {
        var students = await _studentRepository.GetAllAsync();
        Students.Clear();
        foreach (var student in students.OrderBy(s => s.Name))
        {
            Students.Add(new StudentItemViewModel 
            { 
                Id = student.Id, 
                Name = student.Name, 
                IsActive = student.IsActive 
            });
        }
        StatusMessage = $"Loaded {students.Count} students.";
    }

    private async Task LoadEquipmentAsync()
    {
        var equipments = await _equipmentRepository.GetAllAsync();
        Equipments.Clear();
        foreach (var equipment in equipments.OrderBy(e => e.Name))
        {
            Equipments.Add(new EquipmentItemViewModel 
            { 
                Id = equipment.Id, 
                Name = equipment.Name, 
                IsAvailable = equipment.IsAvailable 
            });
        }
        StatusMessage = $"Loaded {equipments.Count} equipment items.";
    }

    private async void AddStudentAsync()
    {
        if (string.IsNullOrWhiteSpace(NewStudentName)) { StatusMessage = "Student name required."; return; }
        IsBusy = true;
        try
        {
            var domainStudent = new Student { Name = NewStudentName, IsActive = NewStudentIsActive };
            await _studentRepository.AddAsync(domainStudent);
            await LoadStudentsAsync();
            NewStudentName = string.Empty;
            StatusMessage = "Student added.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error adding student: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    private async void InspectSqlAsync()
    {
        IsBusy = true;
        try
        {
            var sql = await _inspectionService.GetGeneratedSqlAsync();
            // Write to docs/database-queries.sql (lab-required filename)
            try
            {
                // Write to the repository docs folder (resolve relative to app base directory)
                var baseDir = AppContext.BaseDirectory ?? string.Empty;
                var repoDocsPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, "..", "..", "..", "docs", "database-queries.sql"));
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(repoDocsPath) ?? "docs");
                System.IO.File.WriteAllText(repoDocsPath, sql);
                StatusMessage = $"Generated SQL written to {repoDocsPath}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"SQL retrieved but failed to write file: {ex.Message}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error inspecting SQL: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    private async void UpdateStudentAsync()
    {
        if (SelectedStudent == null) { StatusMessage = "Select a student to update."; return; }
        IsBusy = true;
        try
        {
            var domainStudent = new Student { Id = SelectedStudent.Id, Name = NewStudentName, IsActive = NewStudentIsActive };
            await _studentRepository.UpdateAsync(domainStudent);
            await LoadStudentsAsync();
            StatusMessage = "Student updated.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error updating student: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    private async void RemoveStudentAsync()
    {
        if (SelectedStudent == null) { StatusMessage = "Select a student to remove."; return; }
        IsBusy = true;
        try
        {
            await _studentRepository.DeleteAsync(SelectedStudent.Id);
            await LoadStudentsAsync();
            SelectedStudent = null;
            StatusMessage = "Student removed.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error removing student: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    private void AddEquipment()
    {
        AddEquipmentAsync();
    }

    private async void AddEquipmentAsync()
    {
        if (string.IsNullOrWhiteSpace(NewEquipmentName)) { StatusMessage = "Equipment name required."; return; }
        IsBusy = true;
        try
        {
            var domainEquipment = new Equipment { Name = NewEquipmentName, IsAvailable = NewEquipmentIsAvailable };
            await _equipmentRepository.AddAsync(domainEquipment);
            await LoadEquipmentAsync();
            NewEquipmentName = string.Empty;
            StatusMessage = "Equipment added.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error adding equipment: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    private void UpdateEquipment()
    {
        if (SelectedEquipment == null) { StatusMessage = "Select equipment to update."; return; }
        SelectedEquipment.Name = NewEquipmentName;
        SelectedEquipment.IsAvailable = NewEquipmentIsAvailable;
        StatusMessage = "Equipment updated.";
    }

    private void RemoveEquipment()
    {
        if (SelectedEquipment == null) { StatusMessage = "Select equipment to remove."; return; }
        RemoveEquipmentAsync();
    }

    private async void RemoveEquipmentAsync()
    {
        if (SelectedEquipment == null) { StatusMessage = "Select equipment to remove."; return; }
        IsBusy = true;
        try
        {
            await _equipmentRepository.DeleteAsync(SelectedEquipment.Id);
            await LoadEquipmentAsync();
            SelectedEquipment = null;
            StatusMessage = "Equipment removed.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error removing equipment: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    private async void BorrowAsync()
    {
        if (SelectedStudent == null) { StatusMessage = "Select a student to borrow."; return; }
        if (SelectedEquipment == null) { StatusMessage = "Select equipment to borrow."; return; }
        if (!SelectedEquipment.IsAvailable) { StatusMessage = "Equipment not available."; return; }
        try
        {
            // Disable UI selection to avoid duplicate clicks (caller will refresh UI state)
            SelectedEquipment.IsAvailable = false;

            // Update in repository (persist equipment availability)
            var equipment = await _equipment_repository_GetForUpdateAsync(SelectedEquipment.Id);
            if (equipment == null)
            {
                StatusMessage = "Equipment not found in repository.";
                return;
            }

            equipment.IsAvailable = false;
            await _equipmentRepository.UpdateAsync(equipment);

            // Capture display names before refreshing UI (refresh may replace view-model objects)
            var studentName = SelectedStudent?.Name ?? "<unknown student>";
            var equipmentName = SelectedEquipment?.Name ?? "<unknown equipment>";

            // Create borrowing record in repository
            var borrowing = new Borrowing(0, SelectedStudent.Id, SelectedEquipment.Id, DateTime.Now, DateTime.Now.AddDays(14));
            await _borrowingRepository.AddAsync(borrowing);

            // Refresh UI lists so equipment availability and borrow history reflect persisted state
            var selectedEquipmentId = SelectedEquipment?.Id;
            await LoadEquipmentAsync();
            if (selectedEquipmentId != null)
            {
                var reselect = Equipments.FirstOrDefault(e => e.Id == selectedEquipmentId.Value);
                if (reselect != null)
                    SelectedEquipment = reselect;
            }
            await RefreshStudentBorrowsAsync();

            StatusMessage = $"{studentName} borrowed {equipmentName}.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error borrowing equipment: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Helper to get equipment for update; preserves existing repository abstraction
    private async Task<Equipment?> _equipment_repository_GetForUpdateAsync(int id)
    {
        return await _equipmentRepository.GetByIdAsync(id);
    }

    private async Task RefreshStudentBorrowsAsync()
    {
        StudentBorrows.Clear();
        if (SelectedStudent == null)
            return;

        var borrowings = await _borrowingRepository.GetAllAsync();
        var studentBorrows = borrowings
            .Where(b => b.StudentId == SelectedStudent.Id && b.Status == BorrowingStatus.Active)
            .ToList();

        foreach (var borrow in studentBorrows)
        {
            var equipment = await _equipmentRepository.GetByIdAsync(borrow.EquipmentId);
            if (equipment != null)
            {
                StudentBorrows.Add(new BorrowedItemViewModel
                {
                    EquipmentId = equipment.Id,
                    EquipmentName = equipment.Name,
                    BorrowDate = borrow.BorrowDate
                });
            }
        }
    }

    private void Refresh() => StatusMessage = "Refreshed.";

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
