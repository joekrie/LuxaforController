using LuxaforSharp;
using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LuxaforController.TrayApp
{
    public class TrayAppContext : ApplicationContext, IDisposable
    {
        private const int PomodoroInMilliseconds = 8000; //25 * 60 * 1000;
        private const int PomodoroBreakInMilliseconds = 3000; // 5 * 60 * 1000;

        private readonly NotifyIcon _notifyIcon = new NotifyIcon();
        private readonly DeviceList _luxaforDevices = new DeviceList();
        private IDevice _luxaforDevice;
        private readonly Timer _pomodoroTimer = new Timer();
        private readonly Timer _pomodoroBreakTimer = new Timer();
        private int _pomodoroPausedAt = 0;
        private readonly MenuItem _pomodoroMenuItem;
        private bool _inPomodoro = false;

        private event EventHandler PomodoroStarted;
        private event EventHandler PomodoroFinished;

        public TrayAppContext()
        {
            PomodoroStarted += async (s, e) => await OnPomodoroStarted();

            ConnectLuxafor();

            _notifyIcon.Icon = new Icon(SystemIcons.Application, 40, 40);
            _notifyIcon.Visible = true;
            _notifyIcon.BalloonTipClicked += (s, e) => ClickTooltip();

            _pomodoroMenuItem = new MenuItem("&Start pomodoro", (s, e) => PomodoroStarted(this, EventArgs.Empty));

            _notifyIcon.ContextMenu = new ContextMenu(new MenuItem[]
            {
                new MenuItem("&Available", async (s, e) => await SetAsAvailable()),
                new MenuItem("&Busy", async (s, e) => await SetAsBusy()),
                new MenuItem("&Off", async (s, e) => await TurnOff()),
                new MenuItem("-"),
                _pomodoroMenuItem,
                new MenuItem("-"),
                new MenuItem("&Reconnect", (s, e) => ConnectLuxafor()),
                new MenuItem("E&xit", (s, e) => Application.Exit())
            });

            _pomodoroTimer.Interval = PomodoroInMilliseconds;
            _pomodoroTimer.Tick += async (s, e) => await PomodoroFinished();

            _pomodoroBreakTimer.Interval = PomodoroBreakInMilliseconds;
            _pomodoroBreakTimer.Tick += async (s, e) => await PomodoroBreakFinished();
        }

        public async Task Initialize()
        {
            await SetAsAvailable();
        }

        public async Task CleanUp()
        {
            await TurnOff();
            _notifyIcon.Visible = false;
            _pomodoroTimer.Stop();
            _pomodoroTimer.Dispose();
            _pomodoroBreakTimer.Stop();
            _pomodoroBreakTimer.Dispose();
        }

        private void ConnectLuxafor()
        {
            if (_luxaforDevice != null)
            {
                _luxaforDevice.Dispose();
            }

            _luxaforDevices.Scan();
            _luxaforDevice = _luxaforDevices.FirstOrDefault();

            if (_luxaforDevice != null)
            {
                _luxaforDevice.Wave(WaveType.OverlappingLong, new LuxaforSharp.Color(255, 255, 255), 100, 5);
            }
        }

        private async Task SetAsAvailable()
        {
            if (_luxaforDevice != null)
            {
                await _luxaforDevice.SetColor(LedTarget.All, new LuxaforSharp.Color(0, 255, 0));
            }
        }

        private async Task SetAsBusy()
        {
            if (_luxaforDevice != null)
            {
                await _luxaforDevice.SetColor(LedTarget.All, new LuxaforSharp.Color(255, 0, 0));
            }
        }

        private async Task TurnOff()
        {
            if (_luxaforDevice != null)
            {
                await _luxaforDevice.SetColor(LedTarget.All, new LuxaforSharp.Color(0, 0, 0));
            }
        }

        private async Task OnPomodoroStarted()
        {
            _inPomodoro = true;
            await SetAsBusy();
            _pomodoroTimer.Start();
        }

        private async Task PausePomodoro()
        {
            _inPomodoro = false;
            await SetAsAvailable();
            _pomodoroPausedAt = _pomodoroTimer.Interval;
            _pomodoroTimer.Stop();
        }

        private async Task PomodoroFinished()
        {
            _inPomodoro = false;
            _notifyIcon.BalloonTipTitle = "Pomodoro";
            _notifyIcon.BalloonTipText = "Your Pomodoro session has finished.";
            _notifyIcon.ShowBalloonTip(100);
            await SetAsAvailable();
            _pomodoroTimer.Stop();
            await _luxaforDevice.CarryOutPattern(PatternType.RainbowWave, 5);
            _pomodoroBreakTimer.Start();
        }

        private async Task PomodoroBreakFinished()
        {            
            _notifyIcon.BalloonTipTitle = "Pomodoro";
            _notifyIcon.BalloonTipText = "Time for another Pomodoro. Click to start.";
            _notifyIcon.ShowBalloonTip(100);
            await SetAsBusy();
            _pomodoroBreakTimer.Stop();
        }

        private void ClickTooltip()
        {
            MessageBox.Show("clicked it");
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            CleanUp();
        }
    }
}