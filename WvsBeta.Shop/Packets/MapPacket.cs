using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WvsBeta.Common;
using WvsBeta.Common.Sessions;
using WvsBeta.SharedDataProvider;

namespace WvsBeta.Shop
{

    public static class MapPacket
    {
        public static void SendJoinCashServer(Character chr)
        {
            Packet pack = new Packet(ServerMessages.SET_CASH_SHOP);
            var flags = (
                CharacterDataFlag.Stats |
                CharacterDataFlag.Money |
                CharacterDataFlag.Equips |
                CharacterDataFlag.Consume |
                CharacterDataFlag.Install |
                CharacterDataFlag.Etc |
                CharacterDataFlag.Pet |
                CharacterDataFlag.Skills);
            pack.WriteShort((short)flags);

            if (flags.HasFlag(CharacterDataFlag.Stats))
            {
                chr.CharacterStat.Encode(pack);

                pack.WriteByte(20); // Buddylist slots
            }
            // Note: Money is in InventoryPacket

            chr.Inventory.GenerateInventoryPacket(pack, flags);

            if (flags.HasFlag(CharacterDataFlag.Skills))
            {
                pack.WriteShort((short)chr.Skills.Count);

                foreach (var skillId in chr.Skills)
                {
                    pack.WriteInt(skillId);
                    pack.WriteInt(1);
                }
            }


            // No quests, etc

            pack.WriteBool(true);
            pack.WriteString(chr.UserName);

            // If you want to show all items, write 1 not sold SN.
            // The rest will pop up

            var itemsNotOnSale = DataProvider.Commodity.Where(x => x.Value.OnSale == false).Select(x => x.Key).ToList();

            pack.WriteShort(0);
            //pack.WriteShort((short)itemsNotOnSale.Count);
            //itemsNotOnSale.ForEach(pack.WriteInt);
            
            // Client does not have modified commodity support...

            // Newer versions will have discount-per-category stuff here
            // byte amount, foreach { byte category, byte categorySub, byte discountRate  }

            
            // BEST

            // Categories
            for (byte i = 1; i <= 8; i++)
            {
                // Gender (0 = male, 1 = female)
                for (byte j = 0; j <= 1; j++)
                {
                    // Top 5 items
                    for (byte k = 0; k < 5; k++)
                    {
                        pack.WriteInt(i);
                        pack.WriteInt(j);

                        if (Server.Instance.BestItems.TryGetValue((i, j, k), out var sn))
                        {
                            pack.WriteInt(sn);
                        }
                        else
                        {
                            pack.WriteInt(0);
                        }
                    }
                }
            }

            // -1 == available, 2 is not available, 1 = default?

            var customStockState = DataProvider.Commodity.Values.Where(x => x.StockState != StockState.DefaultState).ToList();

            pack.WriteUShort((ushort)customStockState.Count);
            customStockState.ForEach(x =>
            {
                pack.WriteInt(x.SerialNumber);
                pack.WriteInt((int)x.StockState);
            });

            pack.WriteLong(0);
            pack.WriteLong(0);
            pack.WriteLong(0);
            pack.WriteLong(0);
            pack.WriteLong(0);
            pack.WriteLong(0);
            pack.WriteLong(0);
            pack.WriteLong(0);
            pack.WriteLong(0);

            chr.SendPacket(pack);
        }
    }
}
