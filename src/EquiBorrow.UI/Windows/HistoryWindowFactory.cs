using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using EquiBorrow.UI.ViewModels;

namespace EquiBorrow.UI.Windows;

public sealed class HistoryWindowFactory
{
    private readonly IServiceProvider _services;

    public HistoryWindowFactory(IServiceProvider services) => _services = services;

    public Window CreateStudentHistoryWindow(int studentId)
    {
        var viewModel = ActivatorUtilities.CreateInstance<StudentHistoryViewModel>(_services, studentId);
        var window = ActivatorUtilities.CreateInstance<StudentHistoryWindow>(_services, viewModel);
        return window;
    }

    public Window CreateEquipmentHistoryWindow(int equipmentId)
    {
        var viewModel = ActivatorUtilities.CreateInstance<EquipmentHistoryViewModel>(_services, equipmentId);
        var window = ActivatorUtilities.CreateInstance<EquipmentHistoryWindow>(_services, viewModel);
        return window;
    }

    public void Show(Window window, Window owner)
    {
        window.Show(owner);
    }
}
