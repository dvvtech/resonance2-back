namespace Resonance.Api.Models;

public sealed class RoomV2
{
    public object SyncRoot { get; } = new();

    public string Id { get; set; } = string.Empty;

    public string HostConnectionId { get; set; } = string.Empty;

    public string TrackUrl { get; set; } = string.Empty;

    public long Version { get; set; }

    public PlaybackSnapshotV2 Current { get; set; } = new();

    public PendingRoomCommandV2? PendingCommand { get; set; }
}

public sealed class PlaybackSnapshotV2
{
    public bool IsPlaying { get; set; }

    public double PositionSeconds { get; set; }

    public long ReferenceServerTimeUnixMs { get; set; }
}

public sealed class PendingRoomCommandV2
{
    public string Type { get; set; } = string.Empty;

    public long ExecuteAtUnixMs { get; set; }

    public double PositionSeconds { get; set; }

    public bool IsPlayingAfterExecution { get; set; }

    public long Version { get; set; }
}
