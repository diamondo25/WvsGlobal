using System;
using System.Linq;
using WvsBeta.Common;
using WvsBeta.Common.Sessions;
using WvsBeta.Common.Tracking;

namespace WvsBeta.Game
{
    public static class DropPacket
    {
        public static void HandleDropMesos(Character chr, int amount)
        {

            //30 E8 03 00 00 
            if (chr.AssertForHack(amount < 10, "Trying to drop less than 10 mesos") || 
                chr.AssertForHack(amount > 50000, "Trying to drop more than 50k mesos") || 
                chr.AssertForHack(amount > chr.Inventory.Mesos, "Trying to drop more mesos than he's got") || 
                chr.AssertForHack(chr.Room != null, "Trying to drop mesos while in a 'room'"))
            {
                InventoryPacket.NoChange(chr);
                return;
            }

            if (chr.IsGM && !chr.IsAdmin)
            {
                MessagePacket.SendNotice("You cannot drop mesos.", chr);
                InventoryPacket.NoChange(chr);
                return;
            }

            chr.AddMesos(-amount, true);
            Common.Tracking.MesosTransfer.PlayerDropMesos(chr.ID, amount, chr.MapID.ToString());

            chr.Field.DropPool.Create(Reward.Create(amount), chr.ID, 0, DropType.Normal, chr.ID, new Pos(chr.Position), chr.Position.X, 0, false, 0, false, true);
            // This shouldn't be required
            InventoryPacket.NoChange(chr);
        }

        public static void HandlePickupDrop(Character chr, Packet packet)
        {
            // 5F 18 FF 12 01 00 00 00 00 
            packet.Skip(4); // pos?

            int dropid = packet.ReadInt();
            if (chr.AssertForHack(chr.Room != null, "Trying to loot a drop while in a 'room'") ||
                !chr.Field.DropPool.Drops.TryGetValue(dropid, out Drop drop) ||
                !drop.CanTakeDrop(chr))
            {
                InventoryPacket.NoChange(chr);
                return;
            }

            var dropLootRange = drop.Pt2 - chr.Position;

            chr.AssertForHack(dropLootRange > 200, "Possible drop VAC! Distance: " + dropLootRange, dropLootRange > 250);

            bool SentDropNotice = false;
            Reward reward = drop.Reward;
            int dropNoticeItemIdOrMesos = reward.Drop;
            short pickupAmount = reward.Amount;
            if (reward.Mesos)
            {
                // Party meso distribution
                if (drop.SourceID != 0 &&
                    chr.PartyID != 0 &&
                    drop.OwnPartyID == chr.PartyID)
                {
                    var PartyData = chr.Field.GetInParty(chr.PartyID);
                    var Count = PartyData.Count();

                    if (Count > 1)
                    {
                        SentDropNotice = true;
                        var Base = drop.Reward.Drop * 0.8 / Count + 0.5;
                        Base = Math.Floor(Base);
                        if (Base <= 0.0) Base = 0.0;

                        var Bonus = Convert.ToInt32(drop.Reward.Drop - Count * Base);
                        if (Bonus < 0) Bonus = 0;

                        drop.Reward.Drop = Convert.ToInt32(Base);

                        foreach (var BonusUser in PartyData)
                        {
                            int mesosGiven = reward.Drop;
                            if (chr.ID == BonusUser.ID)
                            {
                                mesosGiven += Bonus;
                            }
                            // Now figure out what we really gave the user
                            mesosGiven = BonusUser.AddMesos(mesosGiven, true);

                            Common.Tracking.MesosTransfer.PlayerLootMesos(drop.SourceID, chr.ID, mesosGiven, "Party " + chr.PartyID + ", " + chr.MapID + ", " + drop.GetHashCode());

                            CharacterStatsPacket.SendGainDrop(BonusUser, true, mesosGiven, 0);
                        }
                    }
                }

                if (!SentDropNotice)
                {
                    dropNoticeItemIdOrMesos = chr.AddMesos(reward.Drop, true);
                    Common.Tracking.MesosTransfer.PlayerLootMesos(
                        drop.SourceID,
                        chr.ID,
                        dropNoticeItemIdOrMesos,
                        chr.MapID + ", " + drop.GetHashCode()
                    );
                }
            }
            else if (Constants.isStar(reward.ItemID))
            {
                if (!chr.Inventory.HasSlotsFreeForItem(reward.ItemID, reward.Amount, Constants.isStackable(reward.ItemID)))
                {
                    CannotLoot(chr, -1);
                    InventoryPacket.NoChange(chr);
                    return;
                }
                var rewardItem = drop.Reward.GetData();
                chr.Inventory.AddItem2(rewardItem);
                ItemTransfer.ItemPickedUp(chr.ID, chr.MapID, reward.ItemID, reward.Amount, chr.MapID + ", " + drop.GetHashCode(), rewardItem);
            }
            else if (chr.Inventory.AddItem2(drop.Reward.GetData()) == drop.Reward.Amount)
            {
                CannotLoot(chr, -1);
                InventoryPacket.NoChange(chr); // ._. stupid nexon
                return;
            }
            else
            {
                if (Constants.isEquip(drop.Reward.ItemID))
                {
                    ItemTransfer.ItemPickedUp(chr.ID, chr.MapID, reward.ItemID, reward.Amount, chr.MapID + ", " + drop.GetHashCode(), drop.Reward.GetData());
                }
            }
            if (!SentDropNotice)
            {
                CharacterStatsPacket.SendGainDrop(chr, reward.Mesos, dropNoticeItemIdOrMesos, pickupAmount);
            }
            chr.Field.DropPool.RemoveDrop(drop, RewardLeaveType.FreeForAll, chr.ID);
        }

