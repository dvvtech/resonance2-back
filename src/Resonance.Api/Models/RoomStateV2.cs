namespace Resonance.Api.Models;

public sealed class RoomStateV2
{
    public string RoomId { get; init; } = string.Empty;

    public string TrackUrl { get; init; } = string.Empty;

    public bool IsPlaying { get; init; }

    public double PositionSeconds { get; init; }

    public long ReferenceServerTimeUnixMs { get; init; }

    public long ServerNowUnixMs { get; init; }

    public long Version { get; init; }

    public PendingRoomCommandStateV2? PendingCommand { get; init; }
}

public sealed class PendingRoomCommandStateV2
{
    public string Type { get; init; } = string.Empty;

    public long ExecuteAtUnixMs { get; init; }

    public double PositionSeconds { get; init; }

    public bool IsPlayingAfterExecution { get; init; }

    public long Version { get; init; }
}
