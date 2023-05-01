using AssettoServer.Commands;
using AssettoServer.Commands.Modules;
using AssettoServer.Network.Packets.Shared;
using AssettoServer.Network.Tcp;
using AssettoServer.Server;
using Qmmands;

namespace CatMouseRacePlugin;

[RequireConnectedPlayer]
public class CatMouseCommandModule : ACModuleBase
{
    private readonly CatMouseChallengePlugin _plugin;
    private readonly SessionManager _sessionManager;

    public CatMouseCommandModule(CatMouseChallengePlugin plugin, SessionManager sessionManager)
    {
        _plugin = plugin;
        _sessionManager = sessionManager;
    }

    [Command("race")]
    public void Race(ACTcpClient player)
        => _plugin.GetRace(Context.Client!.EntryCar).ChallengeCar(player.EntryCar);

    [Command("accept")]
    public async ValueTask AcceptRaceAsync()
    {
        var currentRace = _plugin.GetRace(Context.Client!.EntryCar).CurrentRace;
        if (currentRace == null)
            Reply("You do not have a pending race request.");
        else if (currentRace.HasStarted)
            Reply("This race has already started.");
        else
            await currentRace.StartAsync();
    }
}
