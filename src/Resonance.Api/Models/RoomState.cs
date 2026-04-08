namespace Resonance.Api.Models;

public sealed class RoomState
{
    public string RoomId { get; init; } = string.Empty;

    public string TrackUrl { get; init; } = string.Empty;

    public bool IsPlaying { get; init; }

    public double CurrentTime { get; init; }

    public DateTime ServerTimeUtc { get; init; }

    public DateTime LastUpdateUtc { get; init; }
}
