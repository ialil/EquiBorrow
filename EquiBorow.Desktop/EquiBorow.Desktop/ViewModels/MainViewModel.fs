namespace EquiBorow.Desktop.ViewModels


type MainViewModel() =
    inherit ViewModelBase()

    let mutable greeting = "Welcome to Avalonia!"

    member this.Greeting
        with get () = greeting
        and set value = this.SetProperty(&greeting, value) |> ignore
