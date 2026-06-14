using Avalonia;
using Serilog;
using System.Runtime.InteropServices;

namespace MyPlayer;

internal static class Program
{
    [DllImport("libX11")]
    public static extern int XInitThreads();

    [STAThread]
    static void Main(string[] args)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            try
            {
                XInitThreads();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Aviso: Falha ao inicializar XInitThreads: {ex.Message}");
            }
        }

        try
        {
            LibVLCSharp.Shared.Core.Initialize();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao inicializar LibVLC: {ex.Message}");
        }

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
            .With(new X11PlatformOptions { EnableMultiTouch = true })
            .LogToTrace();
}
