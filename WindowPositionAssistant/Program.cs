using System.Runtime.InteropServices;
using System.Diagnostics;
using Application = System.Windows.Forms.Application;
using System.Net.Http;
using System.Text.Json;

namespace WindowPositionAssistant
{
    class Program
    {
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new WindowPositionAssistantContext());
        }

        public class WindowPositionAssistantContext : ApplicationContext
        {
            private readonly NotifyIcon NotifyIcon;
            private readonly WebApplication WebApplication;
            private readonly ToolStripMenuItem SubmitterMenuItem;
            private readonly IConfiguration Configuration;
            private System.Threading.Timer? SubmitterTimer;

            public WindowPositionAssistantContext()
            {
                Configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

                SubmitterMenuItem = new ToolStripMenuItem("Submit online", null, new EventHandler(HandleSubmitter));
                NotifyIcon = SetupNotifyIcon();
                WebApplication = SetupApplication();

                WebApplication.StartAsync();
            }

            private static WebApplication SetupApplication()
            {
                var builder = WebApplication.CreateBuilder();
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen();

                var app = builder.Build();

                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }

                app.MapGet("/windows", () =>
                {
                    var windows = GetWindows();
                    return windows;
                })
                .WithName("GetWindows");

                return app;
            }

            private NotifyIcon SetupNotifyIcon()
            {
                var notifyIcon = new NotifyIcon()
                {
                    Icon = new Icon("icon.ico"),
                    ContextMenuStrip = new ContextMenuStrip(),
                    Visible = true
                };

                notifyIcon.ContextMenuStrip.Items.Add(SubmitterMenuItem);
                notifyIcon.ContextMenuStrip.Items.Add(new ToolStripMenuItem("Exit", null, new EventHandler(HandleExit)));

                return notifyIcon;
            }

            private void HandleExit(object? sender, EventArgs e)
            {
                WebApplication.StopAsync();
                NotifyIcon.Dispose();
                Application.Exit();
            }

            private void HandleSubmitter(object? sender, EventArgs e)
            {
                SubmitterMenuItem.Checked = !SubmitterMenuItem.Checked;
                if (SubmitterMenuItem.CheckState == CheckState.Checked)
                {
                    StartSubmitting();
                }
                else
                {
                    StopSubmitting();
                }
            }

            private void StartSubmitting()
            {
                int id = Random.Shared.Next(100, 1000);  // [100-999]
                SubmitterMenuItem.Text = "Online ID: " + id.ToString();
                SubmitterTimer = new System.Threading.Timer(SubmitWindows, id, 0, int.Parse(Configuration.GetSection("ProxySettings")["PeriodMs"]));
            }

            private void StopSubmitting()
            {
                if (SubmitterTimer != null)
                {
                    SubmitterMenuItem.Text = "Submit online";
                    SubmitterTimer.Dispose();
                }
            }

            private async void SubmitWindows(Object? stateInfo)
            {
                if (stateInfo == null)
                {
                    return;
                }

                var windows = GetWindows();
                int id = (int)stateInfo;
                var client = new HttpClient();

                try
                {
                    var serializerOptions = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    };

                    await client.PostAsync(
                        $"{Configuration.GetSection("ProxySettings")["Url"]}/{id}",
                        new StringContent(JsonSerializer.Serialize(windows, serializerOptions))
                    );
                }
                catch (HttpRequestException) { }
                catch (System.Net.Sockets.SocketException) { }
                catch (System.IO.IOException) { }
            }

            [DllImport("user32.dll")]
            static extern bool ClientToScreen(IntPtr hwnd, ref Point point);
            [DllImport("user32.dll")]
            static extern bool GetClientRect(IntPtr hwnd, ref Rect rectangle);

            static List<WindowInfo> GetWindows()
            {
                List<WindowInfo> results = new();
                Process[] processes = Process.GetProcesses();

                foreach (Process process in processes)
                {
                    WindowInfo windowInfo = new(process.Id, process.ProcessName, process.MainWindowTitle);

                    Rect clientAreaRect = new();
                    Point clientAreaAnchor = new();

                    GetClientRect(process.MainWindowHandle, ref clientAreaRect);
                    ClientToScreen(process.MainWindowHandle, ref clientAreaAnchor);

                    process.Dispose();  // important to free underlying resources

                    windowInfo.X = clientAreaAnchor.X;
                    windowInfo.Y = clientAreaAnchor.Y;
                    windowInfo.W = clientAreaRect.Right - clientAreaRect.Left;
                    windowInfo.H = clientAreaRect.Bottom - clientAreaRect.Top;

                    if (windowInfo.W > 0 && windowInfo.H > 0)
                    {
                        results.Add(windowInfo);
                    }
                }

                results.Sort(comparison: (a, b) => a.ProcessName.CompareTo(b.ProcessName));

                Array.Clear(processes);
                GC.Collect();

                return results;
            }
        }
    }

    public struct Point
    {
        public int X;
        public int Y;
    }

    public struct Rect
    {
        public int Left { get; set; }
        public int Top { get; set; }
        public int Right { get; set; }
        public int Bottom { get; set; }
    }

    public class WindowInfo
    {
        public int PID { get; }
        public string ProcessName { get; }
        public string WindowTitle { get; }
        public int X { get; set; }
        public int Y { get; set; }
        public int W { get; set; }
        public int H { get; set; }


        public WindowInfo(int pid, string processName, string windowTitle)
        {
            PID = pid;
            ProcessName = processName;
            WindowTitle = windowTitle;
        }
    }
}
