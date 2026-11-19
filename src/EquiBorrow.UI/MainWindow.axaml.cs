using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using EquiBorrow.UI.ViewModels;
using EquiBorrow.UI.Windows;

namespace EquiBorrow.UI;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly HistoryWindowFactory _historyWindowFactory;
    private readonly CheckoutWindowFactory _checkoutWindowFactory;
    private readonly BorrowingSearchWindowFactory _borrowingSearchWindowFactory;

    // MainWindow will be created by the DI container; accept view model via constructor
    public MainWindow(
        MainViewModel viewModel,
        HistoryWindowFactory historyWindowFactory,
        CheckoutWindowFactory checkoutWindowFactory,
        BorrowingSearchWindowFactory borrowingSearchWindowFactory)
    {
        _viewModel = viewModel;
        _historyWindowFactory = historyWindowFactory;
        _checkoutWindowFactory = checkoutWindowFactory;
        _borrowingSearchWindowFactory = borrowingSearchWindowFactory;
        InitializeComponent();
        DataContext = _viewModel;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void StudentHistory_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_viewModel.SelectedStudent is { } student)
            _historyWindowFactory.Show(_historyWindowFactory.CreateStudentHistoryWindow(student.Id), this);
    }

    private void EquipmentHistory_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_viewModel.SelectedEquipment is { } equipment)
            _historyWindowFactory.Show(_historyWindowFactory.CreateEquipmentHistoryWindow(equipment.Id), this);
    }

    private void MultiItemCheckout_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var window = _checkoutWindowFactory.CreateCheckoutWindow(_viewModel.SelectedStudent?.Id);
        if (window.ViewModel is { } checkoutViewModel)
        {
            EventHandler refreshHandler = (_, _) => _viewModel.RefreshCommand.Execute(null);
            checkoutViewModel.RefreshMainWindowRequested += refreshHandler;
            window.Closed += (_, _) => checkoutViewModel.RefreshMainWindowRequested -= refreshHandler;
        }
        _checkoutWindowFactory.Show(window, this);
    }

    private void SearchBorrowings_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _borrowingSearchWindowFactory.Show(_borrowingSearchWindowFactory.CreateBorrowingSearchWindow(), this);
    }

    private void Close_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}
