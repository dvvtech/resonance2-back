using System.Collections.Concurrent;
using System.Security.Cryptography;
using Resonance.Api.Models;

namespace Resonance.Api.Services;

public sealed class RoomSyncServiceV2
{
    private const int PlayLeadTimeMs = 1500;
    private const int SeekLeadTimeMs = 1000;

    private readonly ConcurrentDictionary<string, RoomV2> _rooms = new(StringComparer.OrdinalIgnoreCase);

    public string CreateRoom(string hostConnectionId)
    {
        while (true)
        {
            var roomId = Convert.ToHexString(RandomNumberGenerator.GetBytes(3));
            var now = GetUnixTimeMilliseconds();
            var room = new RoomV2
            {
                Id = roomId,
                HostConnectionId = hostConnectionId,
                TrackUrl = string.Empty,
                Version = 0,
                Current = new PlaybackSnapshotV2
                {
                    IsPlaying = false,
                    PositionSeconds = 0,
                    ReferenceServerTimeUnixMs = now,
                },
            };

            if (_rooms.TryAdd(roomId, room))
            {
                return roomId;
            }
        }
    }

    public bool RoomExists(string roomId) => _rooms.ContainsKey(roomId);

    public RoomStateV2 GetRoomState(string roomId)
    {
        var room = GetRoomOrThrow(roomId);
        lock (room.SyncRoot)
        {
            var now = GetUnixTimeMilliseconds();
            PromotePendingCommandIfDue(room, now);
            return CreateState(room, now);
        }
    }

    public RoomStateV2 SetTrack(string roomId, string url)
    {
        var room = GetRoomOrThrow(roomId);
        lock (room.SyncRoot)
        {
            var now = GetUnixTimeMilliseconds();
            PromotePendingCommandIfDue(room, now);

            room.TrackUrl = url.Trim();
            room.PendingCommand = null;
            room.Current.IsPlaying = false;
            room.Current.PositionSeconds = 0;
            room.Current.ReferenceServerTimeUnixMs = now;
            room.Version++;

            return CreateState(room, now);
        }
    }

    public RoomStateV2 Play(string roomId, double startTime)
    {
        var room = GetRoomOrThrow(roomId);
        lock (room.SyncRoot)
        {
            var now = GetUnixTimeMilliseconds();
            PromotePendingCommandIfDue(room, now);

            var sanitizedStartTime = SanitizeTime(startTime);
            room.Current.IsPlaying = false;
            room.Current.PositionSeconds = sanitizedStartTime;
            room.Current.ReferenceServerTimeUnixMs = now;

            room.Version++;
            room.PendingCommand = new PendingRoomCommandV2
            {
                Type = "play",
                ExecuteAtUnixMs = now + PlayLeadTimeMs,
                PositionSeconds = sanitizedStartTime,
                IsPlayingAfterExecution = true,
                Version = room.Version,
            };

            return CreateState(room, now);
        }
    }

    public RoomStateV2 Pause(string roomId)
    {
        var room = GetRoomOrThrow(roomId);
        lock (room.SyncRoot)
        {
            var now = GetUnixTimeMilliseconds();
            PromotePendingCommandIfDue(room, now);

            room.PendingCommand = null;
            room.Current.PositionSeconds = GetCurrentPosition(room.Current, now);
            room.Current.IsPlaying = false;
            room.Current.ReferenceServerTimeUnixMs = now;
            room.Version++;

            return CreateState(room, now);
        }
    }

    public RoomStateV2 Seek(string roomId, double time)
    {
        var room = GetRoomOrThrow(roomId);
        lock (room.SyncRoot)
        {
            var now = GetUnixTimeMilliseconds();
            PromotePendingCommandIfDue(room, now);
            var sanitizedTime = SanitizeTime(time);

            if (room.Current.IsPlaying)
            {
                room.Current.PositionSeconds = GetCurrentPosition(room.Current, now);
                room.Current.ReferenceServerTimeUnixMs = now;

                room.Version++;
                room.PendingCommand = new PendingRoomCommandV2
                {
                    Type = "seek",
                    ExecuteAtUnixMs = now + SeekLeadTimeMs,
                    PositionSeconds = sanitizedTime,
                    IsPlayingAfterExecution = true,
                    Version = room.Version,
                };

                return CreateState(room, now);
            }

            room.PendingCommand = null;
            room.Current.PositionSeconds = sanitizedTime;
            room.Current.ReferenceServerTimeUnixMs = now;
            room.Version++;

            return CreateState(room, now);
        }
    }

    private RoomV2 GetRoomOrThrow(string roomId)
    {
        if (_rooms.TryGetValue(roomId, out var room))
        {
            return room;
        }

        throw new KeyNotFoundException($"Room '{roomId}' was not found.");
    }

    private static RoomStateV2 CreateState(RoomV2 room, long now)
    {
        return new RoomStateV2
        {
            RoomId = room.Id,
            TrackUrl = room.TrackUrl,
            IsPlaying = room.Current.IsPlaying,
            PositionSeconds = Math.Round(room.Current.PositionSeconds, 3),
            ReferenceServerTimeUnixMs = room.Current.ReferenceServerTimeUnixMs,
            ServerNowUnixMs = now,
            Version = room.Version,
            PendingCommand = room.PendingCommand is null
                ? null
                : new PendingRoomCommandStateV2
                {
                    Type = room.PendingCommand.Type,
                    ExecuteAtUnixMs = room.PendingCommand.ExecuteAtUnixMs,
                    PositionSeconds = Math.Round(room.PendingCommand.PositionSeconds, 3),
                    IsPlayingAfterExecution = room.PendingCommand.IsPlayingAfterExecution,
                    Version = room.PendingCommand.Version,
                },
        };
    }

    private static void PromotePendingCommandIfDue(RoomV2 room, long now)
    {
        if (room.PendingCommand is null || now < room.PendingCommand.ExecuteAtUnixMs)
        {
            return;
        }

        room.Current.IsPlaying = room.PendingCommand.IsPlayingAfterExecution;
        room.Current.PositionSeconds = room.PendingCommand.PositionSeconds;
        room.Current.ReferenceServerTimeUnixMs = room.PendingCommand.ExecuteAtUnixMs;
        room.PendingCommand = null;
    }

    private static double GetCurrentPosition(PlaybackSnapshotV2 snapshot, long now)
    {
        if (!snapshot.IsPlaying)
        {
            return snapshot.PositionSeconds;
        }

        var elapsedSeconds = Math.Max(0, (now - snapshot.ReferenceServerTimeUnixMs) / 1000d);
        return Math.Max(0, snapshot.PositionSeconds + elapsedSeconds);
    }

    private static double SanitizeTime(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return 0;
        }

        return Math.Max(0, value);
    }

    private static long GetUnixTimeMilliseconds() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}
