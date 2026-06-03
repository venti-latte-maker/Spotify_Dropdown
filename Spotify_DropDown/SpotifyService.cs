using System.Linq;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;

namespace Spotify_DropDown.Services;


public class SpotifyService
{
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
            new AuthorizationCodeTokenRequest(
                ClientId,
                ClientSecret,
                response.Code,
                new Uri("http://127.0.0.1:5000/callback")));
        Client = new SpotifyClient(tokenResponse.AccessToken);

        var me = await Client.UserProfile.Current();

        System.Windows.MessageBox.Show($"Logged in as {me.DisplayName}");

        await ActivateFirstDevice();

        var devices = await GetDevices();   //debug code

        string text = string.Join("\n",
            devices.Select(d => $"{d.Name} (Active: {d.IsActive})"));

        System.Windows.MessageBox.Show(text);
    }

    private const string ClientId = "a5f9ead859a746a884e96caa854709a5";
    private const string ClientSecret = "14b8eef377cc4e3694cbf8e5c88db5bf";

    public SpotifyClient? Client { get; private set; }

    public async Task LoginAsync()
    {   

        var server = new EmbedIOAuthServer(
            new Uri("http://127.0.0.1:5000/callback"),
            5000);
        await server.Start();

        server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;
        server.ErrorReceived += OnErrorReceived;

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
            }
        };

        BrowserUtil.Open(request.ToUri());
    }

    public async Task<(string Song, string Artist)> GetCurrentTrack()
    {
        if (Client == null)
        {
            return ("Not Connected", "");
        }

        var playback = await Client.Player.GetCurrentPlayback();

        if (playback?.Item == null) return ("Nothing playing", "");

        if (playback.Item is FullTrack track)
        {
            return (
                track.Name,
                string.Join(",e ", track.Artists.Select(a => a.Name)));
        }

        return ("Unknown Item", "");
    }

    public async Task<bool> IsPlaying()
    {
        if (Client == null)
        {
            return false;
        }

        var playback = await Client.Player.GetCurrentPlayback();
        return playback?.IsPlaying ?? false;
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
}