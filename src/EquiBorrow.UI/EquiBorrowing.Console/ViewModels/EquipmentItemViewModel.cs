using Avalonia.Media;
using ReactiveUI;

namespace EquiBorrow.UI.ViewModels;

public class EquipmentItemViewModel : ReactiveObject
{
    public int Id { get; }
    public string Name { get; }

    private bool _isAvailable;
    public bool IsAvailable
    {
        get => _isAvailable;
        set
        {
            this.RaiseAndSetIfChanged(ref _isAvailable, value);
            this.RaisePropertyChanged(nameof(AvailabilityBrush));
            this.RaisePropertyChanged(nameof(AvailabilityText));
        }
    }

    public IBrush AvailabilityBrush => IsAvailable ? Brushes.Green : Brushes.Gray;
    public string AvailabilityText => IsAvailable ? "Available" : "Unavailable";

    public EquipmentItemViewModel(int id, string name, bool isAvailable)
    {
        Id = id;
        Name = name;
        _isAvailable = isAvailable;
    }
}
