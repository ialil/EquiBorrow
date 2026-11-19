using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using EquiBorrow.UI.ViewModels;

namespace EquiBorrow.UI.Windows;

public sealed class CheckoutWindowFactory
{
    private readonly IServiceProvider _services;

    public CheckoutWindowFactory(IServiceProvider services) => _services = services;

    public CheckoutWindow CreateCheckoutWindow(int? selectedStudentId)
    {
        var viewModel = ActivatorUtilities.CreateInstance<CheckoutViewModel>(_services, selectedStudentId ?? 0);
        return ActivatorUtilities.CreateInstance<CheckoutWindow>(_services, viewModel);
    }

    public void Show(Window window, Window owner) => window.Show(owner);
}
