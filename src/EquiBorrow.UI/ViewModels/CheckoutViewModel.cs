using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EquiBorrow.Application;
using EquiBorrow.Application.Interfaces;
using EquiBorrow.Application.Services;
using EquiBorrow.Domain;

namespace EquiBorrow.UI.ViewModels;

public sealed class CheckoutViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly ListStudentsService _listStudentsService;
    private readonly ViewEquipmentCatalogService _catalogService;
    private readonly CheckOutMultipleEquipmentService _checkoutService;
    private readonly TimeProvider _timeProvider;
    private readonly CancellationTokenSource _lifetime = new();
    private CheckoutStudentItemViewModel? _selectedStudent;
    private DateTime? _expectedReturnDate;
    private bool _isBusy;
    private bool _isLoadingData;
    private bool _isCheckingOut;
    private bool _hasLoadError;
    private string _loadErrorMessage = string.Empty;
    private string _statusMessage = string.Empty;
    private bool _disposed;

    public CheckoutViewModel(
        int? selectedStudentId,
        ListStudentsService listStudentsService,
        ViewEquipmentCatalogService catalogService,
        CheckOutMultipleEquipmentService checkoutService,
        TimeProvider timeProvider)
    {
        _listStudentsService = listStudentsService ?? throw new ArgumentNullException(nameof(listStudentsService));
        _catalogService = catalogService ?? throw new ArgumentNullException(nameof(catalogService));
        _checkoutService = checkoutService ?? throw new ArgumentNullException(nameof(checkoutService));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _initialStudentId = selectedStudentId == 0 ? null : selectedStudentId;
        ExpectedReturnDate = _timeProvider.GetLocalNow().Date.AddDays(7);
        SubmitCommand = new RelayCommand(_ => RunCommandAsync(SubmitAsync), _ => !IsBusy && !HasLoadError);
        RetryCommand = new RelayCommand(_ => RunCommandAsync(LoadDataAsync), _ => !IsBusy && HasLoadError);
        Equipments.CollectionChanged += (_, _) => OnPropertyChanged(nameof(SelectedItemsText));
        RunCommandAsync(LoadDataAsync);
    }

    private readonly int? _initialStudentId;
    public ObservableCollection<CheckoutStudentItemViewModel> Students { get; } = new();
    public ObservableCollection<CheckoutEquipmentItemViewModel> Equipments { get; } = new();
    public RelayCommand SubmitCommand { get; }
    public RelayCommand RetryCommand { get; }

    public CheckoutStudentItemViewModel? SelectedStudent
    {
        get => _selectedStudent;
        set
        {
            if (_selectedStudent?.Id == value?.Id)
                return;
            _selectedStudent = value;
            OnPropertyChanged(nameof(SelectedStudent));
        }
    }

    public DateTime? ExpectedReturnDate
    {
        get => _expectedReturnDate;
        set
        {
            if (_expectedReturnDate == value)
                return;
            _expectedReturnDate = value;
            OnPropertyChanged(nameof(ExpectedReturnDate));
        }
    }

    public DateTime EarliestExpectedReturnDate => _timeProvider.GetLocalNow().Date.AddDays(1);
    public bool IsStudentSelectorEnabled => !IsBusy && !HasLoadError;
    public bool IsEquipmentListEnabled => !IsBusy && !HasLoadError;
    public bool IsExpectedReturnDateEnabled => !IsBusy && !HasLoadError;
    public int SelectedItemCount => Equipments.Count(item => item.IsSelected);
    public string SelectedItemsText => $"{SelectedItemCount} items selected";
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy == value)
                return;
            _isBusy = value;
            OnPropertyChanged(nameof(IsBusy));
            OnPropertyChanged(nameof(IsStudentSelectorEnabled));
            OnPropertyChanged(nameof(IsEquipmentListEnabled));
            OnPropertyChanged(nameof(IsExpectedReturnDateEnabled));
            SubmitCommand.RaiseCanExecuteChanged();
            RetryCommand.RaiseCanExecuteChanged();
        }
    }

    public bool IsLoadingData
    {
        get => _isLoadingData;
        private set { if (_isLoadingData == value) return; _isLoadingData = value; OnPropertyChanged(nameof(IsLoadingData)); }
    }

    public bool IsCheckingOut
    {
        get => _isCheckingOut;
        private set { if (_isCheckingOut == value) return; _isCheckingOut = value; OnPropertyChanged(nameof(IsCheckingOut)); }
    }

    public bool HasLoadError
    {
        get => _hasLoadError;
        private set
        {
            if (_hasLoadError == value)
                return;
            _hasLoadError = value;
            OnPropertyChanged(nameof(HasLoadError));
            OnPropertyChanged(nameof(IsStudentSelectorEnabled));
            OnPropertyChanged(nameof(IsEquipmentListEnabled));
            OnPropertyChanged(nameof(IsExpectedReturnDateEnabled));
            SubmitCommand.RaiseCanExecuteChanged();
            RetryCommand.RaiseCanExecuteChanged();
        }
    }

    public string LoadErrorMessage
    {
        get => _loadErrorMessage;
        private set { _loadErrorMessage = value; OnPropertyChanged(nameof(LoadErrorMessage)); }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set { _statusMessage = value; OnPropertyChanged(nameof(StatusMessage)); }
    }

    public event EventHandler? RefreshMainWindowRequested;
    public event PropertyChangedEventHandler? PropertyChanged;

    private async Task LoadDataAsync()
    {
        IsLoadingData = true;
        HasLoadError = false;
        LoadErrorMessage = string.Empty;
        StatusMessage = string.Empty;

        try
        {
            var studentsTask = _listStudentsService.ExecuteAsync(_lifetime.Token);
            var equipmentTask = _catalogService.ExecuteAsync(_lifetime.Token);
            await Task.WhenAll(studentsTask, equipmentTask);
            var studentsResult = await studentsTask;
            var equipmentResult = await equipmentTask;

            if (studentsResult.Outcome != OperationOutcome.Success || studentsResult.Value is null)
            {
                SetLoadError(studentsResult.Message);
                return;
            }
            if (equipmentResult.Outcome != OperationOutcome.Success || equipmentResult.Value is null)
            {
                SetLoadError(equipmentResult.Message);
                return;
            }

            var selectedStudentId = SelectedStudent?.Id ?? _initialStudentId;
            var selectedEquipmentIds = Equipments.Where(item => item.IsSelected).Select(item => item.Id).ToHashSet();

            Students.Clear();
            foreach (var student in studentsResult.Value)
                Students.Add(new CheckoutStudentItemViewModel(student));
            SelectedStudent = Students.FirstOrDefault(student => student.Id == selectedStudentId);

            Equipments.Clear();
            foreach (var equipment in equipmentResult.Value)
            {
                var item = new CheckoutEquipmentItemViewModel(equipment, selectedEquipmentIds.Contains(equipment.Id));
                item.PropertyChanged += EquipmentItem_PropertyChanged;
                Equipments.Add(item);
            }
            OnPropertyChanged(nameof(SelectedItemsText));
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            SetLoadError("Could not load students or equipment. Check the database connection and retry.");
        }
        finally
        {
            IsLoadingData = false;
        }
    }

    private async Task SubmitAsync()
    {
        if (SelectedStudent is null)
        {
            StatusMessage = "Select a student.";
            return;
        }
        if (ExpectedReturnDate is null)
        {
            StatusMessage = "Choose an expected return date.";
            return;
        }

        StatusMessage = "Checking out...";
        IsCheckingOut = true;
        OperationResult<System.Collections.Generic.IReadOnlyList<BorrowingDetails>> result;
        try
        {
            var selectedIds = Equipments.Where(item => item.IsSelected).Select(item => item.Id).ToArray();
            result = await _checkoutService.ExecuteAsync(
                SelectedStudent.Id,
                selectedIds,
                DateOnly.FromDateTime(ExpectedReturnDate.Value),
                _lifetime.Token);
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
            return;
        }
        catch (Exception)
        {
            StatusMessage = "The result could not be verified. Refresh and check the borrowings before retrying.";
            IsCheckingOut = false;
            await LoadEquipmentAsyncPreservingSelections();
            RefreshMainWindowRequested?.Invoke(this, EventArgs.Empty);
            return;
        }
        finally
        {
            IsCheckingOut = false;
        }

        StatusMessage = result.Message;
        if (result.Outcome != OperationOutcome.Rejected)
        {
            if (result.Outcome == OperationOutcome.Success)
            {
                foreach (var item in Equipments)
                    item.IsSelected = false;
                OnPropertyChanged(nameof(SelectedItemsText));
            }

            if (result.Outcome is OperationOutcome.Stale or OperationOutcome.UnknownOutcome or OperationOutcome.Success)
                await LoadEquipmentAsyncPreservingSelections();

            RefreshMainWindowRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private async Task LoadEquipmentAsyncPreservingSelections()
    {
        IsLoadingData = true;
        try
        {
            var selectedIds = Equipments.Where(item => item.IsSelected).Select(item => item.Id).ToHashSet();
            var result = await _catalogService.ExecuteAsync(_lifetime.Token);
            if (result.Outcome != OperationOutcome.Success || result.Value is null)
            {
                SetLoadError(result.Message);
                return;
            }

            HasLoadError = false;
            LoadErrorMessage = string.Empty;
            Equipments.Clear();
            foreach (var equipment in result.Value)
            {
                var item = new CheckoutEquipmentItemViewModel(equipment, selectedIds.Contains(equipment.Id));
                item.PropertyChanged += EquipmentItem_PropertyChanged;
                Equipments.Add(item);
            }
            OnPropertyChanged(nameof(SelectedItemsText));
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            SetLoadError("Could not load equipment. Check the database connection and retry.");
        }
        finally
        {
            IsLoadingData = false;
        }
    }

    private void EquipmentItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CheckoutEquipmentItemViewModel.IsSelected))
            OnPropertyChanged(nameof(SelectedItemsText));
    }

    private void SetLoadError(string message)
    {
        HasLoadError = true;
        LoadErrorMessage = message;
    }

    private void RunCommandAsync(Func<Task> operation)
    {
        if (IsBusy || _disposed)
            return;
        IsBusy = true;
        _ = ExecuteCommandAsync(operation);
    }

    private async Task ExecuteCommandAsync(Func<Task> operation)
    {
        try
        {
            await operation();
        }
        catch (Exception)
        {
            StatusMessage = "The checkout could not be completed. Check the database connection and retry.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _lifetime.Cancel();
        _lifetime.Dispose();
    }
}
