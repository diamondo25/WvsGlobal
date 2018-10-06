using System.Diagnostics;
using WvsBeta.Common;
using WvsBeta.Common.Sessions;
using WvsBeta.Game.Packets;

namespace WvsBeta.Game
{
    public static class PetsPacket
    {
        public static void HandleSpawnPet(Character chr, short slot)
        {
            if (!(chr.Inventory.GetItem(5, slot) is PetItem petItem))
            {
                InventoryPacket.NoChange(chr);
                return;
            }
            

            if (chr.PetCashId != 0)
            {
                // Already spawned a pet
                SendRemovePet(chr);

                if (chr.PetCashId == petItem.CashId)
                {
                    // Spawned the same mob
                    chr.PetCashId = 0;
                    InventoryPacket.NoChange(chr);
                    return;
                }
            }
            
            chr.PetCashId = petItem.CashId;
            DoPetSpawn(chr);
            InventoryPacket.NoChange(chr);
        }

        public static void DoPetSpawn(Character chr)
        {
            chr.PetLastInteraction = MasterThread.CurrentTime;

            var petItem = chr.GetSpawnedPet();
            var ml = petItem.MovableLife;
            ml.Foothold = chr.Foothold;
            ml.Position = new Pos(chr.Position);
            ml.Position.Y -= 20;
            ml.Stance = 0;

            SendSpawnPet(chr, petItem);
        }

        public static void HandleMovePet(Character chr, Packet packet)
        {
            // 48 00 00 00 00 03 00 00 00 D1 00 00 00 9E 02 00 00 06 E0 01 00 00 00 D7 00 00 00 00 00 00 00 06 09 00 00 00 00 D7 00 00 00 00 00 88 00 04 15 00 00 

            var petItem = chr.GetSpawnedPet();
            if (petItem == null) return;

            var movePath = new MovePath();
            movePath.DecodeFromPacket(packet, MovePath.MovementSource.Pet);
            chr.TryTraceMovement(movePath);

            PacketHelper.ValidateMovePath(petItem.MovableLife, movePath);

            SendMovePet(chr, movePath);
        }

        public static void HandleInteraction(Character chr, Packet packet)
        {
            var petItem = chr.GetSpawnedPet();
            if (petItem == null) return;

            bool success = false;
            double multiplier = 1.0;
            // 4A 00 00 
            byte doMultiplier = packet.ReadByte();

            if (doMultiplier != 0 && Pet.IsNamedPet(petItem))
                multiplier = 1.5;

            byte interactionId = packet.ReadByte(); // dunno lol
            
            if (!DataProvider.Pets.TryGetValue(petItem.ItemID, out var petData) || 
                !petData.Reactions.TryGetValue(interactionId, out var petReactionData)) return;

            long timeSinceLastInteraction = MasterThread.CurrentTime - chr.PetLastInteraction;

            // shouldnt be able to do this yet.
            if (petReactionData.LevelMin > petItem.Level ||
                petReactionData.LevelMax < petItem.Level ||
                timeSinceLastInteraction < 15000) goto send_response;

            // sick math

            chr.PetLastInteraction = MasterThread.CurrentTime;
            double additionalSucceedProbability = (((timeSinceLastInteraction - 15000.0) / 10000.0) * 0.01 + 1.0) * multiplier;

            var random = Rand32.Next() % 100;
            if (random >= (petReactionData.Prob * additionalSucceedProbability) ||
                petItem.Fullness < 50) goto send_response;

            success = true;
            Pet.IncreaseCloseness(chr, petItem, petReactionData.Inc);
            Pet.UpdatePet(chr, petItem);

            send_response:
            SendPetInteraction(chr, interactionId, success);
        }

        public static void HandlePetLoot(Character chr, Packet packet)
        {
            // 4B 23 06 D7 00 3A 00 00 00
            /*
            Pet pet = chr.Pets.GetEquippedPet();
            if (pet == null) return;

            packet.Skip(4); // X, Y
            int dropid = packet.ReadInt();
            if (!chr.Field.DropPool.Drops.ContainsKey(dropid)) return;
            Drop drop = chr.Field.DropPool.Drops[dropid];
            if (!drop.Reward.Mesos && !chr.Admin) return;

            short pickupAmount = drop.Reward.Amount;
            if (drop.Reward.Mesos)
            {
                chr.AddMesos(drop.Reward.Drop);
            }
            else
            {
                if (chr.Inventory.AddItem2(drop.Reward.GetData()) == drop.Reward.Amount)
                {
                    DropPacket.CannotLoot(chr, -1);
                    InventoryPacket.NoChange(chr); // ._. stupid nexon
                    return;
                }
                
            }
            CharacterStatsPacket.SendGainDrop(chr, drop.Reward.Mesos, drop.Reward.Drop, pickupAmount);
            chr.Field.DropPool.RemoveDrop(drop, RewardLeaveType.PetPickup, chr.ID);
            */
        }

