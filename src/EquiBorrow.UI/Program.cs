using Avalonia;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using EquiBorrow.Infrastructure.Persistence;
using EquiBorrow.Infrastructure.Repositories;
using EquiBorrow.Application.Interfaces;
using EquiBorrow.UI.ViewModels;


namespace EquiBorrow.UI;

public static class Program
{
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .UseReactiveUI()
            .LogToTrace();

    public static void Main(string[] args)
    {
        // Configure DI
        var services = new ServiceCollection();

        // Ensure log and docs directories exist
        System.IO.Directory.CreateDirectory("logs");
        System.IO.Directory.CreateDirectory("docs");

        var interceptor = new EquiBorrow.Infrastructure.Sql.EfCommandLoggingInterceptor();

        services.AddDbContext<EquipmentBorrowingDbContext>(options =>
            options.UseSqlite("Data Source=EquiBorrow.db")
                   .LogTo(s => System.IO.File.AppendAllText(System.IO.Path.Combine("logs","ef.log"), s + Environment.NewLine), Microsoft.Extensions.Logging.LogLevel.Information)
                   .AddInterceptors(interceptor)
            );

        services.AddScoped<IStudentRepository, EfStudentRepository>();
        services.AddScoped<IEquipmentRepository, EfEquipmentRepository>();
        services.AddScoped<IBorrowingRepository, EfBorrowingRepository>();
        // Register application-level inspection service implemented by the infrastructure
        // Register the concrete SqlInspector and map the application-level inspection service
        services.AddScoped<EquiBorrow.Infrastructure.Sql.SqlInspector>();
        services.AddScoped<IInspectionService>(sp => sp.GetRequiredService<EquiBorrow.Infrastructure.Sql.SqlInspector>());

        services.AddTransient<MainViewModel>();
        services.AddTransient<MainWindow>();

        var serviceProvider = services.BuildServiceProvider();

        // Expose the provider to App so it can resolve MainWindow
        App.ServiceProvider = serviceProvider;

        // Ensure database migrations are applied on startup
        using (var scope = serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EquipmentBorrowingDbContext>();
            db.Database.Migrate();
        }

        // Create a retained scope that will be used for resolving the MainWindow
        // This keeps scoped services (DbContext, repositories, inspection service) alive
        // for the lifetime of the MainWindow and avoids resolving scoped services from the root provider.
        var windowScope = serviceProvider.CreateScope();
        App.WindowScope = windowScope;

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

        // Dispose retained window scope after application exits
        windowScope.Dispose();
    }
}
