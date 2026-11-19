using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using EquiBorrow.UI.ViewModels;
using EquiBorrow.UI.Windows;

namespace EquiBorrow.UI;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly HistoryWindowFactory _historyWindowFactory;

    // MainWindow will be created by the DI container; accept view model via constructor
    public MainWindow(MainViewModel viewModel, HistoryWindowFactory historyWindowFactory)
    {
        _viewModel = viewModel;
        _historyWindowFactory = historyWindowFactory;
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

    private void Close_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}
