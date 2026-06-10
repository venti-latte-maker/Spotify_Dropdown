namespace Spotify_DropDown.Models;

public class PlaybackInfo
{
    public string Song { get; set; } = "";
    public string Artist { get; set; } = "";
    public bool IsPlaying { get; set; }
    public int VolumePercent { get; set; }
    public string? AlbumArtUrl { get; set; }
}