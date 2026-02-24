using Serilog;

namespace MyPlayer
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // ✅ Configura logging global
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File("logs/myplayer-.txt", rollingInterval: Serilog.RollingInterval.Day)
                .CreateLogger();

            // ✅ Tratamento global de exceções
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            try
            {
                ApplicationConfiguration.Initialize();
                Application.Run(new frmMyPlayer());
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Erro fatal na aplicação");
                MessageBox.Show($"Erro fatal:\n{ex.Message}", "Erro", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            Log.Error(e.Exception, "Exceção não tratada na thread da UI");
            MessageBox.Show($"Erro:\n{e.Exception.Message}", "Erro", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                Log.Fatal(ex, "Exceção não tratada no domínio");
                MessageBox.Show($"Erro fatal:\n{ex.Message}", "Erro Fatal", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}