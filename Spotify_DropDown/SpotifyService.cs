using Spotify_DropDown.Models;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;

namespace Spotify_DropDown.Services;

public class SpotifyService
{
    public event Action? LoginSucceeded;

    private async Task OnErrorReceived(
        object sender,
        string error,
        string? state)
    {
        System.Windows.MessageBox.Show(error);
        await Task.CompletedTask;
    }

    private async Task OnAuthorizationCodeReceived(
        object sender, AuthorizationCodeResponse response)
    {
        var tokenResponse = await new OAuthClient().RequestToken(
            new PKCETokenRequest(
                ClientId,
                response.Code,
                new Uri("http://127.0.0.1:5000/callback"),
                codeVerifier!));

        TokenStorage.SaveRefreshToken(tokenResponse.RefreshToken);

        Client = new SpotifyClient(tokenResponse.AccessToken);

        var me = await Client.UserProfile.Current();

        System.Windows.MessageBox.Show($"Logged in as {me.DisplayName}");

        await ActivateFirstDevice();

        LoginSucceeded?.Invoke();

        var devices = await GetDevices();   //debug code

        string text = string.Join("\n",
            devices.Select(d => $"{d.Name} (Active: {d.IsActive})"));

        System.Windows.MessageBox.Show(text);
    }

    private const string ClientId = "a5f9ead859a746a884e96caa854709a5";
    private string? codeVerifier;

    public SpotifyClient? Client { get; private set; }

    public async Task LoginAsync()
    {   

        var server = new EmbedIOAuthServer(
            new Uri("http://127.0.0.1:5000/callback"),
            5000);
        await server.Start();

        server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;
        server.ErrorReceived += OnErrorReceived;

        var pkce = PKCEUtil.GenerateCodes();
        codeVerifier = pkce.verifier;

        var request = new LoginRequest(
            server.BaseUri,
            ClientId,
            LoginRequest.ResponseType.Code)
        {
            Scope = new[]
            {
                Scopes.UserReadPlaybackState,
                Scopes.UserModifyPlaybackState,
                Scopes.UserReadCurrentlyPlaying
            },
            
            CodeChallengeMethod = "S256",
            CodeChallenge = pkce.challenge
        };

        BrowserUtil.Open(request.ToUri());
    }   

    public async Task<bool> TogglePlayback()
    {
        if (Client == null)
        {
            return false;
        }
        try
        {
            var playback = await Client.Player.GetCurrentPlayback();

            if (playback?.IsPlaying == true)
            {
                await Client.Player.PausePlayback();
                return false;
            }
            else
            {
                await Client.Player.ResumePlayback();
                return true;
            }
        }
        catch (APIException ex)
        {
            System.Windows.MessageBox.Show($"Spotify error:\n{ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error:\n{ex.Message}");
            return false;
        }
    }

    public async Task<bool> ActivateFirstDevice()
    {
        if (Client == null) return false;

        var devices = await Client.Player.GetAvailableDevices();
        var device = devices.Devices.FirstOrDefault();
        if (device == null)
        {
            return false;
        }
        await Client.Player.TransferPlayback(
            new PlayerTransferPlaybackRequest(
                new List<string> { device.Id }));

        return true;
    }

    public async Task<List<Device>> GetDevices()
    {
        if (Client == null) return new List<Device>();

        var devices = await Client.Player.GetAvailableDevices();

        return devices.Devices.ToList();
    }

    public async Task Next()
    {
        if (Client != null) await Client.Player.SkipNext();
    }

    public async Task Previous()
    {
        if (Client != null) await Client.Player.SkipPrevious();
    }

    public async Task SetVolume(int volume)
    {
        if (Client != null){
            await Client.Player.SetVolume(new PlayerVolumeRequest(volume));
        }
    }
    public async Task<PlaybackInfo?> GetPlaybackInfo()
    {
        if (Client == null) return null;
        var playback = await Client.Player.GetCurrentPlayback();
        if (playback == null) return null;

        var info = new PlaybackInfo
        {
            IsPlaying = playback.IsPlaying,
            VolumePercent = playback.Device?.VolumePercent ?? 0
        };

        if(playback.Item is FullTrack track)
        {
            info.Song = track.Name;
            info.Artist = string.Join(", ", track.Artists.Select(a => a.Name));
            info.AlbumArtUrl = track.Album.Images.FirstOrDefault()?.Url;

        }

        return info;
    }

    public async Task<bool> AutoLoginAsync()
    {
        var refreshToken = TokenStorage.LoadRefreshToken();

        if (string.IsNullOrWhiteSpace(refreshToken)) return false;

        try
        {
            var response = await new OAuthClient().RequestToken(
                new PKCETokenRefreshRequest(
                    ClientId,
                    refreshToken));

            Client = new SpotifyClient(response.AccessToken);

            await ActivateFirstDevice();
            return true;
        }

        catch
        {
            return false;
        }
    }
}
