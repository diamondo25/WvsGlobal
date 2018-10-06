using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WvsBeta.Common.Sessions;
using WvsBeta.Game;

namespace WvsBeta.SharedDataProvider
{
    public abstract class BasePacketHelper
    {

        public static void AddItemData(Packet packet, BaseItem item, short slot, bool shortslot)
        {
            AddItemDataWithAmount(packet, item, slot, shortslot, item.Amount);
        }

        public static void AddItemDataWithAmount(Packet packet, BaseItem item, short slot, bool shortslot, short amount)
        {
            if (slot != 0)
            {
                if (shortslot)
                {
                    packet.WriteShort(slot);
                }
                else
                {
                    slot = Math.Abs(slot);
                    if (slot > 100) slot -= 100;
                    packet.WriteByte((byte)slot);
                }
            }
            item.Encode(packet);
        }

    }
}
