namespace EquiBorow.Desktop.Android

open Android.App
open Android.Content.PM
open Avalonia
open Avalonia.Android
open EquiBorow.Desktop

[<Activity(
    Label = "EquiBorow.Desktop.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = (ConfigChanges.Orientation ||| ConfigChanges.ScreenSize ||| ConfigChanges.UiMode))>]
type MainActivity() =
    inherit AvaloniaMainActivity()
