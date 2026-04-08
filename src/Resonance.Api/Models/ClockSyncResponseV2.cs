namespace Resonance.Api.Models;

public sealed class ClockSyncResponseV2
{
    public long ClientSendUnixMs { get; init; }

    public long ServerReceiveUnixMs { get; init; }

    public long ServerSendUnixMs { get; init; }
}
