using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using EquiBorrow.UI.ViewModels;

namespace EquiBorrow.UI;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    // MainWindow will be created by the DI container; accept view model via constructor
    public MainWindow(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = _viewModel;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void Close_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}
