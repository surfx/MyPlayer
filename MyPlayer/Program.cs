using Avalonia;
using Serilog;

namespace MyPlayer;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.File("logs/myplayer-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Erro fatal na aplicação");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
