using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

class Program
{
    const uint DESKTOP_READOBJECTS = 0x0001;
    const string CsvFilePath = "session_log.csv";

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr OpenInputDesktop(uint dwFlags, bool fInherit, uint dwDesiredAccess);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool CloseDesktop(IntPtr hDesktop);

    static void Main()
    {
        Console.WriteLine("Starting session monitor...");
        DateTime? sessionStart = null;
        DateTime? sessionEnd = null;
        DateTime lastUpdateTime = DateTime.MinValue;

        // Create CSV header if not exists
        if (!File.Exists(CsvFilePath))
        {
            File.WriteAllText(CsvFilePath, "SessionStart,SessionEnd\n");
        }

        while (true)
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
                    Console.WriteLine($"New session started at {sessionStart.Value:yyyy-MM-dd HH:mm:ss}");
                    AppendOrUpdateSessionCsv(sessionStart.Value, sessionEnd);
                    lastUpdateTime = now;
                }
                else
                {
                    if ((now - lastUpdateTime).TotalSeconds >= 15)
                    {
                        sessionEnd = now;
                        AppendOrUpdateSessionCsv(sessionStart.Value, sessionEnd);
                        lastUpdateTime = now;
                    }

                    Console.WriteLine($"Session start: {sessionStart.Value:yyyy-MM-dd HH:mm:ss} | Latest checked: {now:yyyy-MM-dd HH:mm:ss}");
                }
            }
            else
            {
                if (sessionStart != null)
                {
                    sessionEnd = now;
                    Console.WriteLine($"Session ended at {sessionEnd.Value:yyyy-MM-dd HH:mm:ss}");
                    AppendOrUpdateSessionCsv(sessionStart.Value, sessionEnd);

                    sessionStart = null;
                    sessionEnd = null;
                    lastUpdateTime = DateTime.MinValue;
                }
                else
                {
                    Console.WriteLine($"No active session at {now:yyyy-MM-dd HH:mm:ss}");
                }
            }

            Thread.Sleep(5000);
        }
    }

    static bool IsSessionUnlocked()
    {
        IntPtr hDesktop = OpenInputDesktop(0, false, DESKTOP_READOBJECTS);
        if (hDesktop == IntPtr.Zero)
        {
            return false;
        }
        else
        {
            CloseDesktop(hDesktop);
            return true;
        }
    }

    static DateTime TruncateToSecond(DateTime dt)
    {
        return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second);
    }

    static void AppendOrUpdateSessionCsv(DateTime sessionStart, DateTime? sessionEnd)
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
                lines.Add($"{sessionStart:yyyy-MM-dd HH:mm:ss},{(sessionEnd.HasValue ? sessionEnd.Value.ToString("yyyy-MM-dd HH:mm:ss") : "")}");
            }
            else
            {
                string lastLine = lines[lastDataLineIndex].Trim();
                var parts = lastLine.Split(',');

                if (parts.Length >= 1 && DateTime.TryParse(parts[0].Trim(), out DateTime lastSessionStart))
                {
                    DateTime roundedLast = TruncateToSecond(lastSessionStart);
                    DateTime roundedCurrent = TruncateToSecond(sessionStart);

                    if (roundedLast == roundedCurrent)
                    {
                        lines[lastDataLineIndex] = $"{sessionStart:yyyy-MM-dd HH:mm:ss},{(sessionEnd.HasValue ? sessionEnd.Value.ToString("yyyy-MM-dd HH:mm:ss") : "")}";
                    }
                    else
                    {
                        lines.Add($"{sessionStart:yyyy-MM-dd HH:mm:ss},{(sessionEnd.HasValue ? sessionEnd.Value.ToString("yyyy-MM-dd HH:mm:ss") : "")}");
                    }
                }
                else
                {
                    lines.Add($"{sessionStart:yyyy-MM-dd HH:mm:ss},{(sessionEnd.HasValue ? sessionEnd.Value.ToString("yyyy-MM-dd HH:mm:ss") : "")}");
                }
            }

            File.WriteAllLines(CsvFilePath, lines);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating CSV: {ex.Message}");
        }
    }
}
