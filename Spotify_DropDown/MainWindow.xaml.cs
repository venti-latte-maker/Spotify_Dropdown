using System;
using System.Windows;
using System.Runtime.InteropServices;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Spotify_DropDown.Services;

namespace Spotify_DropDown
{

    public partial class MainWindow : Window
    {
        private SpotifyService spotifyService;
        private bool isHoveringWindow = false;
        private DispatcherTimer spotifyTimer;

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

        public MainWindow()     //Constructor
        {
            InitializeComponent(); // Ensure XAML-created controls are initialized first

            spotifyService = new SpotifyService();
            LoginButton.Click += LoginButton_Click;
            PlayPauseButton.Click += PlayPauseButton_Click;

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

            spotifyTimer = new DispatcherTimer();
            spotifyTimer.Interval = TimeSpan.FromSeconds(1);
            spotifyTimer.Tick += SpotifyTimer_Tick;
            spotifyTimer.Start();
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

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            await spotifyService.LoginAsync();

        }

        private async void SpotifyTimer_Tick(object? sender, EventArgs e)
        {
            var track = await spotifyService.GetCurrentTrack();

            SongTitle.Text = track.Song;
            ArtistName.Text = track.Artist;

            bool isPlaying = await spotifyService.IsPlaying();

            PlayPauseButton.Content = isPlaying ? "⏸" : "▶";
        }

        private async void PlayPauseButton_Click(object sender,
            RoutedEventArgs e)
        {
            bool isPlaying = await spotifyService.TogglePlayback();
            PlayPauseButton.Content = isPlaying ? "⏸" : "▶";
        }
    }
}

