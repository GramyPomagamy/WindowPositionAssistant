using System.Runtime.InteropServices;
using System.Diagnostics;
using Application = System.Windows.Forms.Application;

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
            private NotifyIcon NotifyIcon;
            private WebApplication WebApplication;

            public WindowPositionAssistantContext()
            {
                NotifyIcon = SetupNotifyIcon();
                WebApplication = SetupApplication();

                WebApplication.StartAsync();
            }

            private WebApplication SetupApplication()
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

                notifyIcon.ContextMenuStrip.Items.Add(new ToolStripMenuItem("Exit", null, new EventHandler(Exit)));

                return notifyIcon;
            }

            private void Exit(object sender, EventArgs e)
            {
                WebApplication.StopAsync();
                NotifyIcon.Dispose();
                Application.Exit();
            }
        }

        [DllImport("user32.dll")]
        static extern bool ClientToScreen(IntPtr hwnd, ref Point point);
        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);
        [DllImport("user32.dll")]
        static extern bool GetClientRect(IntPtr hwnd, ref Rect rectangle);
        [DllImport("dwmapi.dll")]
        static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out Rect pvAttribute, int cbAttribute);

        static List<WindowInfo> GetWindows()
        {
            List<WindowInfo> results = new();

            Process[] processlist = Process.GetProcesses();
            foreach (Process process in processlist)
            {
                WindowInfo windowInfo = new WindowInfo(process.Id, process.ProcessName, process.MainWindowTitle);

                Rect clientRect = new Rect();
                Rect windowRect = new Rect();
                Rect rect;
                Point point = new Point();

                GetWindowRect(process.MainWindowHandle, ref windowRect);
                GetClientRect(process.MainWindowHandle, ref clientRect);
                ClientToScreen(process.MainWindowHandle, ref point);

                int size = Marshal.SizeOf(typeof(Rect));
                DwmGetWindowAttribute(process.MainWindowHandle, (int)DwmWindowAttribute.DWMWA_EXTENDED_FRAME_BOUNDS, out rect, size);

                int borderWidth = (windowRect.Right - windowRect.Left - (clientRect.Right - clientRect.Left)) / 2;
                
                windowInfo.X = point.X;
                windowInfo.Y = point.Y;
                windowInfo.W = clientRect.Right - clientRect.Left;
                windowInfo.H = clientRect.Bottom - clientRect.Top;

                if (windowInfo.W > 0 && windowInfo.H > 0)
                {
                    results.Add(windowInfo);
                }

            }

            results.Sort(comparison: (a, b) => a.ProcessName.CompareTo(b.ProcessName));
            return results;
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

    [Flags]
    enum DwmWindowAttribute : uint
    {
        DWMWA_NCRENDERING_ENABLED = 1,
        DWMWA_NCRENDERING_POLICY,
        DWMWA_TRANSITIONS_FORCEDISABLED,
        DWMWA_ALLOW_NCPAINT,
        DWMWA_CAPTION_BUTTON_BOUNDS,
        DWMWA_NONCLIENT_RTL_LAYOUT,
        DWMWA_FORCE_ICONIC_REPRESENTATION,
        DWMWA_FLIP3D_POLICY,
        DWMWA_EXTENDED_FRAME_BOUNDS,
        DWMWA_HAS_ICONIC_BITMAP,
        DWMWA_DISALLOW_PEEK,
        DWMWA_EXCLUDED_FROM_PEEK,
        DWMWA_CLOAK,
        DWMWA_CLOAKED,
        DWMWA_FREEZE_REPRESENTATION,
        DWMWA_LAST
    }
}
