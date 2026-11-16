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
        var services = new ServiceCollection();

        System.IO.Directory.CreateDirectory("logs");
        System.IO.Directory.CreateDirectory("docs");

        var interceptor = new EquiBorrow.Infrastructure.Sql.EfCommandLoggingInterceptor();

        string connectionString;
        try
        {
            connectionString = ConnectionStringResolver.Resolve();
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return;
        }

        // One short-lived DbContext per operation (contexts aren't thread-safe or meant to span async work).
        services.AddDbContextFactory<EquipmentBorrowingDbContext>(options =>
            options.UseNpgsql(connectionString)
                   .LogTo(s => System.IO.File.AppendAllText(System.IO.Path.Combine("logs", "ef.log"), s + Environment.NewLine), Microsoft.Extensions.Logging.LogLevel.Information)
                   .AddInterceptors(interceptor)
            );

        // TRANSITIONAL: existing repositories still take a DbContext; removed in the service refactor.
        services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<EquipmentBorrowingDbContext>>().CreateDbContext());

        services.AddScoped<IStudentRepository, EfStudentRepository>();
        services.AddScoped<IEquipmentRepository, EfEquipmentRepository>();
        services.AddScoped<IBorrowingRepository, EfBorrowingRepository>();
        services.AddScoped<EquiBorrow.Infrastructure.Sql.SqlInspector>();
        services.AddScoped<IInspectionService>(sp => sp.GetRequiredService<EquiBorrow.Infrastructure.Sql.SqlInspector>());

        services.AddTransient<MainViewModel>();
        services.AddTransient<MainWindow>();

        var serviceProvider = services.BuildServiceProvider();
        App.ServiceProvider = serviceProvider;

        // Apply migrations and seed once (only into an empty database).
        try
        {
            DatabaseInitializer.InitializeAsync(
                serviceProvider.GetRequiredService<IDbContextFactory<EquipmentBorrowingDbContext>>())
                .GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Could not initialize the PostgreSQL database: " + ex.Message);
            System.IO.File.AppendAllText(System.IO.Path.Combine("logs", "startup.log"), DateTime.Now + " " + ex + Environment.NewLine);
            return;
        }

        var windowScope = serviceProvider.CreateScope();
        App.WindowScope = windowScope;

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

        windowScope.Dispose();
    }
}
