using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;

namespace EquiBorrow.UI;

public partial class App : global::Avalonia.Application
{
    public static global::System.IServiceProvider? ServiceProvider { get; set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (ServiceProvider != null)
            {
                desktop.MainWindow = ServiceProvider.GetService(typeof(MainWindow)) as MainWindow;
            }
            else
            {
                throw new InvalidOperationException(
                    "The application service provider is not configured. Configure dependency injection before starting Avalonia.");
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
