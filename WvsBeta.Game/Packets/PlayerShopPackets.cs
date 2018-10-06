using System.Collections.Generic;
using WvsBeta.Common.Sessions;

namespace WvsBeta.Game
{
    public static class PlayerShopPackets
    {
        public static void OpenPlayerShop(Character pOwner, MiniRoomBase mrb)
        {
            Packet pw = new Packet(ServerMessages.MINI_ROOM_BASE);
            pw.WriteByte(5);
            pw.WriteByte((byte)mrb.Type);
            pw.WriteByte(mrb.MaxUsers);
            pw.WriteBool(mrb.Users[0] == pOwner ? false : true); //owner 
            for (byte i = 0; i < 4; i++)
            {
                Character pUser = mrb.Users[i];
                if (pUser != null)
                {
                    pw.WriteByte(i);
                    PacketHelper.AddAvatar(pw, pUser);
                    pw.WriteString(pUser.Name);
                }
            }
            pw.WriteByte(0xFF);
            pw.WriteString(mrb.Title);
            pw.WriteByte(0x10);
            pw.WriteByte(0);
            pOwner.SendPacket(pw);
        }

        public static void AddPlayer(Character pCharacter, Character pTo)
        {
            Packet pw = new Packet(ServerMessages.MINI_ROOM_BASE);
            pw.WriteByte(4);
            pw.WriteByte(pCharacter.RoomSlotId);
            PacketHelper.AddAvatar(pw, pCharacter);
            pw.WriteString(pCharacter.Name);
            pTo.SendPacket(pw);
        }

        public static void RemovePlayer(Character pCharacter, MiniRoomBase mrb)
        {
            Packet pw = new Packet(ServerMessages.MINI_ROOM_BASE);
            pw.WriteByte(10);
            pw.WriteByte(pCharacter.RoomSlotId);
            mrb.BroadcastPacket(pw, pCharacter);
        }

        public static void CloseShop(Character pCharacter, byte error)
        {
            //2 : The shop is closed. 
            Packet pw = new Packet(ServerMessages.MINI_ROOM_BASE);
            pw.WriteByte(10);
            pw.WriteByte(pCharacter.RoomSlotId);
            pw.WriteByte(error);
            pCharacter.SendPacket(pw);
        }

        public static void PersonalShopRefresh(Character pCharacter, PlayerShop ps)
        {

            Packet pw = new Packet(ServerMessages.MINI_ROOM_BASE);
            pw.WriteByte(0x15);
            pw.WriteByte((byte)ps.Items.Count);
            foreach (KeyValuePair<byte, PlayerShopItem> pst in ps.Items)
            {
                pw.WriteShort(pst.Value.Bundles);
                pw.WriteShort(pst.Value.BundleAmount);
                pw.WriteInt(pst.Value.Price);
                pw.WriteByte(WvsBeta.Common.Constants.getItemTypeInPacket(pst.Value.sItem.ItemID));
                PacketHelper.AddItemData(pw, pst.Value.sItem, 0, false);
            }
            ps.BroadcastPacket(pw);
        }

        public static void OnItemResult(Character pCharacter, byte msg)
        {
            //1 : You do not have enough in stock. 
            //2 : You have not enough mesos  o.o
            //3 : The price of the item is too high for the trade
            //4 i cant even read this one lol, something about not possessing enough mesos
            //5 : Please check and see if your inventory is full or not.
            Packet pw = new Packet(ServerMessages.MINI_ROOM_BASE);
            pw.WriteByte(0x14);
            pw.WriteByte(msg);
            pCharacter.SendPacket(pw);
        }

        public static void MoveItemToInventory(Character pCharacter, byte amount, short slot2)
        {
            Packet pw = new Packet(ServerMessages.MINI_ROOM_BASE);
            pw.WriteByte(0x17);
            pw.WriteByte(amount);
            pw.WriteShort(slot2);
            pCharacter.SendPacket(pw);
        }

        public static void SoldItemResult(Character pCharacter, Character pBuyer, byte slot, short amount)
        {
            //Shows the information on who has bought an item.
            Packet pw = new Packet(ServerMessages.MINI_ROOM_BASE);
            pw.WriteByte(0x16);
            pw.WriteByte(slot); //Slot in shop 
            pw.WriteShort(amount); //Number of purchases
            pw.WriteString(pBuyer.Name);
            pCharacter.SendPacket(pw);
        }
    }
}
