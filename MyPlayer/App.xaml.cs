using System.Windows;
using Serilog;

namespace MyPlayer
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // ✅ Configura logging global
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File("logs/myplayer-.txt", rollingInterval: Serilog.RollingInterval.Day)
                .CreateLogger();

            // ✅ Tratamento global de exceções
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;

            Log.Information("Aplicação WPF iniciada");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            classes.keyhook.GlobalKeyboardHook.Unhook();
            Log.Information("Aplicação encerrada");
            Log.CloseAndFlush();
            base.OnExit(e);
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Error(e.Exception, "Exceção não tratada na thread da UI");
            MessageBox.Show($"Erro:\n{e.Exception.Message}", "Erro", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                Log.Fatal(ex, "Exceção não tratada no domínio");
                MessageBox.Show($"Erro fatal:\n{ex.Message}", "Erro Fatal", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
