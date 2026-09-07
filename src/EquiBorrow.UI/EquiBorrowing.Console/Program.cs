using System;
using Avalonia;
using Avalonia.ReactiveUI;

namespace EquiBorrow.UI;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static global::Avalonia.AppBuilder BuildAvaloniaApp()
        => global::Avalonia.AppBuilder.Configure<global::Avalonia.Application>()
            .UsePlatformDetect()
            .UseReactiveUI()
            .LogToTrace();
}
