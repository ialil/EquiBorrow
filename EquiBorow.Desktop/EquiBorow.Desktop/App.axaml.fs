namespace EquiBorow.Desktop

open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Data.Core
open Avalonia.Data.Core.Plugins
open Avalonia.Markup.Xaml
open EquiBorow.Desktop.ViewModels
open EquiBorow.Desktop.Views

type App() =
    inherit Application()

    override this.Initialize() =
            AvaloniaXamlLoader.Load(this)
#if DEBUG
            this.AttachDeveloperTools() |> ignore
#endif

    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktopLifetime ->
            desktopLifetime.MainWindow <- MainWindow(DataContext = MainViewModel())
        | :? IActivityApplicationLifetime as singleViewFactoryApplicationLifetime ->
            singleViewFactoryApplicationLifetime.MainViewFactory <- fun () -> MainView(DataContext = MainViewModel())
        | :? ISingleViewApplicationLifetime as singleViewLifetime ->
            singleViewLifetime.MainView <- MainView(DataContext = MainViewModel())
        | _ -> ()

        base.OnFrameworkInitializationCompleted()
