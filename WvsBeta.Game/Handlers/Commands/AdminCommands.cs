using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WvsBeta.Common.Sessions;

namespace WvsBeta.Game.Handlers.Commands
{
    public class AdminCommands : HandlerHelpers
    {
        [CommandHandler(
            CommandName = "map",
            Aliases = new[] { "m", "goto" }, 
            UserRanks = UserAdminLevels.AdminGmIntern | UserAdminLevels.Tespian
        )]
        public static bool HandleMapCommand(Character character,  string initialCommand, CommandHandling.CommandArgs Args)
        {
            if (Args.Count <= 0) return true;

            var FieldID = -1;

            if (!Args[0].IsNumber())
            {
                var MapStr = Args[0];
                var TempID = GetMapidFromName(MapStr);

                if (TempID == -1)
                {
                    switch (MapStr)
                    {
                        case "here":
                            FieldID = character.MapID;
                            break;
                        case "town":
                            FieldID = character.Field.ReturnMap;
                            break;
                    }
                }
                else
                    FieldID = TempID;
            }
            else
                FieldID = Args[0].GetInt32();

            if (DataProvider.Maps.ContainsKey(FieldID))
                character.ChangeMap(FieldID);
            else
                ShowError("Map not found.");
            return true;
        }

        [CommandHandler(
            CommandName = "whereami",
            Aliases = new[] { "whatmap", "pos" },
            UserRanks = UserAdminLevels.AdminGmIntern | UserAdminLevels.Tespian
        )]
        public static bool HandleCommandWhereAmI(Character character, string initialCommand, CommandHandling.CommandArgs Args)
        {
            ShowInfo($"You are on mapid {character.MapID}, X {character.Position.X}, Y {character.Position.Y}, FH {character.Foothold}");
            return true;
        }

        [CommandHandler(
            CommandName = "chase",
            Aliases = new[] {"c", "warpto"},
            UserRanks = UserAdminLevels.Tespian | UserAdminLevels.AdminGmIntern
        )]
        public static bool HandleCommandChase(Character character, string initialCommand,
            CommandHandling.CommandArgs Args)
        {
            if (Args.Count > 0)
            {
                string other = Args[0].Value.ToLower();
                var otherChar = Server.Instance.GetCharacter(other);
                if (otherChar != null)
                {
                    if (character.MapID != otherChar.MapID)
                    {
                        character.ChangeMap(otherChar.MapID);
                    }

                    var p = new Packet(0xC1);
                    p.WriteShort(otherChar.Position.X);
                    p.WriteShort(otherChar.Position.Y);
                    character.SendPacket(p);
                    return true;
                }

                MessagePacket.SendText(MessagePacket.MessageTypes.RedText, "Victim not found.",
                    character, MessagePacket.MessageMode.ToPlayer);
            }
            return true;
        }

        [CommandHandler(
            CommandName = "chasehere",
            Aliases = new[] { "warphere" },
            UserRanks = UserAdminLevels.Tespian | UserAdminLevels.AdminGmIntern
        )]
        public static bool HandleCommandChaseHere(Character character, string initialCommand,
            CommandHandling.CommandArgs Args)
        {
            if (Args.Count > 0)
            {
                string other = Args[0].Value.ToLower();
                var otherChar = Server.Instance.GetCharacter(other);
                if (otherChar != null)
                {
                    if (character.MapID != otherChar.MapID)
                    {
                        otherChar.ChangeMap(character.MapID);
                    }

                    var p = new Packet(0xC1);
                    p.WriteShort(character.Position.X);
                    p.WriteShort(character.Position.Y);
                    otherChar.SendPacket(p);
                    return true;
                }

                MessagePacket.SendText(MessagePacket.MessageTypes.RedText, "Victim not found.",
                    character, MessagePacket.MessageMode.ToPlayer);
            }
            return true;
        }
    }
}
