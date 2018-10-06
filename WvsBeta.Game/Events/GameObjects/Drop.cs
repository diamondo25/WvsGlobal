using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WvsBeta.Common;

namespace WvsBeta.Game
{
    public enum DropType
    {
        Normal = 0,
        Party = 1,
        FreeForAll = 2,
        Explosive = 3
    }

    public class Drop
    {
        public byte Type { get; set; }
        public short QuestID { get; set; }
        public int Owner { get; set; }
        public int Time { get; set; }
        public int MapID { get; set; }
        public int ID { get; set; }
        public int Mesos { get; set; }
        public int PlayerID { get; set; }
        public bool PlayerDrop { get; set; }
        public bool Tradable { get; set; }
        public Pos Position { get; set; }
        public Item ItemData { get; set; }
        public DateTime Droptime { get; set; }
        public int DropperID { get; set; }

        public Drop(int mapid, int mesos, Pos position, int owner, bool playerdrop = false, int dropperid = 0)
        {
            QuestID = 0;
            Owner = owner;
            MapID = mapid;
            ItemData = null;
            Mesos = mesos;
            PlayerID = 0;
            PlayerDrop = playerdrop;
            Type = (byte)DropType.Normal;
            Tradable = true;
            Position = position;
            Droptime = DateTime.Now;
            DropperID = dropperid;

            DataProvider.Maps[MapID].AddDrop(this);
        }


        public Drop(int mapid, Item item, Pos position, int owner, bool playerdrop = false, int dropperid = 0)
        {
            QuestID = 0;
            Owner = owner;
            MapID = mapid;
            ItemData = item;
            PlayerID = 0;
            PlayerDrop = playerdrop;
            Type = (byte)DropType.Normal;
            Tradable = true;
            Position = position;
            Mesos = 0;
            Droptime = DateTime.Now;
            DropperID = dropperid;

            DataProvider.Maps[MapID].AddDrop(this);
        }

        public int GetObjectID()
        {
            return (Mesos > 0 ? Mesos : ItemData.ItemID);
        }

        public short GetAmount()
        {
            return (short)(Mesos > 0 ? 0 : ItemData.Amount);
        }

        public bool IsMesos()
        {
            return Mesos > 0;
        }

        public void RemoveDrop(bool showPacket)
        {
            if (showPacket)
            {
                DropPacket.RemoveDrop(this);
            }
            DataProvider.Maps[MapID].RemoveDrop(this);
        }

        public void TakeDrop(Character chr, bool petPickup)
        {
            DropPacket.TakeDrop(chr, this, petPickup);
            DataProvider.Maps[MapID].RemoveDrop(this);
        }

        public void TakeDropMob(int mobid)
        {
            DropPacket.MobLootDrop(this, mobid);
            DataProvider.Maps[MapID].RemoveDrop(this);
        }

        public void ShowDrop(Character chr)
        {
            if (QuestID != 0 && chr.ID != PlayerID)
            {
                return;
            }
            DropPacket.ShowDrop(chr, this, (byte)DropPacket.DropTypes.ShowExisting, false, new Pos());
        }

        public void DoDrop(Pos Origin)
        {
            Time = (int)DateTime.Now.Ticks;
            if (QuestID == 0)
            {
                if (!Tradable)
                {
                    DropPacket.ShowDrop(null, this, (byte)DropPacket.DropTypes.DisappearDuringDrop, false, Origin);
                }
                else
                {
                    DropPacket.ShowDrop(null, this, (byte)DropPacket.DropTypes.DropAnimation, true, Origin);
                }
            }
            else
            {
                Character chr = DataProvider.Maps[MapID].GetPlayer(PlayerID);
                if (chr != null)
                {
                    DropPacket.ShowDrop(chr, this, (byte)DropPacket.DropTypes.DropAnimation, true, Origin);
                }
            }
        }
    }
}
