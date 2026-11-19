using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using EquiBorrow.Application;
using EquiBorrow.Application.Interfaces;

namespace EquiBorrow.UI.ViewModels;

public abstract class BorrowingHistoryViewModelBase : INotifyPropertyChanged, IDisposable
{
    private CancellationTokenSource? _activeRequest;
    private long _requestNumber;
    private bool _isLoading;
    private bool _hasError;
    private bool _isStale;
    private string _message = string.Empty;
    private bool _disposed;

    protected BorrowingHistoryViewModelBase()
    {
        _isLoading = true;
        _message = "Loading history...";
        Borrowings.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(ShowRows));
            OnPropertyChanged(nameof(ShowEmptyState));
        };
        RetryCommand = new RelayCommand(_ => Retry(), _ => !IsLoading && CanRetry);
    }

    public ObservableCollection<BorrowingHistoryItemViewModel> Borrowings { get; } = new();
    public RelayCommand RetryCommand { get; }
    public bool IsLoading { get => _isLoading; private set { _isLoading = value; OnPropertyChanged(nameof(IsLoading)); OnPropertyChanged(nameof(ShowEmptyState)); OnPropertyChanged(nameof(ShowRows)); RetryCommand.RaiseCanExecuteChanged(); } }
    public bool HasError { get => _hasError; private set { _hasError = value; OnPropertyChanged(nameof(HasError)); OnPropertyChanged(nameof(ShowEmptyState)); OnPropertyChanged(nameof(ShowRows)); RetryCommand.RaiseCanExecuteChanged(); } }
    public bool IsStale { get => _isStale; private set { _isStale = value; OnPropertyChanged(nameof(IsStale)); OnPropertyChanged(nameof(ShowEmptyState)); OnPropertyChanged(nameof(ShowRows)); RetryCommand.RaiseCanExecuteChanged(); } }
    public string Message { get => _message; protected set { _message = value; OnPropertyChanged(nameof(Message)); } }
    public bool ShowRows => !IsLoading && !HasError && !IsStale && Borrowings.Count > 0;
    public bool ShowEmptyState => !IsLoading && !HasError && !IsStale && Borrowings.Count == 0;
    protected abstract bool CanRetry { get; }

    protected void StartHistoryLoad(
        Func<CancellationToken, Task<OperationResult<IReadOnlyList<BorrowingDetails>>>> load,
        Func<BorrowingDetails, BorrowingHistoryItemViewModel> project)
    {
        if (_disposed)
            return;

        var requestNumber = Interlocked.Increment(ref _requestNumber);
        var cancellation = new CancellationTokenSource();
        var previousRequest = Interlocked.Exchange(ref _activeRequest, cancellation);
        CancelSafely(previousRequest);
        Borrowings.Clear();
        HasError = false;
        IsStale = false;
        Message = "Loading history...";
        IsLoading = true;
        _ = ExecuteHistoryLoadAsync(requestNumber, cancellation, load, project);
    }

    protected void CancelHistoryLoad()
    {
        Interlocked.Increment(ref _requestNumber);
        CancelSafely(Interlocked.Exchange(ref _activeRequest, null));
        Borrowings.Clear();
        IsLoading = false;
        HasError = false;
        IsStale = false;
        Message = string.Empty;
    }

    protected void BeginLoading()
    {
        IsLoading = true;
        HasError = false;
        IsStale = false;
        Message = "Loading history...";
    }

    protected void SetLoadError(string message)
    {
        Borrowings.Clear();
        IsLoading = false;
        IsStale = false;
        HasError = true;
        Message = message;
    }

    protected void SetStale(string message)
    {
        Borrowings.Clear();
        IsLoading = false;
        HasError = false;
        IsStale = true;
        Message = message;
    }

    protected void SetEmpty(string message)
    {
        IsLoading = false;
        HasError = false;
        IsStale = false;
        Message = message;
    }

    protected void SetLoaded()
    {
        IsLoading = false;
        HasError = false;
        IsStale = false;
        Message = string.Empty;
    }

    protected abstract void Retry();
    protected abstract void OnSelectionChanged();

    protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    public event PropertyChangedEventHandler? PropertyChanged;

    private async Task ExecuteHistoryLoadAsync(
        long requestNumber,
        CancellationTokenSource cancellation,
        Func<CancellationToken, Task<OperationResult<IReadOnlyList<BorrowingDetails>>>> load,
        Func<BorrowingDetails, BorrowingHistoryItemViewModel> project)
    {
        try
        {
            var result = await load(cancellation.Token);
            if (requestNumber != Interlocked.Read(ref _requestNumber) || cancellation.IsCancellationRequested)
                return;

            if (result.Outcome == OperationOutcome.Stale)
            {
                SetStale(result.Message);
                return;
            }
            if (result.Outcome != OperationOutcome.Success || result.Value is null)
            {
                SetLoadError(result.Message);
                return;
            }

            foreach (var borrowing in result.Value)
                Borrowings.Add(project(borrowing));
            if (Borrowings.Count == 0)
                SetEmpty(EmptyStateMessage);
            else
                SetLoaded();
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            if (requestNumber == Interlocked.Read(ref _requestNumber))
                SetLoadError("Could not load the borrowing history. Check the database connection and retry.");
        }
        finally
        {
            if (requestNumber == Interlocked.Read(ref _requestNumber))
                IsLoading = false;
            Interlocked.CompareExchange(ref _activeRequest, null, cancellation);
            cancellation.Dispose();
        }
    }

    protected abstract string EmptyStateMessage { get; }

    protected static void CancelSafely(CancellationTokenSource? cancellation)
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

    public virtual void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        Interlocked.Increment(ref _requestNumber);
        CancelSafely(Interlocked.Exchange(ref _activeRequest, null));
    }
}
