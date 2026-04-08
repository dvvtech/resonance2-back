using System.Text.Json.Serialization;

namespace Resonance.Api.Models;

public sealed class Room
{
    [JsonIgnore]
    public object SyncRoot { get; } = new();

    public string Id { get; set; } = string.Empty;

    public string HostConnectionId { get; set; } = string.Empty;

    public string TrackUrl { get; set; } = string.Empty;

    public bool IsPlaying { get; set; }

    public double CurrentTime { get; set; }

    public DateTime LastUpdateUtc { get; set; }

    public DateTime StartTimestampUtc { get; set; }
}
