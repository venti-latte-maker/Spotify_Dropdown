using System.Windows.Media.Imaging;
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
        private bool updatingVolumeFromSpotify;
        private DispatcherTimer volumeTimer;
        private string? lastAlbumArtUrl;

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
            Loaded += MainWindow_Loaded;
            LoginButton.Click += LoginButton_Click;
            PlayPauseButton.Click += PlayPauseButton_Click;
            NextButton.Click += NextButton_Click;
            PrevButton.Click += PrevButton_Click;
            VolumeSlider.ValueChanged += VolumeSlider_ValueChange;
            spotifyService.LoginSucceeded += SpotifyService_LoginSucceeded;

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

            volumeTimer = new DispatcherTimer();
            volumeTimer.Interval = TimeSpan.FromMilliseconds(300);
            volumeTimer.Tick += async(_, _) =>
            {
                volumeTimer.Stop();

                await spotifyService.SetVolume((int)VolumeSlider.Value);
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

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            await spotifyService.LoginAsync();

        }

        private async void SpotifyTimer_Tick(object? sender, EventArgs e)
        {
            var playback = await spotifyService.GetPlaybackInfo();
            if (playback == null) return;

            SongTitle.Text = playback.Song;
            ArtistName.Text = playback.Artist;
            UpdateAlbumArt(playback.AlbumArtUrl);

            PlayPauseButton.Content = playback.IsPlaying? "⏸" : "▶";

            updatingVolumeFromSpotify = true;
            updatingVolumeFromSpotify = false;
        }

        private async void PlayPauseButton_Click(object sender,
            RoutedEventArgs e)
        {
            bool isPlaying = await spotifyService.TogglePlayback();
            PlayPauseButton.Content = isPlaying ? "⏸" : "▶";
        }

        private async void NextButton_Click(object sender, EventArgs e)
        {
            await spotifyService.Next();
        }

        private async void PrevButton_Click(object sender, EventArgs e)
        {
            await spotifyService.Previous();
        }

        private async void VolumeSlider_ValueChange(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (updatingVolumeFromSpotify) return;

            volumeTimer.Stop();
            volumeTimer.Start();
        }
        private void UpdateAlbumArt(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;
            if (imageUrl == lastAlbumArtUrl) return;
            lastAlbumArtUrl = imageUrl;
            AlbumArt.Source = new BitmapImage(new Uri(imageUrl));
        }

        private void SpotifyService_LoginSucceeded()
        {
            Dispatcher.Invoke(() =>
            {
                LoginButton.Visibility = Visibility.Collapsed;
            });
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            bool success = await spotifyService.AutoLoginAsync();

            if (success) LoginButton.Visibility = Visibility.Collapsed;
        }
    }
}