        public static void SendMakeEnterFieldPacket(Drop drop, RewardEnterType EnterType, short Delay, Character chr = null)
        {
            Packet pw = new Packet(ServerMessages.DROP_ENTER_FIELD);
            pw.WriteByte((byte)EnterType);
            pw.WriteInt(drop.DropID);
            pw.WriteBool(drop.Reward.Mesos);
            pw.WriteInt(drop.Reward.Drop);
            pw.WriteInt(drop.OwnerID);
            pw.WriteByte((byte)drop.OwnType);
            pw.WriteShort(drop.Pt2.X);
            pw.WriteShort(drop.Pt2.Y);

            if (EnterType == RewardEnterType.ShowDrop ||
                EnterType == RewardEnterType.DropAnimation ||
                EnterType == RewardEnterType.DisappearDuringDrop)
            {
                pw.WriteInt(drop.SourceID);
                pw.WriteShort(drop.Pt1.X);
                pw.WriteShort(drop.Pt1.Y);
                pw.WriteShort(Delay);
            }

            if (!drop.Reward.Mesos)
                pw.WriteLong(drop.Reward.DateExpire);

            pw.WriteBool(!drop.ByPet);

            if (chr != null)
                chr.SendPacket(pw);
            else
                drop.Field.SendPacket(drop, pw);
        }

        public static void SendMakeLeaveFieldPacket(Drop Drop, RewardLeaveType LeaveType, int Option = 0)
        {
            Packet pw = new Packet(ServerMessages.DROP_LEAVE_FIELD);
            pw.WriteByte((byte)LeaveType);
            pw.WriteInt(Drop.DropID);

            if (LeaveType == RewardLeaveType.FreeForAll ||
                LeaveType == RewardLeaveType.Remove ||
                LeaveType == RewardLeaveType.PetPickup)
                pw.WriteInt(Option);
            else if (LeaveType == RewardLeaveType.Explode)
                pw.WriteShort((short)Option);

            Drop.Field.SendPacket(Drop, pw);
        }

        public static void CannotLoot(Character chr, sbyte reason)
        {
            Packet pw = new Packet(ServerMessages.SHOW_STATUS_INFO);
            pw.WriteByte(0);
            pw.WriteSByte(reason);
            chr.SendPacket(pw);
        }
    }
}