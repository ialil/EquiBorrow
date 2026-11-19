using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using EquiBorrow.UI.ViewModels;

namespace EquiBorrow.UI.Windows;

public partial class StudentHistoryWindow : Window
{
    private readonly StudentHistoryViewModel? _viewModel;

    public StudentHistoryWindow()
    {
        InitializeComponent();
    }

    public StudentHistoryWindow(StudentHistoryViewModel viewModel) : this()
    {
        _viewModel = viewModel;
        DataContext = _viewModel;
        Closed += (_, _) => _viewModel?.Dispose();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
