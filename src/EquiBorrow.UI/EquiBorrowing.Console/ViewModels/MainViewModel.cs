using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using EquiBorrow.Application.Services;
using EquiBorrow.Infrastructure.Repositories;

namespace EquiBorrow.UI.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    public ObservableCollection<StudentItemViewModel> Students { get; } = new();
    public ObservableCollection<EquipmentItemViewModel> Equipments { get; } = new();

    private StudentItemViewModel? _selectedStudent;
    public StudentItemViewModel? SelectedStudent
    {
        get => _selectedStudent;
        set
        {
            _selectedStudent = value;
            OnPropertyChanged(nameof(SelectedStudent));
            if (_selectedStudent != null)
            {
                NewStudentName = _selectedStudent.Name;
                NewStudentIsActive = _selectedStudent.IsActive;
            }
            UpdateCommands();
            OnPropertyChanged(nameof(CanBorrowEnabled));
            OnPropertyChanged(nameof(BorrowButtonTooltip));
        }
    }

    private EquipmentItemViewModel? _selectedEquipment;
    public EquipmentItemViewModel? SelectedEquipment
    {
        get => _selectedEquipment;
        set
        {
            if (_selectedEquipment is System.ComponentModel.INotifyPropertyChanged oldNotify)
                oldNotify.PropertyChanged -= SelectedEquipment_PropertyChanged;

            _selectedEquipment = value;
            OnPropertyChanged(nameof(SelectedEquipment));
            if (_selectedEquipment != null)
            {
                NewEquipmentName = _selectedEquipment.Name;
                NewEquipmentIsAvailable = _selectedEquipment.IsAvailable;
                if (_selectedEquipment is System.ComponentModel.INotifyPropertyChanged newNotify)
                    newNotify.PropertyChanged += SelectedEquipment_PropertyChanged;
            }
            UpdateCommands();
            OnPropertyChanged(nameof(CanBorrowEnabled));
            OnPropertyChanged(nameof(BorrowButtonTooltip));
        }
    }

    private void SelectedEquipment_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EquipmentItemViewModel.IsAvailable))
        {
            OnPropertyChanged(nameof(CanBorrowEnabled));
            OnPropertyChanged(nameof(BorrowButtonTooltip));
            UpdateCommands();
        }
    }

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(nameof(StatusMessage)); }
    }

    public bool CanBorrowEnabled => SelectedStudent != null && SelectedStudent.IsActive && SelectedEquipment != null && SelectedEquipment.IsAvailable;

    public string BorrowButtonTooltip
    {
        get
        {
            if (SelectedStudent == null) return "Select a student first.";
            if (!SelectedStudent.IsActive) return "Selected student is inactive and cannot borrow.";
            if (SelectedEquipment == null) return "Select equipment to borrow.";
            if (!SelectedEquipment.IsAvailable) return "Selected equipment is currently unavailable.";
            return "Click to borrow the selected equipment.";
        }
    }

    public ICommand BorrowCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand AddStudentCommand { get; }
    public ICommand UpdateStudentCommand { get; }
    public ICommand RemoveStudentCommand { get; }

    public ICommand AddEquipmentCommand { get; }
    public ICommand UpdateEquipmentCommand { get; }
    public ICommand RemoveEquipmentCommand { get; }

    // in-memory repos
    private readonly InMemoryStudentRepository _studentRepo = new();
    private readonly InMemoryEquipmentRepository _equipmentRepo = new();
    private readonly InMemoryBorrowingRepository _borrowingRepo = new();
    private readonly BorrowEquipmentService _borrowService;

    public MainViewModel()
    {
        _borrowService = new BorrowEquipmentService(_studentRepo, _equipmentRepo, _borrowingRepo);

        BorrowCommand = new AsyncRelayCommand(async _ => await OnBorrowAsync(), _ => CanBorrow());
        RefreshCommand = new AsyncRelayCommand(_ => { LoadData(); return System.Threading.Tasks.Task.CompletedTask; });
        AddStudentCommand = new AsyncRelayCommand(_ => { AddStudent(); return System.Threading.Tasks.Task.CompletedTask; });
        UpdateStudentCommand = new AsyncRelayCommand(_ => { UpdateStudent(); return System.Threading.Tasks.Task.CompletedTask; }, _ => SelectedStudent != null);
        RemoveStudentCommand = new AsyncRelayCommand(_ => { RemoveStudent(); return System.Threading.Tasks.Task.CompletedTask; }, _ => SelectedStudent != null);

        AddEquipmentCommand = new AsyncRelayCommand(_ => { AddEquipment(); return System.Threading.Tasks.Task.CompletedTask; });
        UpdateEquipmentCommand = new AsyncRelayCommand(_ => { UpdateEquipment(); return System.Threading.Tasks.Task.CompletedTask; }, _ => SelectedEquipment != null);
        RemoveEquipmentCommand = new AsyncRelayCommand(_ => { RemoveEquipment(); return System.Threading.Tasks.Task.CompletedTask; }, _ => SelectedEquipment != null);

        LoadData();
    }

    private void LoadData()
    {
        Students.Clear();
        for (int id = 1; id <= 10; id++)
        {
            var s = _studentRepo.GetByIdAsync(id).ConfigureAwait(false).GetAwaiter().GetResult();
            if (s != null)
                Students.Add(new StudentItemViewModel(s.Id, s.Name, s.IsActive));
        }

        Equipments.Clear();
        for (int id = 101; id <= 110; id++)
        {
            var e = _equipmentRepo.GetByIdAsync(id).ConfigureAwait(false).GetAwaiter().GetResult();
            if (e != null)
                Equipments.Add(new EquipmentItemViewModel(e.Id, e.Name, e.IsAvailable));
        }

        StatusMessage = "Ready";
        UpdateCommands();
    }

    // CRUD
    private string _newStudentName = string.Empty;
    public string NewStudentName { get => _newStudentName; set { _newStudentName = value; OnPropertyChanged(nameof(NewStudentName)); } }

    private bool _newStudentIsActive = true;
    public bool NewStudentIsActive { get => _newStudentIsActive; set { _newStudentIsActive = value; OnPropertyChanged(nameof(NewStudentIsActive)); } }

    private string _newEquipmentName = string.Empty;
    public string NewEquipmentName { get => _newEquipmentName; set { _newEquipmentName = value; OnPropertyChanged(nameof(NewEquipmentName)); } }

    private bool _newEquipmentIsAvailable = true;
    public bool NewEquipmentIsAvailable { get => _newEquipmentIsAvailable; set { _newEquipmentIsAvailable = value; OnPropertyChanged(nameof(NewEquipmentIsAvailable)); } }

    private void AddStudent()
    {
        if (string.IsNullOrWhiteSpace(NewStudentName)) { StatusMessage = "Student name required."; return; }
        var s = new EquiBorrow.Domain.Student(0, NewStudentName, NewStudentIsActive);
        _studentRepo.AddAsync(s).ConfigureAwait(false).GetAwaiter().GetResult();
        LoadData();
        NewStudentName = string.Empty;
        StatusMessage = "Student added.";
    }

    private void UpdateStudent()
    {
        if (SelectedStudent == null) return;
        var s = new EquiBorrow.Domain.Student(SelectedStudent.Id, NewStudentName, NewStudentIsActive);
        _studentRepo.UpdateAsync(s).ConfigureAwait(false).GetAwaiter().GetResult();
        LoadData();
        StatusMessage = "Student updated.";
    }

    private void RemoveStudent()
    {
        if (SelectedStudent == null) return;
        _studentRepo.RemoveAsync(SelectedStudent.Id).ConfigureAwait(false).GetAwaiter().GetResult();
        LoadData();
        StatusMessage = "Student removed.";
    }

    private void AddEquipment()
    {
        if (string.IsNullOrWhiteSpace(NewEquipmentName)) { StatusMessage = "Equipment name required."; return; }
        var e = new EquiBorrow.Domain.Equipment(0, NewEquipmentName, NewEquipmentIsAvailable);
        _equipmentRepo.AddAsync(e).ConfigureAwait(false).GetAwaiter().GetResult();
        LoadData();
        NewEquipmentName = string.Empty;
        StatusMessage = "Equipment added.";
    }

    private void UpdateEquipment()
    {
        if (SelectedEquipment == null) return;
        var e = new EquiBorrow.Domain.Equipment(SelectedEquipment.Id, NewEquipmentName, NewEquipmentIsAvailable);
        _equipmentRepo.UpdateAsync(e).ConfigureAwait(false).GetAwaiter().GetResult();
        LoadData();
        StatusMessage = "Equipment updated.";
    }

    private void RemoveEquipment()
    {
        if (SelectedEquipment == null) return;
        _equipmentRepo.RemoveAsync(SelectedEquipment.Id).ConfigureAwait(false).GetAwaiter().GetResult();
        LoadData();
        StatusMessage = "Equipment removed.";
    }

    private bool CanBorrow()
    {
        return SelectedStudent != null && SelectedStudent.IsActive && SelectedEquipment != null && SelectedEquipment.IsAvailable;
    }

    private async Task OnBorrowAsync()
    {
        if (!CanBorrow())
        {
            StatusMessage = "Select an active student and an available equipment to borrow.";
            return;
        }

        try
        {
            StatusMessage = "Processing...";
            var result = await _borrowService.ExecuteAsync(SelectedStudent!.Id, SelectedEquipment!.Id);
            // mark equipment
            SelectedEquipment.IsAvailable = false;
            StatusMessage = $"Success: Borrowing created (Id {result.Id})";
            UpdateCommands();
        }
        catch (Exception ex)
        {
            StatusMessage = "Error: " + ex.Message;
            UpdateCommands();
        }
    }

    private void UpdateCommands()
    {
        if (BorrowCommand is AsyncRelayCommand arc) arc.RaiseCanExecuteChanged();
        if (BorrowCommand is RelayCommand rc) rc.RaiseCanExecuteChanged();
        if (UpdateStudentCommand is AsyncRelayCommand usc) usc.RaiseCanExecuteChanged();
        if (UpdateStudentCommand is RelayCommand usc2) usc2.RaiseCanExecuteChanged();
        if (RemoveStudentCommand is AsyncRelayCommand rsc) rsc.RaiseCanExecuteChanged();
        if (RemoveStudentCommand is RelayCommand rsc2) rsc2.RaiseCanExecuteChanged();
        if (UpdateEquipmentCommand is AsyncRelayCommand uec) uec.RaiseCanExecuteChanged();
        if (UpdateEquipmentCommand is RelayCommand uec2) uec2.RaiseCanExecuteChanged();
        if (RemoveEquipmentCommand is AsyncRelayCommand rec) rec.RaiseCanExecuteChanged();
        if (RemoveEquipmentCommand is RelayCommand rec2) rec2.RaiseCanExecuteChanged();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
