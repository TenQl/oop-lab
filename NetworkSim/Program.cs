using System;
using System.Windows.Forms;

namespace NetworkSim
{
    /// <summary>Точка входа приложения.</summary>
    internal static class Program
    {
        /// Главная точка входа: настраивает приложение и запускает главное окно.
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.Run(new MainForm());
        }
    }
}
