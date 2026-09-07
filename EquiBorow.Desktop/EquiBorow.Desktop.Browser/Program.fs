open System.Runtime.Versioning
open Avalonia
open Avalonia.Browser
open EquiBorow.Desktop

module Program =
    [<assembly: SupportedOSPlatform("browser")>]
    do ()

    [<CompiledName "BuildAvaloniaApp">] 
    let buildAvaloniaApp () = 
        AppBuilder
            .Configure<App>()

    [<EntryPoint>]
    let main argv =
        task {
            do! (buildAvaloniaApp()
            .WithInterFont()
            .StartBrowserAppAsync("out"))
        }
        |> ignore
        0