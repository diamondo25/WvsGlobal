using System;
using WvsBeta.Common;
using WvsBeta.Common.Sessions;
using WvsBeta.Common.Tracking;

namespace WvsBeta.Game
{
    public static class CashPacket
    {
        // Thank you, Bui :D
        public enum RockModes
        {
            Delete = 0x02,
            Add = 0x03
        };

        public enum RockErrors
        {
            CannotGo2 = 0x05, // This is unused
            DifficultToLocate = 0x06,
            DifficultToLocate2 = 0x07, // This is unused
            CannotGo = 0x08,
            AlreadyThere = 0x09,
            CannotSaveMap = 0x0A
        };

        public static void HandleTeleRockFunction(Character chr, Packet packet)
        {
            bool AddCurrentMap = packet.ReadBool();
            if (AddCurrentMap)
            {
                if (chr.Inventory.AddRockLocation(chr.MapID))
                {
                    SendRockUpdate(chr, RockModes.Add);
                }
                else
                {
                    SendRockError(chr, RockErrors.CannotSaveMap);
                }
            }
            else
            {
                int map = packet.ReadInt();
                chr.Inventory.RemoveRockLocation(map);
                SendRockUpdate(chr, RockModes.Delete);
            }
        }

        public static void HandleCashItem(Character chr, Packet packet)
        {
            short slot = packet.ReadShort();
            int itemid = packet.ReadInt();

            BaseItem item = chr.Inventory.GetItem(2, slot);

            if (chr.AssertForHack(item == null, "HandleCashItem with null item") ||
                chr.AssertForHack(item.ItemID != itemid, "HandleCashItem with itemid inconsistency") ||
                chr.AssertForHack(!DataProvider.Items.TryGetValue(itemid, out var data), "HandleCashItem with unknown item") ||
                chr.AssertForHack(!data.Cash, "HandleCashItem with non-cash item"))
            {
                return;
            }

            var itemType = (Constants.Items.Types.ItemTypes)Constants.getItemType(itemid);

            bool used = false;

            switch (itemType)
            {
                case Constants.Items.Types.ItemTypes.ItemWeather:
                    used = chr.Field.MakeWeatherEffect(itemid, packet.ReadString(), new TimeSpan(0, 0, 30));
                    break;
                case Constants.Items.Types.ItemTypes.ItemJukebox:
                    used = chr.Field.MakeJukeboxEffect(itemid, chr.Name, packet.ReadInt());
                    break;

                case Constants.Items.Types.ItemTypes.ItemPetTag:
                    {
                        var name = packet.ReadString();
                        var petItem = chr.GetSpawnedPet();
                        if (petItem != null &&
                            !chr.IsInvalidTextInput("Pet name tag", name, Constants.MaxPetName, Constants.MinPetName))
                        {
                            petItem.Name = name;
                            PetsPacket.SendPetNamechange(chr, petItem.Name);
                            used = true;
                        }
                    }

                    break;

                case Constants.Items.Types.ItemTypes.ItemMegaPhone:
                    {
                        var text = packet.ReadString();
                        if (!chr.IsInvalidTextInput("Megaphone item", text, Constants.MaxSpeakerTextLength))
                        {
                            switch (itemid)
                            {
                                case 2081000: // Super Megaphone (channel)
                                    MessagePacket.SendMegaphoneMessage(chr.Name + " : " + text);
                                    used = true;
                                    break;

                                case 2082000: // Super Megaphone
                                    Server.Instance.CenterConnection.PlayerSuperMegaphone(
                                        chr.Name + " : " + text,
                                        packet.ReadBool()
                                    );
                                    used = true;
                                    break;
                            }
                        }
                    }
                    break;

                case Constants.Items.Types.ItemTypes.ItemKite:
                    if (chr.Field.Kites.Count > 0)
                    {
                        //Todo : check for character positions..?
                        MapPacket.KiteMessage(chr);
                    }
                    else
                    {
                        string message = packet.ReadString();
                        Kite pKite = new Kite(chr, chr.ID, itemid, message, chr.Field);

                        used = true;
                    }
                    break;

                case Constants.Items.Types.ItemTypes.ItemMesoSack:
                    if (data.Mesos > 0)
                    {
                        int amountGot = chr.AddMesos(data.Mesos);

                        MiscPacket.SendGotMesosFromLucksack(chr, amountGot);
                        used = true;
                    }
                    break;
                case Constants.Items.Types.ItemTypes.ItemTeleportRock:
                    {
                        byte mode = packet.ReadByte();
                        int map = -1;
                        if (mode == 1)
                        {
                            string name = packet.ReadString();
                            Character target = Server.Instance.GetCharacter(name);
                            if (target != null && target != chr)
                            {
                                map = target.MapID;
                                used = true;
                            }
                            else
                            {
                                SendRockError(chr, RockErrors.DifficultToLocate);
                            }
                        }
                        else
                        {
                            map = packet.ReadInt();
                            if (!chr.Inventory.HasRockLocation(map))
                            {
                                map = -1;
                            }
                        }

                        if (map != -1)
                        {
                            //I don't think it's even possible for you to be in a map that doesn't exist and use a Teleport rock?
                            Map from = chr.Field;
                            Map to = DataProvider.Maps.ContainsKey(map) ? DataProvider.Maps[map] : null;

                            if (to == from)
                            {
                                SendRockError(chr, RockErrors.AlreadyThere);
                            }
                            else if (from.Limitations.HasFlag(FieldLimit.TeleportItemLimit))
                            {
                                SendRockError(chr, RockErrors.CannotGo);
                            }
                            else if (chr.AssertForHack(chr.PrimaryStats.Level < 7, "Using telerock while not lvl 8 or higher."))
                            {
                                // Hacks.
                            }
                            else
                            {
                                chr.ChangeMap(map);
                                used = true;
                            }
                        }

                        break;
                    }
                default:
                    Program.MainForm.LogAppend("Unknown cashitem used: {0} {1} {2}", itemType, itemid, packet.ToString());
                    break;
            }

            if (used)
            {
                ItemTransfer.ItemUsed(chr.ID, item.ItemID, 1, "");
                chr.Inventory.TakeItem(item.ItemID, 1);
            }
            else
            {
                InventoryPacket.NoChange(chr);
            }
        }

        public static void SendRockError(Character chr, RockErrors code)
        {
            Packet pw = new Packet(ServerMessages.SHOW_STATUS_INFO);
            pw.WriteByte((byte)code);
            chr.SendPacket(pw);
        }

        public static void SendRockUpdate(Character chr, RockModes mode)
        {
            Packet pw = new Packet(ServerMessages.SHOW_STATUS_INFO);
            pw.WriteByte((byte)mode);
            chr.Inventory.AddRockPacket(pw);
            chr.SendPacket(pw);
        }
    }
}