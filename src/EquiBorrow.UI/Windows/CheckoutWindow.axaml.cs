using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using EquiBorrow.UI.ViewModels;

namespace EquiBorrow.UI.Windows;

public partial class CheckoutWindow : Window
{
    public CheckoutWindow()
    {
        InitializeComponent();
    }

    public CheckoutWindow(CheckoutViewModel viewModel) : this()
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        Closed += (_, _) => ViewModel?.Dispose();
    }

    public CheckoutViewModel? ViewModel { get; }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void Close_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close();
}
