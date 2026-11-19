using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using EquiBorrow.Application;
using EquiBorrow.Application.Interfaces;
using EquiBorrow.Application.Services;
using EquiBorrow.Domain;

namespace EquiBorrow.UI.ViewModels;

public sealed class BorrowingSearchViewModel : INotifyPropertyChanged, IDisposable
{
    private static readonly BorrowingStatusOption[] StatusOptionValues =
    [
        new("All", null),
        new("Active", BorrowingStatus.Active),
        new("Returned", BorrowingStatus.Returned)
    ];
    private static readonly BorrowingSortOption[] SortOptionValues =
    [
        new("Checkout date", BorrowingSortField.BorrowDate),
        new("Expected return date", BorrowingSortField.ExpectedReturnDate)
    ];
    private static readonly BorrowingDirectionOption[] DirectionOptionValues =
    [
        new("Ascending", SortDirection.Ascending),
        new("Descending", SortDirection.Descending)
    ];

    private readonly SearchBorrowingsService _searchService;
    private CancellationTokenSource? _activeRequest;
    private CancellationTokenSource? _debounceRequest;
    private long _requestNumber;
    private string _searchText = string.Empty;
    private BorrowingStatusOption _selectedStatus = StatusOptionValues[0];
    private BorrowingSortOption _selectedSort = SortOptionValues[0];
    private BorrowingDirectionOption _selectedDirection = DirectionOptionValues[1];
    private bool _isLoading = true;
    private bool _hasError;
    private string _message = "Loading borrowings...";
    private string _resultCountText = "0 results";
    private bool _disposed;

    public BorrowingSearchViewModel(SearchBorrowingsService searchService)
    {
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        StatusOptions = new ObservableCollection<BorrowingStatusOption>(StatusOptionValues);
        SortOptions = new ObservableCollection<BorrowingSortOption>(SortOptionValues);
        DirectionOptions = new ObservableCollection<BorrowingDirectionOption>(DirectionOptionValues);
        Results.CollectionChanged += (_, _) => OnPropertyChanged(nameof(ShowResults));
        SearchNowCommand = new RelayCommand(_ => LoadCurrentQuery());
        ClearCommand = new RelayCommand(_ => ClearCriteria());
        RetryCommand = new RelayCommand(_ => LoadCurrentQuery(), _ => HasError && !IsLoading);
        LoadCurrentQuery();
    }