        public static void HandlePetAction(Character chr, Packet packet)
        {
            var type = packet.ReadByte();
            var action = packet.ReadByte();
            var message = packet.ReadString();

            Trace.WriteLine($"Pet Action {type} {action} {message}");
            
            SendPetChat(chr, type, action, message);

        }

        public static void HandlePetFeed(Character chr, Packet packet)
        {
            // 26 06 00 40 59 20 00 
        }

        public static void SendPetChat(Character chr, byte type, byte action, string text)
        {
            var pw = new Packet(ServerMessages.PET_ACTION);
            pw.WriteInt(chr.ID);
            pw.WriteByte(type);
            pw.WriteByte(action);
            pw.WriteString(text);
            chr.Field.SendPacket(chr, pw);
        }

        public static void SendPetNamechange(Character chr, string name)
        {
            var pw = new Packet(ServerMessages.PET_NAME_CHANGED);
            pw.WriteInt(chr.ID);
            pw.WriteString(name);
            chr.Field.SendPacket(chr, pw);
        }



        public static void SendPetLevelup(Character chr, byte wat = 0)
        {
            var pw = new Packet(ServerMessages.PLAYER_EFFECT);
            pw.WriteByte(0x04);
            pw.WriteByte(wat); // 0 = levelup, 1 = teleport to base, 2 = teleport to your back
            chr.SendPacket(pw);

            pw = new Packet(ServerMessages.SHOW_FOREIGN_EFFECT);
            pw.WriteInt(chr.ID);
            pw.WriteByte(0x04);
            pw.WriteByte(wat);
            chr.Field.SendPacket(chr, pw, chr);
        }

        public static void SendPetAction(Character chr, byte a, byte b)
        {
            var pw = new Packet(ServerMessages.PET_INTERACTION);
            pw.WriteInt(chr.ID);
            pw.WriteByte(a);
            pw.WriteByte(b);
            pw.WriteBool(false);
            pw.WriteLong(0);
            pw.WriteLong(0);
            chr.Field.SendPacket(chr, pw);
        }

        public static void SendPetInteraction(Character chr, byte action, bool inc)
        {
            var pw = new Packet(ServerMessages.PET_INTERACTION);
            pw.WriteInt(chr.ID);
            pw.WriteByte(0);
            pw.WriteByte(action);
            pw.WriteBool(inc);
            pw.WriteLong(0);
            pw.WriteLong(0);
            chr.Field.SendPacket(chr, pw);
        }

        public static void SendMovePet(Character chr, MovePath movePath)
        {
            var pw = new Packet(ServerMessages.PET_MOVE);
            pw.WriteInt(chr.ID);
            movePath.EncodeToPacket(pw);

            chr.Field.SendPacket(chr, pw, chr);
        }

        public static void SendSpawnPet(Character chr, PetItem pet, Character tochar = null)
        {
            // 43 10000000 01 404B4C00 0300312031 3A00000000000000 0000 00 0000  000000000000000000000000000000000000000000000000000000 
            var pw = new Packet(ServerMessages.SPAWN_PET);
            pw.WriteInt(chr.ID);
            pw.WriteBool(true); // Spawns
            pw.WriteInt(pet.ItemID);
            pw.WriteString(pet.Name);
            pw.WriteLong(pet.CashId);
            pw.WriteShort(pet.MovableLife.Position.X);
            pw.WriteShort(pet.MovableLife.Position.Y);
            pw.WriteByte(pet.MovableLife.Stance);
            pw.WriteShort(pet.MovableLife.Foothold);
            pw.WriteLong(0);
            pw.WriteLong(0);
            if (tochar == null)
                chr.Field.SendPacket(chr, pw);
            else
                tochar.SendPacket(pw);
        }

        public static void SendRemovePet(Character chr, bool gmhide = false)
        {
            var pw = new Packet(ServerMessages.SPAWN_PET);
            pw.WriteInt(chr.ID);
            pw.WriteBool(false);
            pw.WriteLong(0);
            pw.WriteLong(0);
            chr.Field.SendPacket(chr, pw, (gmhide ? chr : null));
        }
    }
}