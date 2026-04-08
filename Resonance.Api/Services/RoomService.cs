using System.Collections.Concurrent;
using System.Security.Cryptography;
using Resonance.Api.Models;

namespace Resonance.Api.Services;

public sealed class RoomService
{
    private readonly ConcurrentDictionary<string, Room> _rooms = new(StringComparer.OrdinalIgnoreCase);

    public string CreateRoom(string hostConnectionId)
    {
        while (true)
        {
            var roomId = Convert.ToHexString(RandomNumberGenerator.GetBytes(3));
            var now = DateTime.UtcNow;
            var room = new Room
            {
                Id = roomId,
                HostConnectionId = hostConnectionId,
                TrackUrl = string.Empty,
                IsPlaying = false,
                CurrentTime = 0,
                LastUpdateUtc = now,
                StartTimestampUtc = now,
            };

            if (_rooms.TryAdd(roomId, room))
            {
                return roomId;
            }
        }
    }

    public bool RoomExists(string roomId) => _rooms.ContainsKey(roomId);

    public RoomState GetRoomState(string roomId)
    {
        var room = GetRoomOrThrow(roomId);
        lock (room.SyncRoot)
        {
            return CreateState(room, DateTime.UtcNow);
        }
    }

    public RoomState SetTrack(string roomId, string url)
    {
        var room = GetRoomOrThrow(roomId);
        lock (room.SyncRoot)
        {
            var now = DateTime.UtcNow;
            room.TrackUrl = url.Trim();
            room.IsPlaying = false;
            room.CurrentTime = 0;
            room.LastUpdateUtc = now;
            room.StartTimestampUtc = now;

            return CreateState(room, now);
        }
    }

    public RoomState Play(string roomId, double startTime)
    {
        var room = GetRoomOrThrow(roomId);
        lock (room.SyncRoot)
        {
            var now = DateTime.UtcNow;
            room.IsPlaying = true;
            room.CurrentTime = SanitizeTime(startTime);
            room.StartTimestampUtc = now;
            room.LastUpdateUtc = now;

            return CreateState(room, now);
        }
    }

    public RoomState Pause(string roomId)
    {
        var room = GetRoomOrThrow(roomId);
        lock (room.SyncRoot)
        {
            var now = DateTime.UtcNow;
            room.CurrentTime = GetPlaybackTime(room, now);
            room.IsPlaying = false;
            room.LastUpdateUtc = now;

            return CreateState(room, now);
        }
    }

    public RoomState Seek(string roomId, double time)
    {
        var room = GetRoomOrThrow(roomId);
        lock (room.SyncRoot)
        {
            var now = DateTime.UtcNow;
            room.CurrentTime = SanitizeTime(time);
            room.LastUpdateUtc = now;

            if (room.IsPlaying)
            {
                room.StartTimestampUtc = now;
            }

            return CreateState(room, now);
        }
    }

    private Room GetRoomOrThrow(string roomId)
    {
        if (_rooms.TryGetValue(roomId, out var room))
        {
            return room;
        }

        throw new KeyNotFoundException($"Room '{roomId}' was not found.");
    }

    private static RoomState CreateState(Room room, DateTime now)
    {
        return new RoomState
        {
            RoomId = room.Id,
            TrackUrl = room.TrackUrl,
            IsPlaying = room.IsPlaying,
            CurrentTime = Math.Round(GetPlaybackTime(room, now), 3),
            ServerTimeUtc = now,
            LastUpdateUtc = room.LastUpdateUtc,
        };
    }

    private static double GetPlaybackTime(Room room, DateTime now)
    {
        if (!room.IsPlaying)
        {
            return room.CurrentTime;
        }

        return Math.Max(0, room.CurrentTime + (now - room.StartTimestampUtc).TotalSeconds);
    }

    private static double SanitizeTime(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return 0;
        }

        return Math.Max(0, value);
    }
}
