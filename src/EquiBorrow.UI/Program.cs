using Avalonia;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using EquiBorrow.Infrastructure.Persistence;
using EquiBorrow.Application.Interfaces;
using EquiBorrow.Application.Services;
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

        // One short-lived DbContext per unit of work.
        services.AddDbContextFactory<EquipmentBorrowingDbContext>(options =>
            options.UseNpgsql(connectionString)
                   .LogTo(s => System.IO.File.AppendAllText(System.IO.Path.Combine("logs", "ef.log"), s + Environment.NewLine), Microsoft.Extensions.Logging.LogLevel.Information)
                   .AddInterceptors(interceptor)
            );

        services.AddSingleton<IUnitOfWorkFactory, EfUnitOfWorkFactory>();
        services.AddSingleton(TimeProvider.System);
        services.AddTransient<BorrowEquipmentService>();
        services.AddTransient<CheckOutMultipleEquipmentService>();
        services.AddTransient<ReturnEquipmentService>();
        services.AddTransient<RegisterStudentService>();
        services.AddTransient<RegisterEquipmentService>();
        services.AddTransient<UpdateStudentService>();
        services.AddTransient<UpdateEquipmentService>();
        services.AddTransient<ChangeStudentEligibilityService>();
        services.AddTransient<ListStudentsService>();
        services.AddTransient<ViewEquipmentCatalogService>();
        services.AddTransient<FindAvailableEquipmentService>();
        services.AddTransient<ViewActiveBorrowingsService>();
        services.AddTransient<ViewStudentBorrowingHistoryService>();
        services.AddTransient<ViewEquipmentBorrowingHistoryService>();
        services.AddSingleton<IInspectionService, EquiBorrow.Infrastructure.Sql.SqlInspector>();

        services.AddTransient<MainViewModel>();
        services.AddTransient<StudentHistoryViewModel>();
        services.AddTransient<EquipmentHistoryViewModel>();
        services.AddTransient<Windows.StudentHistoryWindow>();
        services.AddTransient<Windows.EquipmentHistoryWindow>();
        services.AddTransient<CheckoutViewModel>();
        services.AddTransient<Windows.CheckoutWindow>();
        services.AddSingleton<Windows.HistoryWindowFactory>();
        services.AddSingleton<Windows.CheckoutWindowFactory>();
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

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }
}
