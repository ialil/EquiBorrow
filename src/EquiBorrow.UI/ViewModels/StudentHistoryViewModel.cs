using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using EquiBorrow.Application;
using EquiBorrow.Application.Services;
using EquiBorrow.Domain;

namespace EquiBorrow.UI.ViewModels;

public sealed class StudentHistoryViewModel : BorrowingHistoryViewModelBase
{
    private readonly ListStudentsService _listStudentsService;
    private readonly ViewStudentBorrowingHistoryService _historyService;
    private readonly int _initialStudentId;
    private Student? _selectedStudent;
    private bool _optionsFailed;
    private CancellationTokenSource? _optionsRequest;
    private long _optionsRequestNumber;

    public StudentHistoryViewModel(
        int studentId,
        ListStudentsService listStudentsService,
        ViewStudentBorrowingHistoryService historyService)
    {
        _initialStudentId = studentId;
        _listStudentsService = listStudentsService;
        _historyService = historyService;
        _ = LoadStudentsAsync();
    }

    public ObservableCollection<Student> Students { get; } = new();
    public Student? SelectedStudent
    {
        get => _selectedStudent;
        set
        {
            if (_selectedStudent?.Id == value?.Id) return;
            _selectedStudent = value;
            OnPropertyChanged(nameof(SelectedStudent));
            OnPropertyChanged(nameof(SelectedIdentity));
            OnSelectionChanged();
        }
    }
    public string SelectedIdentity => SelectedStudent is null
        ? string.Empty
        : $"Borrowing history for {SelectedStudent.StudentNumber} — {SelectedStudent.Name}";

    protected override bool CanRetry => SelectedStudent is not null || _optionsFailed;
    protected override string EmptyStateMessage => "No borrowing history for this student.";

    protected override void OnSelectionChanged()
    {
        if (SelectedStudent is null)
        {
            CancelHistoryLoad();
            return;
        }

        var selectedId = SelectedStudent.Id;
        StartHistoryLoad(
            cancellationToken => _historyService.ExecuteAsync(selectedId, cancellationToken),
            BorrowingHistoryItemViewModel.ForStudentHistory);
    }

    protected override void Retry()
    {
        if (_optionsFailed)
        {
            _ = LoadStudentsAsync();
            return;
        }
        OnSelectionChanged();
    }

    private async Task LoadStudentsAsync()
    {
        BeginLoading();
        var cancellation = new CancellationTokenSource();
        var requestNumber = Interlocked.Increment(ref _optionsRequestNumber);
        CancelSafely(Interlocked.Exchange(ref _optionsRequest, cancellation));
        try
        {
            var result = await _listStudentsService.ExecuteAsync(cancellation.Token);
            if (requestNumber != Interlocked.Read(ref _optionsRequestNumber) || cancellation.IsCancellationRequested)
                return;
            if (result.Outcome != OperationOutcome.Success || result.Value is null)
            {
                _optionsFailed = true;
                SetLoadError(result.Message);
                RetryCommand.RaiseCanExecuteChanged();
                return;
            }

            _optionsFailed = false;
            Students.Clear();
            foreach (var student in result.Value)
                Students.Add(student);
            SelectedStudent = FindStudent(_initialStudentId);
            if (SelectedStudent is null)
                SetStale("That student no longer exists. Select another student.");
            RetryCommand.RaiseCanExecuteChanged();
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            if (requestNumber == Interlocked.Read(ref _optionsRequestNumber))
            {
                _optionsFailed = true;
                SetLoadError("Could not load students. Check the database connection and retry.");
                RetryCommand.RaiseCanExecuteChanged();
            }
        }
        finally
        {
            Interlocked.CompareExchange(ref _optionsRequest, null, cancellation);
            cancellation.Dispose();
        }
    }

    public override void Dispose()
    {
        Interlocked.Increment(ref _optionsRequestNumber);
        CancelSafely(Interlocked.Exchange(ref _optionsRequest, null));
        base.Dispose();
    }

    private Student? FindStudent(int studentId)
    {
        foreach (var student in Students)
        {
            if (student.Id == studentId)
                return student;
        }
        return null;
    }
}
