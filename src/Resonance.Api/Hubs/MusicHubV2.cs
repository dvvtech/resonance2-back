using Microsoft.AspNetCore.SignalR;
using Resonance.Api.Models;
using Resonance.Api.Services;

namespace Resonance.Api.Hubs;

public sealed class MusicHubV2(RoomSyncServiceV2 roomService) : Hub
{
    public Task<string> CreateRoom()
    {
        var roomId = roomService.CreateRoom(Context.ConnectionId);
        return Task.FromResult(roomId);
    }

    public async Task JoinRoom(string roomId)
    {
        EnsureRoomExists(roomId);
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

        var state = roomService.GetRoomState(roomId);
        await Clients.Caller.SendAsync("RoomUpdated", state);
    }

    public Task<RoomStateV2> GetRoomState(string roomId)
    {
        EnsureRoomExists(roomId);
        return Task.FromResult(roomService.GetRoomState(roomId));
    }

    public Task<ClockSyncResponseV2> SyncClock(long clientSendUnixMs)
    {
        var receiveTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var response = new ClockSyncResponseV2
        {
            ClientSendUnixMs = clientSendUnixMs,
            ServerReceiveUnixMs = receiveTime,
            ServerSendUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        };

        return Task.FromResult(response);
    }

    public async Task SetTrack(string roomId, string url)
    {
        EnsureRoomExists(roomId);

        if (string.IsNullOrWhiteSpace(url))
        {
            throw new HubException("Track URL is required.");
        }

        var state = roomService.SetTrack(roomId, url);
        await Clients.Group(roomId).SendAsync("StateChanged", state);
    }

    public async Task Play(string roomId, double startTime)
    {
        EnsureRoomExists(roomId);

        var currentState = roomService.GetRoomState(roomId);
        if (string.IsNullOrWhiteSpace(currentState.TrackUrl))
        {
            throw new HubException("Track URL is required before playback starts.");
        }

        var state = roomService.Play(roomId, startTime);
        await Clients.Group(roomId).SendAsync("StateChanged", state);
    }

    public async Task Pause(string roomId)
    {
        EnsureRoomExists(roomId);
        var state = roomService.Pause(roomId);
        await Clients.Group(roomId).SendAsync("StateChanged", state);
    }

    public async Task Seek(string roomId, double time)
    {
        EnsureRoomExists(roomId);
        var state = roomService.Seek(roomId, time);
        await Clients.Group(roomId).SendAsync("StateChanged", state);
    }

    private void EnsureRoomExists(string roomId)
    {
        if (string.IsNullOrWhiteSpace(roomId) || !roomService.RoomExists(roomId))
        {
            throw new HubException("Room not found.");
        }
    }
}
