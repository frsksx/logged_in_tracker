using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace SessionTrayMonitor
{
    public class SessionMonitor
    {
        const uint DESKTOP_READOBJECTS = 0x0001;
        const string CsvFileName = "session_log.csv";
        readonly string CsvFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            CsvFileName
        );
        private DateTime? sessionStart = null;
        private DateTime? sessionEnd = null;
        private DateTime lastUpdateTime = DateTime.MinValue;
        private bool running = true;
        private readonly Action<string, string> notifier;

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr OpenInputDesktop(uint dwFlags, bool fInherit, uint dwDesiredAccess);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool CloseDesktop(IntPtr hDesktop);

        public SessionMonitor(Action<string, string> notifyCallback)
        {
            notifier = notifyCallback;

            if (!File.Exists(CsvFilePath))
            {
                File.WriteAllText(CsvFilePath, "SessionStart,SessionEnd\n");
            }
        }

        public void StartMonitoring()
        {
            while (running)
            {
                bool isLoggedIn = Environment.UserInteractive && !string.IsNullOrEmpty(Environment.UserName);
                bool isUnlocked = IsSessionUnlocked();
                DateTime now = DateTime.Now;

                if (isLoggedIn && isUnlocked)
                {
                    if (sessionStart == null)
                    {
                        sessionStart = now;
                        sessionEnd = now;
                        AppendOrUpdateSessionCsv(sessionStart.Value, sessionEnd);
                        lastUpdateTime = now;
                        notifier("New Session", $"Started at {sessionStart.Value:HH:mm:ss}");
                    }
                    else
                    {
                        if ((now - lastUpdateTime).TotalSeconds >= 15)
                        {
                            sessionEnd = now;
                            AppendOrUpdateSessionCsv(sessionStart.Value, sessionEnd);
                            lastUpdateTime = now;
                        }
                    }
                }
                else
                {
                    if (sessionStart != null)
                    {
                        sessionEnd = now;
                        AppendOrUpdateSessionCsv(sessionStart.Value, sessionEnd);
                        notifier("Session Ended", $"Ended at {sessionEnd.Value:HH:mm:ss}");
                        sessionStart = null;
                        sessionEnd = null;
                        lastUpdateTime = DateTime.MinValue;
                    }
                }

                Thread.Sleep(5000);
            }
        }

        public void StopMonitoring()
        {
            running = false;
        }

        private bool IsSessionUnlocked()
        {
            IntPtr hDesktop = OpenInputDesktop(0, false, DESKTOP_READOBJECTS);
            if (hDesktop == IntPtr.Zero)
                return false;

            CloseDesktop(hDesktop);
            return true;
        }

        private static DateTime TruncateToSecond(DateTime dt) =>
            new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second);

        private void AppendOrUpdateSessionCsv(DateTime sessionStart, DateTime? sessionEnd)
        {
            try
            {
                var lines = new List<string>(File.ReadAllLines(CsvFilePath));

                if (lines.Count == 0)
                    lines.Add("SessionStart,SessionEnd");

                int lastDataLineIndex = -1;
                for (int i = lines.Count - 1; i > 0; i--)
                {
                    if (!string.IsNullOrWhiteSpace(lines[i]))
                    {
                        lastDataLineIndex = i;
                        break;
                    }
                }

                if (lastDataLineIndex == -1)
                {
                    lines.Add($"{sessionStart:yyyy-MM-dd HH:mm:ss},{sessionEnd:yyyy-MM-dd HH:mm:ss}");
                }
                else
                {
                    var parts = lines[lastDataLineIndex].Split(',');

                    if (parts.Length >= 1 && DateTime.TryParse(parts[0], out DateTime lastSessionStart))
                    {
                        if (TruncateToSecond(lastSessionStart) == TruncateToSecond(sessionStart))
                            lines[lastDataLineIndex] = $"{sessionStart:yyyy-MM-dd HH:mm:ss},{sessionEnd:yyyy-MM-dd HH:mm:ss}";
                        else
                            lines.Add($"{sessionStart:yyyy-MM-dd HH:mm:ss},{sessionEnd:yyyy-MM-dd HH:mm:ss}");
                    }
                    else
                    {
                        lines.Add($"{sessionStart:yyyy-MM-dd HH:mm:ss},{sessionEnd:yyyy-MM-dd HH:mm:ss}");
                    }
                }

                File.WriteAllLines(CsvFilePath, lines);
            }
            catch { }
        }
    }
}