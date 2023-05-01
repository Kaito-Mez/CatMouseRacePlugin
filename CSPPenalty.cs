using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AssettoServer.Network.Packets.Outgoing;
using AssettoServer.Network.Packets.Shared;

namespace CatMouseRacePlugin
{
    public class CSPPenalty : CSPClientMessageOutgoing
    {
        public int Seconds { get; init; }

        public CSPPenalty()
        {
            Type = CSPClientMessageType.AdminPenalty;
        }

        protected override void ToWriter(BinaryWriter writer)
        {
            writer.Write(Seconds);
        }
    }
}
