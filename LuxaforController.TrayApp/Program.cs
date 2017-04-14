using System;
using System.Windows.Forms;

namespace LuxaforController.TrayApp
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var appContext = new TrayAppContext();
            appContext.Initialize();
            Application.ApplicationExit += async (s, e) => await appContext.CleanUp();
            Application.Run(appContext);            
        }
    }
}
