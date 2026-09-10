using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using EquiBorrow.Infrastructure.Repositories;
using EquiBorrow.UI.ViewModels;

namespace EquiBorrow.UI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Create repository instances
        var studentRepository = new InMemoryStudentRepository();
        var equipmentRepository = new InMemoryEquipmentRepository();
        var borrowingRepository = new InMemoryBorrowingRepository();

        // Create viewmodel with repositories
        var viewModel = new MainViewModel(studentRepository, equipmentRepository, borrowingRepository);

        // Set the data context
        DataContext = viewModel;
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
