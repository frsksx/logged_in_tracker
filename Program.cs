using System;
using System.Threading;
using System.Windows.Forms;

namespace SessionTrayMonitor
{
    internal static class Program
    {
        // Unique mutex name - change this to something unique to your app
        private const string MutexName = "SessionTrayMonitor_UniqueMutexName_123456";

        [STAThread]
        static void Main()
        {
            bool createdNew;
            using (Mutex mutex = new Mutex(true, MutexName, out createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show("Another instance of SessionTrayMonitor is already running.", "Instance Running", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return; // Exit this instance
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());

                // Mutex is released automatically on app exit because of 'using'
            }
        }
    }
}