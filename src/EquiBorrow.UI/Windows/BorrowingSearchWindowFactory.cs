using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using EquiBorrow.UI.ViewModels;

namespace EquiBorrow.UI.Windows;

public sealed class BorrowingSearchWindowFactory
{
    private readonly IServiceProvider _services;

    public BorrowingSearchWindowFactory(IServiceProvider services) => _services = services;

    public BorrowingSearchWindow CreateBorrowingSearchWindow()
    {
        var viewModel = ActivatorUtilities.CreateInstance<BorrowingSearchViewModel>(_services);
        return ActivatorUtilities.CreateInstance<BorrowingSearchWindow>(_services, viewModel);
    }

    public void Show(Window window, Window owner) => window.Show(owner);
}
