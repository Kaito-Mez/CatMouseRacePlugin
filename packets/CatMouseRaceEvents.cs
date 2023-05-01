using AssettoServer.Network.Packets;
using AssettoServer.Network.Packets.Outgoing;
using Serilog;
using System.Numerics;

namespace CatMouseRacePlugin.Packets;

public class CatMouseRaceEvents : IOutgoingNetworkPacket
{
    public int Index { get; set; }
    public string? Message { get; set; }
    public Vector3 Destination { get; set; }
    public Vector3 Rotation { get; set; }
    public int Locked { get; set; }
    public void ToWriter(ref PacketWriter writer)
    {
        writer.Write((byte)ACServerProtocol.Extended);
        writer.Write((byte)CSPMessageTypeTcp.ClientMessage);
        writer.Write<byte>(255);
        writer.Write<ushort>(60000);
        writer.Write(0xA8505268);
        //writer.WriteStringFixed(Message!, System.Text.Encoding.ASCII, 20);
        writer.Write(Rotation);
        writer.Write(Index);
        writer.Write(Locked);
    }
}
