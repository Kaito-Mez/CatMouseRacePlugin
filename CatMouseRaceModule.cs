using AssettoServer.Server.Plugin;
using Autofac;

namespace CatMouseRacePlugin;

public class CatMouseRaceModule : AssettoServerModule
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<CatMouseChallengePlugin>().AsSelf().As<IAssettoServerAutostart>().SingleInstance();
        builder.RegisterType<EntryCarCatMouse>().AsSelf();
        builder.RegisterType<CatMouseRace>().AsSelf();
    }
}
