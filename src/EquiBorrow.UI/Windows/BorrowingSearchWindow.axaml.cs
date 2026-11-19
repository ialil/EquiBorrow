using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using EquiBorrow.UI.ViewModels;

namespace EquiBorrow.UI.Windows;

public partial class BorrowingSearchWindow : Window
{
    private readonly BorrowingSearchViewModel? _viewModel;

    public BorrowingSearchWindow()
    {
        InitializeComponent();
    }

    public BorrowingSearchWindow(BorrowingSearchViewModel viewModel) : this()
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        Closed += (_, _) => _viewModel.Dispose();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void SearchText_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;
        _viewModel?.SearchNow();
        e.Handled = true;
    }
}
