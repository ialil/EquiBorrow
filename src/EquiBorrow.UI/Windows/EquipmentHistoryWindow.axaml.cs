using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using EquiBorrow.UI.ViewModels;

namespace EquiBorrow.UI.Windows;

public partial class EquipmentHistoryWindow : Window
{
    private readonly EquipmentHistoryViewModel? _viewModel;

    public EquipmentHistoryWindow()
    {
        InitializeComponent();
    }

    public EquipmentHistoryWindow(EquipmentHistoryViewModel viewModel) : this()
    {
        _viewModel = viewModel;
        DataContext = _viewModel;
        Closed += (_, _) => _viewModel?.Dispose();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