    public ObservableCollection<BorrowingStatusOption> StatusOptions { get; }
    public ObservableCollection<BorrowingSortOption> SortOptions { get; }
    public ObservableCollection<BorrowingDirectionOption> DirectionOptions { get; }
    public ObservableCollection<BorrowingSearchItemViewModel> Results { get; } = new();
    public RelayCommand SearchNowCommand { get; }
    public RelayCommand ClearCommand { get; }
    public RelayCommand RetryCommand { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText == value)
                return;
            _searchText = value;
            OnPropertyChanged(nameof(SearchText));
            ScheduleTextSearch();
        }
    }

    public BorrowingStatusOption SelectedStatus
    {
        get => _selectedStatus;
        set
        {
            if (_selectedStatus == value || value is null)
                return;
            _selectedStatus = value;
            OnPropertyChanged(nameof(SelectedStatus));
            ReloadImmediately();
        }
    }

    public BorrowingSortOption SelectedSort
    {
        get => _selectedSort;
        set
        {
            if (_selectedSort == value || value is null)
                return;
            _selectedSort = value;
            OnPropertyChanged(nameof(SelectedSort));
            ReloadImmediately();
        }
    }

    public BorrowingDirectionOption SelectedDirection
    {
        get => _selectedDirection;
        set
        {
            if (_selectedDirection == value || value is null)
                return;
            _selectedDirection = value;
            OnPropertyChanged(nameof(SelectedDirection));
            ReloadImmediately();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (_isLoading == value)
                return;
            _isLoading = value;
            OnPropertyChanged(nameof(IsLoading));
            OnPropertyChanged(nameof(ShowEmptyState));
            OnPropertyChanged(nameof(ShowResults));
            RetryCommand.RaiseCanExecuteChanged();
        }
    }

    public bool HasError
    {
        get => _hasError;
        private set
        {
            if (_hasError == value)
                return;
            _hasError = value;
            OnPropertyChanged(nameof(HasError));
            OnPropertyChanged(nameof(ShowEmptyState));
            OnPropertyChanged(nameof(ShowResults));
            RetryCommand.RaiseCanExecuteChanged();
        }
    }

    public string Message
    {
        get => _message;
        private set { _message = value; OnPropertyChanged(nameof(Message)); }
    }

    public string ResultCountText
    {
        get => _resultCountText;
        private set { _resultCountText = value; OnPropertyChanged(nameof(ResultCountText)); }
    }

    public bool ShowResults => !IsLoading && !HasError && Results.Count > 0;
    public bool ShowEmptyState => !IsLoading && !HasError && Results.Count == 0;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void SearchNow()
    {
        CancelDebounce();
        BeginCurrentLoad();
    }

    public void ClearCriteria()
    {
        CancelDebounce();
        _searchText = string.Empty;
        _selectedStatus = StatusOptionValues[0];
        _selectedSort = SortOptionValues[0];
        _selectedDirection = DirectionOptionValues[1];
        OnPropertyChanged(nameof(SearchText));
        OnPropertyChanged(nameof(SelectedStatus));
        OnPropertyChanged(nameof(SelectedSort));
        OnPropertyChanged(nameof(SelectedDirection));
        BeginCurrentLoad();
    }

    private void ScheduleTextSearch()
    {
        CancelDebounce();
        CancelActiveRequest();
        var debounce = new CancellationTokenSource();
        _debounceRequest = debounce;
        ClearResultsForPendingQuery();
        _ = DebounceAndLoadAsync(debounce);
    }

    private async Task DebounceAndLoadAsync(CancellationTokenSource debounce)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(300), debounce.Token);
            if (_disposed || !ReferenceEquals(_debounceRequest, debounce))
                return;
            _debounceRequest = null;
            debounce.Dispose();
            BeginCurrentLoad();
        }
        catch (OperationCanceledException) when (debounce.IsCancellationRequested)
        {
            debounce.Dispose();
        }
    }

    private void ReloadImmediately()
    {
        CancelDebounce();
        BeginCurrentLoad();
    }

    private void LoadCurrentQuery() => BeginCurrentLoad();

    private void BeginCurrentLoad()
    {
        if (_disposed)
            return;
        CancelDebounce();
        var requestNumber = Interlocked.Increment(ref _requestNumber);
        var request = new CancellationTokenSource();
        var previousRequest = Interlocked.Exchange(ref _activeRequest, request);
        CancelSafely(previousRequest);
        Results.Clear();
        HasError = false;
        Message = "Loading borrowings...";
        IsLoading = true;
        _ = ExecuteLoadAsync(requestNumber, request, CreateQuery());
    }

    private async Task ExecuteLoadAsync(long requestNumber, CancellationTokenSource request, BorrowingQuery query)
    {
        try
        {
            var result = await _searchService.ExecuteAsync(query, request.Token);
            if (requestNumber != Interlocked.Read(ref _requestNumber) || request.IsCancellationRequested)
                return;

            if (result.Outcome != OperationOutcome.Success || result.Value is null)
            {
                Results.Clear();
                HasError = true;
                Message = result.Message;
                ResultCountText = "0 results";
                return;
            }

            foreach (var borrowing in result.Value)
                Results.Add(new BorrowingSearchItemViewModel(borrowing));
            ResultCountText = $"{Results.Count} result{(Results.Count == 1 ? string.Empty : "s")}";
            Message = Results.Count == 0
                ? HasCriteria ? "No borrowings match the current criteria." : "No borrowings recorded yet."
                : string.Empty;
            HasError = false;
        }
        catch (OperationCanceledException) when (request.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            if (requestNumber == Interlocked.Read(ref _requestNumber))
            {
                Results.Clear();
                HasError = true;
                Message = "Could not load the borrowings. Check the database connection and retry.";
                ResultCountText = "0 results";
            }
        }
        finally
        {
            if (requestNumber == Interlocked.Read(ref _requestNumber))
                IsLoading = false;
            Interlocked.CompareExchange(ref _activeRequest, null, request);
            request.Dispose();
        }
    }

    private BorrowingQuery CreateQuery() => new()
    {
        SearchText = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(),
        Status = SelectedStatus.Value,
        SortField = SelectedSort.Value,
        SortDirection = SelectedDirection.Value
    };

    private bool HasCriteria => !string.IsNullOrWhiteSpace(SearchText) || SelectedStatus.Value.HasValue;

    private void ClearResultsForPendingQuery()
    {
        Results.Clear();
        HasError = false;
        Message = "Loading borrowings...";
        IsLoading = true;
        ResultCountText = "0 results";
    }

    private void CancelDebounce()
    {
        var previous = Interlocked.Exchange(ref _debounceRequest, null);
        CancelSafely(previous);
    }

    private void CancelActiveRequest()
    {
        Interlocked.Increment(ref _requestNumber);
        CancelSafely(Interlocked.Exchange(ref _activeRequest, null));
    }

    private static void CancelSafely(CancellationTokenSource? cancellation)
    {
        if (cancellation is null)
            return;
        try
        {
            cancellation.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        Interlocked.Increment(ref _requestNumber);
        CancelDebounce();
        CancelActiveRequest();
    }
}
