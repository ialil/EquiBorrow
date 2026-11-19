using System.ComponentModel;
using EquiBorrow.Domain;

namespace EquiBorrow.UI.ViewModels;

public sealed class CheckoutEquipmentItemViewModel : INotifyPropertyChanged
{
    private bool _isSelected;

    public CheckoutEquipmentItemViewModel(Equipment equipment, bool isSelected = false)
    {
        Id = equipment.Id;
        Identifier = equipment.Identifier;
        Name = equipment.Name;
        IsAvailable = equipment.IsAvailable;
        _isSelected = isSelected;
    }

    public int Id { get; }
    public string Identifier { get; }
    public string Name { get; }
    public bool IsAvailable { get; }
    public string AvailabilityText => IsAvailable ? "Available" : "Unavailable";

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
                return;
            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
