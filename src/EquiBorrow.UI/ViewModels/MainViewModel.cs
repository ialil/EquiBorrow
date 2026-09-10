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
            RefreshStudentBorrowsAsync();
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

    public MainViewModel(IStudentRepository studentRepository, IEquipmentRepository equipmentRepository, IBorrowingRepository borrowingRepository)
    {
        _studentRepository = studentRepository ?? throw new ArgumentNullException(nameof(studentRepository));
        _equipmentRepository = equipmentRepository ?? throw new ArgumentNullException(nameof(equipmentRepository));
        _borrowingRepository = borrowingRepository ?? throw new ArgumentNullException(nameof(borrowingRepository));

        AddStudentCommand = new RelayCommand(_ => AddStudent());
        UpdateStudentCommand = new RelayCommand(_ => UpdateStudent());
        RemoveStudentCommand = new RelayCommand(_ => RemoveStudent());

        AddEquipmentCommand = new RelayCommand(_ => AddEquipment());
        UpdateEquipmentCommand = new RelayCommand(_ => UpdateEquipment());
        RemoveEquipmentCommand = new RelayCommand(_ => RemoveEquipment());

        BorrowCommand = new RelayCommand(_ => BorrowAsync());
        RefreshCommand = new RelayCommand(_ => Refresh());

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

    private void AddStudent()
    {
        if (string.IsNullOrWhiteSpace(NewStudentName)) { StatusMessage = "Student name required."; return; }
        var s = new StudentItemViewModel { Id = Students.Count + 1, Name = NewStudentName, IsActive = NewStudentIsActive };
        Students.Add(s);
        NewStudentName = string.Empty;
        StatusMessage = "Student added.";
    }

    private void UpdateStudent()
    {
        if (SelectedStudent == null) { StatusMessage = "Select a student to update."; return; }
        SelectedStudent.Name = NewStudentName;
        SelectedStudent.IsActive = NewStudentIsActive;
        StatusMessage = "Student updated.";
    }

    private void RemoveStudent()
    {
        if (SelectedStudent == null) { StatusMessage = "Select a student to remove."; return; }
        Students.Remove(SelectedStudent);
        SelectedStudent = null;
        StatusMessage = "Student removed.";
    }

    private void AddEquipment()
    {
        if (string.IsNullOrWhiteSpace(NewEquipmentName)) { StatusMessage = "Equipment name required."; return; }
        var e = new EquipmentItemViewModel { Id = Equipments.Count + 1, Name = NewEquipmentName, IsAvailable = NewEquipmentIsAvailable };
        Equipments.Add(e);
        NewEquipmentName = string.Empty;
        StatusMessage = "Equipment added.";
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
        Equipments.Remove(SelectedEquipment);
        SelectedEquipment = null;
        StatusMessage = "Equipment removed.";
    }

    private async void BorrowAsync()
    {
        if (SelectedStudent == null) { StatusMessage = "Select a student to borrow."; return; }
        if (SelectedEquipment == null) { StatusMessage = "Select equipment to borrow."; return; }
        if (!SelectedEquipment.IsAvailable) { StatusMessage = "Equipment not available."; return; }

        // Mark equipment as unavailable
        SelectedEquipment.IsAvailable = false;

        // Update in repository
        var equipment = await _equipmentRepository.GetByIdAsync(SelectedEquipment.Id);
        if (equipment != null)
        {
            equipment.IsAvailable = false;
            await _equipmentRepository.UpdateAsync(equipment);
        }

        // Create borrowing record in repository
        var borrowing = new Borrowing(0, SelectedStudent.Id, SelectedEquipment.Id, DateTime.Now, DateTime.Now.AddDays(14));
        await _borrowingRepository.AddAsync(borrowing);

        await RefreshStudentBorrowsAsync();
        StatusMessage = $"{SelectedStudent.Name} borrowed {SelectedEquipment.Name}.";
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
