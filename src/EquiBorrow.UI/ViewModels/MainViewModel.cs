using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;

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

public class MainViewModel : INotifyPropertyChanged
{
    public ObservableCollection<StudentItemViewModel> Students { get; } = new();
    public ObservableCollection<EquipmentItemViewModel> Equipments { get; } = new();

    private StudentItemViewModel? _selectedStudent;
    public StudentItemViewModel? SelectedStudent { get => _selectedStudent; set { _selectedStudent = value; OnPropertyChanged(nameof(SelectedStudent)); } }

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

    public MainViewModel()
    {
        AddStudentCommand = new RelayCommand(_ => AddStudent());
        UpdateStudentCommand = new RelayCommand(_ => UpdateStudent());
        RemoveStudentCommand = new RelayCommand(_ => RemoveStudent());

        AddEquipmentCommand = new RelayCommand(_ => AddEquipment());
        UpdateEquipmentCommand = new RelayCommand(_ => UpdateEquipment());
        RemoveEquipmentCommand = new RelayCommand(_ => RemoveEquipment());

        BorrowCommand = new RelayCommand(_ => Borrow());
        RefreshCommand = new RelayCommand(_ => Refresh());

        // Seed for demo
        Students.Add(new StudentItemViewModel { Id = 1, Name = "Alice", IsActive = true });
        Students.Add(new StudentItemViewModel { Id = 2, Name = "Bob", IsActive = true });
        Equipments.Add(new EquipmentItemViewModel { Id = 1, Name = "Camera", IsAvailable = true });
        Equipments.Add(new EquipmentItemViewModel { Id = 2, Name = "Tripod", IsAvailable = true });
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

    private void Borrow()
    {
        if (SelectedStudent == null || SelectedEquipment == null) { StatusMessage = "Select student and equipment to borrow."; return; }
        if (!SelectedEquipment.IsAvailable) { StatusMessage = "Equipment not available."; return; }
        SelectedEquipment.IsAvailable = false;
        StatusMessage = $"{SelectedStudent.Name} borrowed {SelectedEquipment.Name}.";
    }

    private void Refresh() => StatusMessage = "Refreshed.";

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
