using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace SessionTrayMonitor
{
    public partial class LogViewerForm : Form
    {
        public LogViewerForm()
        {
            InitializeComponent();
            LoadLogData();
        }

        private void LoadLogData()
        {
            string csvPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "session_log.csv"
            );

            if (!File.Exists(csvPath))
            {
                MessageBox.Show("No log file found.");
                return;
            }

            var dailyTotals = File.ReadAllLines(csvPath)
                .Skip(1)
                .Select(line => line.Split(','))
                .Where(parts => DateTime.TryParse(parts[0], out _) && DateTime.TryParse(parts[1], out _))
                .Select(parts => new
                {
                    Start = DateTime.Parse(parts[0]),
                    End = DateTime.Parse(parts[1])
                })
                .GroupBy(entry => entry.Start.Date)
                .Select(group => new
                {
                    Date = group.Key,
                    TotalTime = group.Sum(entry => (entry.End - entry.Start).TotalMinutes)
                })
                .Where(entry => entry.TotalTime >= 60)
                .OrderByDescending(entry => entry.Date)
                .Take(30)
                .ToList();

            var table = new DataTable();
            table.Columns.Add("Date", typeof(string));
            table.Columns.Add("Time Logged", typeof(string));

            foreach (var row in dailyTotals)
            {
                table.Rows.Add(row.Date.ToShortDateString(), $"{(int)(row.TotalTime / 60):D2}:{(int)(row.TotalTime % 60):D2}");
            }

            dataGridView1.DataSource = table;
        }
    }
}