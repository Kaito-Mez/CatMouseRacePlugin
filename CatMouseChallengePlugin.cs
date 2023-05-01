using AssettoServer.Server;
using AssettoServer.Server.Configuration;
using AssettoServer.Server.Plugin;
using AssettoServer.Utils;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Reflection;

namespace CatMouseRacePlugin;

public class CatMouseChallengePlugin : CriticalBackgroundService, IAssettoServerAutostart
{
    private readonly EntryCarManager _entryCarManager;
    private readonly Func<EntryCar, EntryCarCatMouse> _entryCarRaceFactory;
    private readonly Dictionary<int, EntryCarCatMouse> _instances = new();
    private readonly ACServerConfiguration _serverConfiguration;

    public CatMouseChallengePlugin(EntryCarManager entryCarManager,
        ACServerConfiguration serverConfiguration,
        CSPServerScriptProvider scriptProvider,
        Func<EntryCar, EntryCarCatMouse> entryCarRaceFactory, 
        IHostApplicationLifetime applicationLifetime) : base(applicationLifetime)
    {

        _entryCarManager = entryCarManager;
        _entryCarRaceFactory = entryCarRaceFactory;
        _serverConfiguration = serverConfiguration;

        if (_serverConfiguration.Extra.EnableClientMessages)
        {
            using var streamReader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("CatMouseRacePlugin.lua.catmouse.lua")!);
            scriptProvider.AddScript(streamReader.ReadToEnd(), "catmouse.lua");
            Log.Information("CATMOUSE.LUA LOADED");
            using var streamReader1 = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("CatMouseRacePlugin.lua.teleport.lua")!);
            scriptProvider.AddScript(streamReader1.ReadToEnd(), "teleport.lua");
            Log.Information("teleport.LUA LOADED");
        }
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var entryCar in _entryCarManager.EntryCars)
        {
            _instances.Add(entryCar.SessionId, _entryCarRaceFactory(entryCar));
        }

        return Task.CompletedTask;
    }
    
    internal EntryCarCatMouse GetRace(EntryCar entryCar) => _instances[entryCar.SessionId];
}
