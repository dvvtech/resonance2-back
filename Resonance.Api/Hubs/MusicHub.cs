using Microsoft.AspNetCore.SignalR;
using Resonance.Api.Services;

namespace Resonance.Api.Hubs;

public sealed class MusicHub(RoomService roomService) : Hub
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

    public Task<Resonance.Api.Models.RoomState> GetRoomState(string roomId)
    {
        EnsureRoomExists(roomId);
        return Task.FromResult(roomService.GetRoomState(roomId));
    }

    public async Task SetTrack(string roomId, string url)
    {
        EnsureRoomExists(roomId);

        if (string.IsNullOrWhiteSpace(url))
        {
            throw new HubException("Track URL is required.");
        }

        var state = roomService.SetTrack(roomId, url);
        await Clients.Group(roomId).SendAsync("TrackChanged", state);
    }

    public async Task Play(string roomId, double startTime)
    {
        EnsureRoomExists(roomId);
        var state = roomService.Play(roomId, startTime);
        await Clients.Group(roomId).SendAsync("Play", state);
    }

    public async Task Pause(string roomId)
    {
        EnsureRoomExists(roomId);
        var state = roomService.Pause(roomId);
        await Clients.Group(roomId).SendAsync("Pause", state);
    }

    public async Task Seek(string roomId, double time)
    {
        EnsureRoomExists(roomId);
        var state = roomService.Seek(roomId, time);
        await Clients.Group(roomId).SendAsync("Seek", state);
    }

    private void EnsureRoomExists(string roomId)
    {
        if (string.IsNullOrWhiteSpace(roomId) || !roomService.RoomExists(roomId))
        {
            throw new HubException("Room not found.");
        }
    }
}
