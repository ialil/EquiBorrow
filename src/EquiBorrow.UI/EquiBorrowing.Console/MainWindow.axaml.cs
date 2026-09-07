using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;

namespace EquiBorrow.UI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var vm = new ViewModels.MainViewModel();
        DataContext = vm;

        // set Items
        var studentsList = this.FindControl<ListBox>("StudentsList");
        var equipmentsList = this.FindControl<ListBox>("EquipmentsList");
        if (studentsList != null)
            studentsList.ItemsSource = vm.Students;
        if (equipmentsList != null)
            equipmentsList.ItemsSource = vm.Equipments;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void Close_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
