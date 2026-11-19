using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using EquiBorrow.Application;
using EquiBorrow.Application.Services;
using EquiBorrow.Domain;

namespace EquiBorrow.UI.ViewModels;

public sealed class EquipmentHistoryViewModel : BorrowingHistoryViewModelBase
{
    private readonly ViewEquipmentCatalogService _catalogService;
    private readonly ViewEquipmentBorrowingHistoryService _historyService;
    private readonly int _initialEquipmentId;
    private Equipment? _selectedEquipment;
    private bool _optionsFailed;
    private CancellationTokenSource? _optionsRequest;
    private long _optionsRequestNumber;

    public EquipmentHistoryViewModel(
        int equipmentId,
        ViewEquipmentCatalogService catalogService,
        ViewEquipmentBorrowingHistoryService historyService)
    {
        _initialEquipmentId = equipmentId;
        _catalogService = catalogService;
        _historyService = historyService;
        _ = LoadEquipmentAsync();
    }

    public ObservableCollection<Equipment> EquipmentItems { get; } = new();
    public Equipment? SelectedEquipment
    {
        get => _selectedEquipment;
        set
        {
            if (_selectedEquipment?.Id == value?.Id) return;
            _selectedEquipment = value;
            OnPropertyChanged(nameof(SelectedEquipment));
            OnPropertyChanged(nameof(SelectedIdentity));
            OnSelectionChanged();
        }
    }
    public string SelectedIdentity => SelectedEquipment is null
        ? string.Empty
        : $"Borrowing history for {SelectedEquipment.Identifier} — {SelectedEquipment.Name}";

    protected override bool CanRetry => SelectedEquipment is not null || _optionsFailed;
    protected override string EmptyStateMessage => "No borrowing history for this item.";

    protected override void OnSelectionChanged()
    {
        if (SelectedEquipment is null)
        {
            CancelHistoryLoad();
            return;
        }

        var selectedId = SelectedEquipment.Id;
        StartHistoryLoad(
            cancellationToken => _historyService.ExecuteAsync(selectedId, cancellationToken),
            BorrowingHistoryItemViewModel.ForEquipmentHistory);
    }

    protected override void Retry()
    {
        if (_optionsFailed)
        {
            _ = LoadEquipmentAsync();
            return;
        }
        OnSelectionChanged();
    }

    private async Task LoadEquipmentAsync()
    {
        BeginLoading();
        var cancellation = new CancellationTokenSource();
        var requestNumber = Interlocked.Increment(ref _optionsRequestNumber);
        CancelSafely(Interlocked.Exchange(ref _optionsRequest, cancellation));
        try
        {
            var result = await _catalogService.ExecuteAsync(cancellation.Token);
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
            EquipmentItems.Clear();
            foreach (var equipment in result.Value)
                EquipmentItems.Add(equipment);
            SelectedEquipment = FindEquipment(_initialEquipmentId);
            if (SelectedEquipment is null)
                SetStale("That equipment item no longer exists. Select another item.");
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
                SetLoadError("Could not load equipment. Check the database connection and retry.");
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

    private Equipment? FindEquipment(int equipmentId)
    {
        foreach (var equipment in EquipmentItems)
        {
            if (equipment.Id == equipmentId)
                return equipment;
        }
        return null;
    }
}
