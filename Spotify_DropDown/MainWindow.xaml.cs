using System;
using System.Windows;
using System.Runtime.InteropServices;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Spotify_DropDown
{

    public partial class MainWindow : Window
    {
        private bool isHoveringWindow = false;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        static extern bool GetCursorPos(out POINT lpPoint);

        private DispatcherTimer timer;
        private DispatcherTimer hideTimer;

        public MainWindow()
        {
            InitializeComponent();

            PlayButton.Click += PlayButton_Click;

            Left = (SystemParameters.PrimaryScreenWidth - Width) / 2;
            Top = -Height;

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(50);
            timer.Tick += CheckMouse;
            timer.Start();

            hideTimer = new DispatcherTimer();
            hideTimer.Interval = TimeSpan.FromMilliseconds(500);
            hideTimer.Tick += (_, _) =>
            {
                hideTimer.Stop();
                if (!isHoveringWindow) HideDropdown();
            };

            MouseEnter += (_, _) =>
            {
                isHoveringWindow = true;
                hideTimer.Stop();
            };
            MouseLeave += (_, _) =>
            {
                isHoveringWindow = false;
                hideTimer.Start();
            };
        }

        private void CheckMouse(object? sender, EventArgs e)
        {
            GetCursorPos(out POINT point);
            if (point.Y <= 5)
            {
                hideTimer.Stop();
                ShowDropdown();
            }
            else if (!isHoveringWindow)
            {
                HideDropdown();
            }
        }

        private void ShowDropdown()
        {
            var animation = new DoubleAnimation(0,
                TimeSpan.FromMilliseconds(200));

            BeginAnimation(Window.TopProperty, animation);
        }

        private void HideDropdown()
        {
            var animation = new DoubleAnimation(-Height,
                TimeSpan.FromMilliseconds(200));

            BeginAnimation(Window.TopProperty, animation);
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            SongTitle.Text = "Play button clicked!";
        }
    }
}