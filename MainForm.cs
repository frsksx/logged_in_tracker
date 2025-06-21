using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SessionTrayMonitor
{
    public partial class MainForm : Form
    {
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private SessionMonitor monitor;

        public MainForm()
        {
            InitializeComponent();

            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Exit", null, OnExit);

            trayIcon = new NotifyIcon
            {
                Text = "Session Monitor",
                Icon = new Icon("tray_icon.ico"),
                ContextMenuStrip = trayMenu,
                Visible = true
            };

            trayIcon.MouseClick += (s, e) => {
                if (e.Button == MouseButtons.Left) new LogViewerForm().Show();
            };

            trayIcon.DoubleClick += (s, e) =>
            {
                trayIcon.ShowBalloonTip(1000, "Session Monitor", "Running in background", ToolTipIcon.Info);
            };

            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Load += MainForm_Load;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            monitor = new SessionMonitor(Notify);
            Task.Run(() => monitor.StartMonitoring());
        }

        private void Notify(string title, string message)
        {
            trayIcon.ShowBalloonTip(1000, title, message, ToolTipIcon.Info);
        }

        private void OnExit(object sender, EventArgs e)
        {
            monitor?.StopMonitoring();
            trayIcon.Visible = false;
            Application.Exit();
        }
    }
}