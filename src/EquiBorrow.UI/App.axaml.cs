using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;

namespace EquiBorrow.UI;

public partial class App : global::Avalonia.Application
{
    public static global::System.IServiceProvider? ServiceProvider { get; set; }
    public static IServiceScope? WindowScope { get; set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Resolve MainWindow from the service provider when available
            if (WindowScope != null)
            {
                // Resolve MainWindow from a retained scope so scoped services remain valid for the window lifetime
                desktop.MainWindow = WindowScope.ServiceProvider.GetService(typeof(MainWindow)) as MainWindow;
            }
            else if (ServiceProvider != null)
            {
                desktop.MainWindow = ServiceProvider.GetService(typeof(MainWindow)) as MainWindow;
            }
            else
            {
                // Fallback to in-memory repositories if DI is not configured (useful for designer/debug scenarios)
                var studentRepo = new EquiBorrow.Infrastructure.Repositories.InMemoryStudentRepository();
                var equipmentRepo = new EquiBorrow.Infrastructure.Repositories.InMemoryEquipmentRepository();
                var borrowingRepo = new EquiBorrow.Infrastructure.Repositories.InMemoryBorrowingRepository();
                var sqlInspector = new EquiBorrow.Infrastructure.Sql.NoOpSqlInspector();
                var vm = new EquiBorrow.UI.ViewModels.MainViewModel(studentRepo, equipmentRepo, borrowingRepo, sqlInspector);
                desktop.MainWindow = new MainWindow(vm);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
