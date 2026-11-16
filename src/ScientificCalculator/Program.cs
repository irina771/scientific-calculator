using Avalonia;
using System;

namespace ScientificCalculator;

internal static class Program
{
    // No usar APIs de Avalonia ni código que dependa del SynchronizationContext
    // antes de que AppMain haya inicializado la aplicación.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // También lo usa el diseñador de XAML.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
