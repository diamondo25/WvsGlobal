using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MySql.Data.MySqlClient;
using WvsBeta.Common;
using WvsBeta.Common.Sessions;
using WvsBeta.Game.Events;
using WvsBeta.Game.Events.GMEvents;
using WvsBeta.Game.Packets;

namespace WvsBeta.Game
{
    public class CommandHandling
    {
        public static Dictionary<string, int> MapNameList { get; } = new Dictionary<string, int>
        {
            // Job maps
            { "gm", 180000000 },
            { "3rd", 211000001 },
            { "mage", 101000003 },
            { "bowman", 100000201 },
            { "thief", 103000003 },
            { "warrior", 102000003 },
            // Miscellaneous
            { "happyville", 209000000 },
            { "cafe", 193000000 },
            // Maple Island
            { "southperry", 60000 },
            { "amherst", 1010000 },
            // Victoria
            { "henesys", 100000000 },
            { "perion", 102000000 },
            { "ellinia", 101000000 },
            { "sleepy", 105040300 },
            { "sleepywood", 105040300 },
            { "lith", 104000000 },
            { "florina", 110000000 },
            { "kerning", 103000000 },
            // Ossyria
            { "orbis", 200000000 },
            { "elnath", 211000000 },
            { "nath", 211000000 },
            // Ludus Lake area
            { "ludi", 220000000 },
            { "omega", 221000000 },
            // Training Areas
            { "hhg1", 104040000 },
            { "kerningconstruct", 103010000 },
            { "westrockymountain1", 102020000 },
            { "pigbeach", 104010001 },
            { "fog", 106010102 },
            { "subwayb1", 103000902 },
            { "subwayb2", 103000905 },
            { "subwayb3", 103000909 },
            // Free Markets
            { "henfm", 100000110 },
            { "perionfm", 102000100 },
            { "elnathfm", 211000110 },
            { "ludifm", 220000200 },
            // Dungeon areas
            { "dungeon", 105090200 },
            { "mine", 211041400 },
            // Area boss maps
            { "jrbalrog", 105090900 },
            { "mushmom", 100000005 },
            // PQ maps
            { "kpqexit", 103000890 },
            { "kpqbonus", 103000805 },
            { "kingslime", 103000804 },
            { "kpq5", 103000804 },
            { "kpq4", 103000803 },
            { "kpq3", 103000802 },
            { "kpq2", 103000801 },
            { "kpq1", 103000800 },
            // Boss maps
            { "zakum", 280030000 },
            // Contimove
            { "elliniastation", 101000300 },
            { "orbisstation", 200000100 },
            { "orbiselliniastation", 200000111 },
            { "orbisludistation", 200000121 },
            { "orbiselliniatakeoff", 200000112 },
            { "orbiselliniaboat", 200090000 },
            { "elliniaorbistakeoff", 101000301 },
            { "elliniaorbisboat", 200090010 },
            // Events
            { "findthejewel", 109010000 },
            { "jewel", 109010000 },
            { "ftj", 109010000 },
            { "snowball", 109060001 },
            { "fitness", 109040000 },
            { "ox", 109020001 },
            { "quiz", 109020001 },
            { "oxquiz", 109020001 }
        };

        private readonly string[] AdminCommands = new string[]
        {
            "map <mapid/mapname>: Go to a map (alias: goto)",
            "killmobs: Kill all mobs in map (alias: killall)",
            "killmobsdmg [amount of damage]: Kill all mobs. When [amount of damage] is set, show that amount of damage. (alias: killalldmg)",
            "chase <name>: Warp to 'name' (alias: warp)",
            "chasehere <name>: Warp 'name' to yourself (alias: warphere)",
            "online: Show all online players",
            "dc <name>: Disconnect player from server (alias: kick)",
            "permaban <userid/charname/charid> <value>: Permaban + kick user. For example, !permaban charname hacker",
            "unban <userid>: Unban a user.",
            "maxskills: Max all skills",
            "job <jobid>: Make yourself a specific job.",
            "mp/hp/str/dex/int/luk/ap/sp/level <amount>: Set your MP/HP/str/dex/int/luk/ap/sp/level to 'amount'",
            "addsp <amount>: Add 'amount' SP",
            "lvl <amount>: Set your level to 'amount' (alias: level)",
            "maxslots: Get 100 slots in all your inventories.",
            "maxstats: Get 3k HP/MP/str/dex/int/luk, 0 AP and 2k SP.",
            "pos: Get your current location info (alias: pos1, pos2, pos3)",
            // TODO: add reactor? Its pretty complex
            "cleardrops: Remove all drops from the map"
        };

        public static int GetMapidFromName(string name)
        {
            if (MapNameList.ContainsKey(name)) return MapNameList[name];
            else return -1;
        }

        private static Character.BanReasons GetBanReasonFromText(CommandArg arg)
        {
            switch (arg)
            {
                // Your account has been blocked for hacking or illegal use of third-party programs.
                case "1":
                case "ct":
                case "h":
                case "hack":
                case "hax": return Character.BanReasons.Hack;

                // Your account has been blocked for using macro / auto-keyboard.
                case "2":
                case "bot":
                case "macro": return Character.BanReasons.Macro;

                // Your account has been blocked for illicit promotion and advertising.
                case "3":
                case "promo":
                case "ad":
                case "ads":
                case "advertisement": return Character.BanReasons.Advertisement;

                // Your account has been blocked for for harassment.
                case "4":
                case "harass":
                case "harassment": return Character.BanReasons.Harassment;

                // Your account has been blocked for using profane language.
                case "5":
                case "trol":
                case "trolling":
                case "curse":
                case "badlanguage": return Character.BanReasons.BadLanguage;

                // Your account has been blocked for scamming.
                case "6":
                case "scamming":
                case "scam": return Character.BanReasons.Scam;

                // Your account has been blocked for misconduct.
                case "7":
                case "ks":
                case "misconduct": return Character.BanReasons.Misconduct;

                // Your account has been blocked for illegal cash transaction
                case "8":
                case "sell":
                case "irlmoney": return Character.BanReasons.Sell;

                // Your account has been blocked for illegal charging/funding. Please contact customer support for further details.
                case "9":
                case "moneyloundry":
                case "icash": return Character.BanReasons.ICash;

                default: return Character.BanReasons.Hack;
            }
        }

        enum UserIdFetchResult
        {
            Found,
            UserNotFound,
            PlayerNotFound,
            IDNotFound,
            UnknownType
        }

        private static UserIdFetchResult GetUserIDFromArgs(CommandArg argType, CommandArg argValue, out int userId)
        {
            userId = 0;
            switch (argType)
            {
                case "uid":
                case "userid":
                    if (!int.TryParse(argValue, out int u) || !Server.Instance.CharacterDatabase.ExistsUser(u))
                    {
                        return UserIdFetchResult.UserNotFound;
                    }
                    else
                    {
                        userId = u;
                    }

                    break;
                case "user":
                case "username":
                    userId = Server.Instance.CharacterDatabase.UserIDByUsername(argValue);
                    if (userId == -1)
                    {
                        return UserIdFetchResult.UserNotFound;
                    }
                    break;
                case "name":
                case "player":
                case "character":
                case "charname":
                    int id = Server.Instance.CharacterDatabase.UserIDByCharacterName(argValue);
                    if (id == -1)
                    {
                        return UserIdFetchResult.PlayerNotFound;
                    }
                    else
                    {
                        userId = id;
                    }
                    break;
                case "cid":
                case "charid":
                    int uid = Server.Instance.CharacterDatabase.UserIDByCharID(int.Parse(argValue));
                    if (uid == -1)
                    {
                        return UserIdFetchResult.IDNotFound;
                    }
                    else
                    {
                        userId = uid;
                    }
                    break;
                default:
                    return UserIdFetchResult.UnknownType;

            }

            return UserIdFetchResult.Found;
        }

        static bool shuttingDown = false;
        public static bool HandleChat(Character character, string text)
        {
            if (!character.IsGM) return false;

            string logtext = string.Format("[{0,-9}] {1,-13}: {2}", character.MapID, character.Name, text);
            if (!Directory.Exists("Chatlogs"))
            {
                Directory.CreateDirectory("Chatlogs");
            }
            File.AppendAllText(Path.Combine("Chatlogs", "Map-" + character.MapID + ".txt"), logtext + Environment.NewLine);
            File.AppendAllText(Path.Combine("Chatlogs", character.Name + ".txt"), logtext + Environment.NewLine);

            if (text[0] != '!' && text[0] != '/') return false;

            try
            {
                var Args = new CommandArgs(text);

                if (character.GMLevel >= 1) //Intern commands
                {
                    switch (Args.Command.ToLowerInvariant())
                    {
                        #region Map / Goto

                        case "m":
                        case "map":
                        case "goto":
                            {
                                if (Args.Count > 0)
                                {
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
                                        MessagePacket.SendText(MessagePacket.MessageTypes.RedText, "Map not found.",
                                            character, MessagePacket.MessageMode.ToPlayer);
                                }
                                return true;
                            }

                        #endregion

                        #region Chase / Warp

                        case "chase":
                        case "warp":
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

                        #endregion

                        #region ChaseHere / WarpHere

                        case "chasehere":
                        case "warphere":
                            {
                                if (Args.Count > 0)
                                {
                                    string other = Args[0].Value.ToLower();
                                    var otherChar = Server.Instance.GetCharacter(other);
                                    if (otherChar != null)
                                    {
                                        if (otherChar.MapID != character.MapID)
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

                        #endregion

                        #region Online

                        case "online":
                            {
                                string playersonline =
                                    "Players online (" + Server.Instance.CharacterList.Count + "): \r\n";
                                playersonline += string.Join(
                                    ", ",
                                    Server.Instance.CharacterList.Select(x =>
                                        x.Value.Name + (x.Value.IsAFK ? " (AFK)" : ""))
                                );
                                MessagePacket.SendNotice(playersonline, character);
                                return true;
                            }

                        #endregion

                        #region DC / Kick

                        case "dc":
                        case "kick":
                            {
                                if (Args.Count > 0)
                                {
                                    string victim = Args[0].Value.ToLower();
                                    Character who = Server.Instance.GetCharacter(victim);

                                    if (who != null)
                                        who.Player.Socket.Disconnect();
                                    else
                                        MessagePacket.SendText(MessagePacket.MessageTypes.RedText,
                                            "You have entered an incorrect name.", character,
                                            MessagePacket.MessageMode.ToPlayer);
                                }
                                return true;
                            }

                        #endregion

                        #region Ban

                        case "ban":
                        case "banhelp":
                            {
                                MessagePacket.SendNotice(
                                    "Help: Use !permaban <userid/charname/charid> <value> (reason) to ban permanently. Use !suspend <userid/charname/charid> <value> <days to suspend> (reason)",
                                    character);
                                return true;
                            }
                        case "permban":
                        case "permaban":
                            {
                                if (Args.Count >= 2)
                                {
                                    Character.BanReasons banReason = Args.Count >= 3
                                        ? GetBanReasonFromText(Args[2])
                                        : Character.BanReasons.Hack;

                                    switch (GetUserIDFromArgs(Args[0], Args[1], out int userId))
                                    {
                                        case UserIdFetchResult.UnknownType: break; // Fallthrough
                                        case UserIdFetchResult.IDNotFound:
                                            MessagePacket.SendNotice("User with char id " + Args[1] + " does not exist",
                                                character);
                                            return true;
                                        case UserIdFetchResult.PlayerNotFound:
                                            MessagePacket.SendNotice("Player " + Args[1] + " does not exist.",
                                                character);
                                            return true;
                                        case UserIdFetchResult.UserNotFound:
                                            MessagePacket.SendNotice("User " + Args[1] + " does not exist.", character);
                                            return true;
                                        case UserIdFetchResult.Found:
                                            Server.Instance.CharacterDatabase.PermaBan(userId, (byte)banReason, character.Name, "");
                                            Server.Instance.CenterConnection.KickUser(userId);

                                            var msg =
                                                $"[{character.Name}] Permabanned {Args[0]} {Args[1]} (userid {userId}), reason {banReason}";
                                            Server.Instance.BanDiscordReporter.Enqueue(msg);
                                            MessagePacket.SendNoticeGMs(msg, MessagePacket.MessageTypes.RedText);
                                            return true;
                                    }
                                }
                                MessagePacket.SendNotice("Usage: !permaban <userid/charname/charid> <value> (reason)",
                                    character);

                                return true;
                            }
                        case "suspend":
                        case "tempban":
                            {
                                if (Args.Count >= 3 && Args[2].IsNumber())
                                {
                                    Character.BanReasons banReason = Args.Count > 3
                                        ? GetBanReasonFromText(Args[3])
                                        : Character.BanReasons.Hack;

                                    switch (GetUserIDFromArgs(Args[0], Args[1], out int userId))
                                    {
                                        case UserIdFetchResult.UnknownType: break; // Fallthrough
                                        case UserIdFetchResult.IDNotFound:
                                            MessagePacket.SendNotice("User with char id " + Args[1] + " does not exist",
                                                character);
                                            return true;
                                        case UserIdFetchResult.PlayerNotFound:
                                            MessagePacket.SendNotice("Player " + Args[1] + " does not exist.",
                                                character);
                                            return true;
                                        case UserIdFetchResult.UserNotFound:
                                            MessagePacket.SendNotice("User " + Args[1] + " does not exist.", character);
                                            return true;
                                        case UserIdFetchResult.Found:
                                            var hours = Args[2].GetInt32();

                                            Server.Instance.CharacterDatabase.TempBan(userId, (byte)banReason, hours, character.Name);
                                            Server.Instance.CenterConnection.KickUser(userId);

                                            var msg =
                                                $"[{character.Name}] Tempbanned {Args[0]} {Args[1]} (userid {userId}), reason {banReason}, hours {hours}";
                                            Server.Instance.BanDiscordReporter.Enqueue(msg);
                                            MessagePacket.SendNoticeGMs(msg, MessagePacket.MessageTypes.RedText);
                                            return true;
                                    }
                                }
                                MessagePacket.SendNotice(
                                    "Usage: !suspend/tempban <userid/charname/charid> <value> <hours> (reason)",
                                    character);
                                return true;
                            }

                        #endregion

                        #region Unban

                        case "unban":
                            {
                                if (Args.Count == 2)
                                {
                                    switch (GetUserIDFromArgs(Args[0], Args[1], out int userId))
                                    {
                                        case UserIdFetchResult.UnknownType: break; // Fallthrough
                                        case UserIdFetchResult.IDNotFound:
                                            MessagePacket.SendNotice("User with char id " + Args[1] + " does not exist",
                                                character);
                                            return true;
                                        case UserIdFetchResult.PlayerNotFound:
                                            MessagePacket.SendNotice("Player " + Args[1] + " does not exist.",
                                                character);
                                            return true;
                                        case UserIdFetchResult.UserNotFound:
                                            MessagePacket.SendNotice("User " + Args[1] + " does not exist.", character);
                                            return true;
                                        case UserIdFetchResult.Found:
                                            Server.Instance.CharacterDatabase.RunQuery(
                                                "UPDATE users SET ban_expire = @expire_date WHERE ID = @id",
                                                "@id", userId,
                                                "@expire_date", MasterThread.CurrentDate.AddDays(-1).ToUniversalTime()
                                            );

                                            var msg =
                                                $"[{character.Name}] Unbanned {Args[0]} {Args[1]} (userid {userId})";
                                            Server.Instance.BanDiscordReporter.Enqueue(msg);
                                            MessagePacket.SendNoticeGMs(msg, MessagePacket.MessageTypes.RedText);
                                            return true;
                                    }
                                }
                                MessagePacket.SendNotice("Usage: !unban <userid/charname/charid> <value>", character);

                                return true;
                            }

                        #endregion

                        #region Muting

                        case "muteban":
                        case "mute":
                            {
                                if (Args.Count >= 3 && Args[2].IsNumber())
                                {
                                    MessagePacket.MuteReasons banReason = Args.Count > 3
                                        ? MessagePacket.ParseMuteReason(Args[3])
                                        : MessagePacket.MuteReasons.FoulLanguage;

                                    if (banReason == 0)
                                    {
                                        MessagePacket.SendNotice("Unknown mute reason.", character);
                                        return true;
                                    }

                                    switch (GetUserIDFromArgs(Args[0], Args[1], out int userId))
                                    {
                                        case UserIdFetchResult.UnknownType: break; // Fallthrough
                                        case UserIdFetchResult.IDNotFound:
                                            MessagePacket.SendNotice(
                                                "User with char id " + Args[1] + " does not exist", character);
                                            return true;
                                        case UserIdFetchResult.PlayerNotFound:
                                            MessagePacket.SendNotice("Player " + Args[1] + " does not exist.",
                                                character);
                                            return true;
                                        case UserIdFetchResult.UserNotFound:
                                            MessagePacket.SendNotice("User " + Args[1] + " does not exist.",
                                                character);
                                            return true;
                                        case UserIdFetchResult.Found:
                                            var hours = Args[2].GetInt32();

                                            Server.Instance.CharacterDatabase.MuteBan(userId, (byte)banReason, hours);

                                            var localPlayers = Server.Instance.CharacterList
                                                .Where(x => x.Value.UserID == userId).ToArray();
                                            if (localPlayers.Length == 0)
                                            {
                                                Server.Instance.CenterConnection.KickUser(userId);
                                            }
                                            else
                                            {
                                                localPlayers.ForEach(x =>
                                                {
                                                    x.Value.MutedUntil = MasterThread.CurrentDate.AddDays(hours);
                                                    x.Value.MuteReason = (byte)banReason;
                                                });
                                            }

                                            var msg =
                                                $"[{character.Name}] Muted {Args[0]} {Args[1]} (userid {userId}), reason {banReason}, hours {hours}";
                                            Server.Instance.MutebanDiscordReporter.Enqueue(msg);
                                            MessagePacket.SendNoticeGMs(msg, MessagePacket.MessageTypes.RedText);
                                            return true;
                                    }
                                }
                                MessagePacket.SendNotice(
                                    "Usage: !muteban/mute <userid/charname/charid> <value> <hours> (reason)",
                                    character);
                                return true;
                            }
                        case "unmute":
                            {
                                if (Args.Count == 2)
                                {
                                    switch (GetUserIDFromArgs(Args[0], Args[1], out int userId))
                                    {
                                        case UserIdFetchResult.UnknownType: break; // Fallthrough
                                        case UserIdFetchResult.IDNotFound:
                                            MessagePacket.SendNotice("User with char id " + Args[1] + " does not exist",
                                                character);
                                            return true;
                                        case UserIdFetchResult.PlayerNotFound:
                                            MessagePacket.SendNotice("Player " + Args[1] + " does not exist.",
                                                character);
                                            return true;
                                        case UserIdFetchResult.UserNotFound:
                                            MessagePacket.SendNotice("User " + Args[1] + " does not exist.", character);
                                            return true;
                                        case UserIdFetchResult.Found:
                                            Server.Instance.CharacterDatabase.RunQuery(
                                                "UPDATE users SET quiet_ban_expire = @date WHERE ID = @id", "@id",
                                                userId, "@date", MasterThread.CurrentDate);

                                            var localPlayers = Server.Instance.CharacterList
                                                .Where(x => x.Value.UserID == userId).ToArray();
                                            if (localPlayers.Length == 0)
                                            {
                                                Server.Instance.CenterConnection.KickUser(userId);
                                            }
                                            else
                                            {
                                                localPlayers.ForEach(x =>
                                                {
                                                    x.Value.MutedUntil = MasterThread.CurrentDate;
                                                });
                                            }

                                            var msg =
                                                $"[{character.Name}] Unmuted {Args[0]} {Args[1]} (userid {userId})";
                                            Server.Instance.MutebanDiscordReporter.Enqueue(msg);
                                            MessagePacket.SendNoticeGMs(msg, MessagePacket.MessageTypes.RedText);
                                            return true;
                                    }
                                }
                                MessagePacket.SendNotice("Usage: !unmute <userid/charname/charid> <value>", character);

                                return true;
                            }

                        #endregion

                        #region Hackmute / Hackunmute

                        case "hackmute":
                            {
                                if (Args.Count == 2 && Args[1].IsNumber())
                                {
                                    var chr = Server.Instance.GetCharacter(Args[0]);
                                    if (chr == null)
                                    {
                                        MessagePacket.SendNotice("Character " + Args[0] + " not found on this channel.",
                                            character);
                                    }
                                    else
                                    {
                                        var hours = Args[1].GetInt32();
                                        chr.HacklogMuted = MasterThread.CurrentDate.AddHours(hours);
                                        RedisBackend.Instance.MuteCharacter(character.ID, chr.ID, hours);
                                        MessagePacket.SendNoticeGMs(
                                            $"[{character.Name}] Muted character {Args[0]} for {hours} hours.",
                                            MessagePacket.MessageTypes.RedText);
                                    }
                                    return true;
                                }
                                MessagePacket.SendNotice("Usage: !hackmute <charactername> <hours>", character);
                                return true;
                            }
                        case "hackunmute":
                            {
                                if (Args.Count == 1)
                                {
                                    var chr = Server.Instance.GetCharacter(Args[0]);
                                    if (chr == null)
                                    {
                                        MessagePacket.SendNotice("Character " + Args[0] + " not found on this channel.",
                                            character);
                                    }
                                    else
                                    {
                                        chr.HacklogMuted = DateTime.MinValue;
                                        RedisBackend.Instance.UnmuteCharacter(chr.ID);
                                        MessagePacket.SendNoticeGMs($"[{character.Name}] Unmuted character {Args[0]}",
                                            MessagePacket.MessageTypes.RedText);
                                    }
                                    return true;
                                }
                                MessagePacket.SendNotice("Usage: !hackunmute <charactername>", character);
                                return true;
                            }

                        #endregion

                        #region MoveTrace

                        case "movetracepet":
                        case "movetraceplayer":
                        case "movetracemob":
                        case "movetracesummon":
                            {
                                MovePath.MovementSource source = 0;
                                switch (Args.Command.Replace("movetrace", ""))
                                {
                                    case "pet":
                                        source = MovePath.MovementSource.Pet;
                                        break;
                                    case "player":
                                        source = MovePath.MovementSource.Player;
                                        break;
                                    case "mob":
                                        source = MovePath.MovementSource.Mob;
                                        break;
                                    case "summon":
                                        source = MovePath.MovementSource.Summon;
                                        break;
                                }

                                if (Args.Count == 3 && Args[2].IsNumber())
                                {
                                    switch (GetUserIDFromArgs(Args[0], Args[1], out int userId))
                                    {
                                        case UserIdFetchResult.UnknownType: break; // Fallthrough
                                        case UserIdFetchResult.IDNotFound:
                                            MessagePacket.SendNotice("User with char id " + Args[1] + " does not exist",
                                                character);
                                            return true;
                                        case UserIdFetchResult.PlayerNotFound:
                                            MessagePacket.SendNotice("Player " + Args[1] + " does not exist.",
                                                character);
                                            return true;
                                        case UserIdFetchResult.UserNotFound:
                                            MessagePacket.SendNotice("User " + Args[1] + " does not exist.", character);
                                            return true;
                                        case UserIdFetchResult.Found:
                                            var amount = Math.Min(Args[2].GetInt32(), 10);

                                            var localPlayers = Server.Instance.CharacterList
                                                .Where(x => x.Value.UserID == userId).ToArray();
                                            if (localPlayers.Length > 0)
                                            {
                                                localPlayers.ForEach(x =>
                                                {
                                                    x.Value.MoveTraceCount = amount;
                                                    x.Value.MoveTraceSource = source;
                                                });
                                                MessagePacket.SendNotice(
                                                    $"Tracing player type {source} amount {amount}!", character);
                                            }
                                            return true;
                                    }
                                }
                                MessagePacket.SendNotice(
                                    "Usage: !movetrace(pet|player|mob|summon) <userid/charname/charid> <value> <amount>",
                                    character);
                                return true;
                            }

                        #endregion

                        #region Warn

                        case "w":
                        case "warn":
                            {
                                if (Args.Count >= 2)
                                {
                                    var charname = Args[0];
                                    var victim = Server.Instance.GetCharacter(charname);
                                    if (victim != null)
                                    {
                                        AdminPacket.SentWarning(character, true);
                                        MessagePacket.SendAdminWarning(victim, string.Join(" ", Args.Args.Skip(1)));
                                    }
                                    else
                                    {
                                        AdminPacket.SentWarning(character, false);
                                    }
                                    return true;
                                }
                                MessagePacket.SendNotice("Usage: !warn charname <text...>", character);
                                return true;
                            }

                        case "wm":
                        case "warnmap":
                            {
                                if (Args.Count >= 1)
                                {
                                    AdminPacket.SentWarning(character, true);
                                    MessagePacket.SendAdminWarning(character.Field, string.Join(" ", Args.Args));

                                    return true;
                                }
                                MessagePacket.SendNotice("Usage: !warnmap <text...>", character);
                                return true;
                            }

                        #endregion

                        #region MaxSkills

                        case "maxskills":
                            {
                                var mMaxedSkills = new Dictionary<int, byte>();
                                foreach (var kvp in DataProvider.Skills)
                                {
                                    var level = kvp.Value.MaxLevel;
                                    character.Skills.SetSkillPoint(kvp.Key, level, false);
                                    mMaxedSkills.Add(kvp.Key, level);
                                }
                                SkillPacket.SendSetSkillPoints(character, mMaxedSkills); // 1 packet for all skills
                                mMaxedSkills.Clear();
                                return true;
                            }

                        #endregion

                        #region Job

                        case "job":
                            {
                                if (Args.Count > 0 && Args[0].IsNumber())
                                    character.SetJob(Args[0].GetInt16());
                                return true;
                            }

                        #endregion

                        #region MP

                        case "mp":
                            {
                                if (Args.Count > 0 && Args[0].IsNumber())
                                    character.SetMPAndMaxMP(Args[0].GetInt16());
                                return true;
                            }

                        #endregion

                        #region HP

                        case "hp":
                            {
                                if (Args.Count > 0 && Args[0].IsNumber())
                                    character.SetHPAndMaxHP(Args[0].GetInt16());
                                return true;
                            }

                        #endregion

                        #region Str

                        case "str":
                            {
                                if (Args.Count > 0 && Args[0].IsNumber())
                                    character.SetStr(Args[0].GetInt16());
                                return true;
                            }

                        #endregion

                        #region Dex

                        case "dex":
                            {
                                if (Args.Count > 0 && Args[0].IsNumber())
                                    character.SetDex(Args[0].GetInt16());
                                return true;
                            }

                        #endregion

                        #region Int

                        case "int":
                            {
                                if (Args.Count > 0 && Args[0].IsNumber())
                                    character.SetInt(Args[0].GetInt16());
                                return true;
                            }

                        #endregion

                        #region Luk

                        case "luk":
                            {
                                if (Args.Count > 0 && Args[0].IsNumber())
                                    character.SetLuk(Args[0].GetInt16());
                                return true;
                            }

                        #endregion

                        #region AP

                        case "ap":
                            {
                                if (Args.Count > 0 && Args[0].IsNumber())
                                    character.SetAP(Args[0].GetInt16());
                                return true;
                            }

                        #endregion

                        #region SP

                        case "sp":
                            {
                                if (Args.Count > 0 && Args[0].IsNumber())
                                    character.SetSP(Args[0].GetInt16());
                                return true;
                            }

                        #endregion

                        #region AddSP

                        case "addsp":
                            {
                                if (Args.Count > 0 && Args[0].IsNumber())
                                    character.AddSP(Args[0].GetInt16());
                                return true;
                            }

                        #endregion

                        #region level/lvl

                        case "level":
                        case "lvl":
                            {
                                if (Args.Count > 0 && Args[0].IsNumber())
                                    character.SetLevel(Args[0].GetByte());
                                return true;
                            }

                        #endregion

                        #region MaxSlots

                        case "maxslots":
                            {
                                character.Inventory.SetInventorySlots(1, 100);
                                character.Inventory.SetInventorySlots(2, 100);
                                character.Inventory.SetInventorySlots(3, 100);
                                character.Inventory.SetInventorySlots(4, 100);
                                character.Inventory.SetInventorySlots(5, 100);
                                return true;
                            }

                        #endregion

                        #region MaxStats

                        case "maxstats":
                            {
                                character.SetHPAndMaxHP(30000);
                                character.SetMPAndMaxMP(30000);
                                character.SetLuk(30000);
                                character.SetStr(30000);
                                character.SetInt(30000);
                                character.SetDex(30000);
                                character.SetAP(0);
                                character.SetSP(2000);
                                return true;
                            }

                        #endregion

                        #region Pos

                        case "pos":
                        case "pos1": //prevent client limitation when spamming this command during testing.
                        case "pos2":
                        case "pos3":
                            {
                                string ret = "Position of " + character.Name + ". X: " + character.Position.X +
                                             ". Y: " + character.Position.Y + ". Fh: " + character.Foothold + ".";
                                MessagePacket.SendNotice(ret, character);
                                return true;
                            }

                        #endregion

                        #region Undercover

                        case "undercover":
                            if (Args.Count == 1)
                            {
                                var undercover = Args[0].GetBool();
                                RedisBackend.Instance.SetUndercover(character.ID, undercover);
                                character.Undercover = undercover;
                                MessagePacket.SendNotice(
                                    "You are now " + (undercover ? "" : "not") + " undercover.", character);
                                return true;
                            }

                            MessagePacket.SendNotice("Usage: !undercover <true/false>", character);
                            return true;

                        #endregion

                        #region reportlog/reports

                        case "reportlog":
                        case "reports":
                            {
                                MessagePacket.SendNotice("These are the last (at most) 15 reports: ", character);
                                ReportManager.GetAbuseReports()
                                    .ForEach(r => MessagePacket.SendNotice(r.ToString(), character));
                                return true;
                            }

                        #endregion

                        #region whowashere

                        case "whowashere":
                            {
                                const int MaxAmount = 10;
                                MessagePacket.SendNotice(
                                    "These are the last (at most) " + MaxAmount + " players that entered the map:",
                                    character);
                                var lastPlayers = character.Field.PlayersThatHaveBeenHere.ToList();
                                lastPlayers.Sort((x, y) => (int)(y.Value - x.Value));

                                var str = string.Join(", ", lastPlayers.Take(MaxAmount).Select(x =>
                                {
                                    long secondsAgo = (MasterThread.CurrentTime - x.Value) / 1000;
                                    return x.Key + " (" + secondsAgo + "s ago)";
                                }));

                                MessagePacket.SendNotice(str, character);

                                return true;
                            }

                        #endregion

                        #region runscript

                        case "run":
                        case "runscript":
                            {
                                if (Args.Count == 1)
                                {
                                    NpcChatSession.Start(
                                        2000,
                                        Args[0].Value,
                                        character,
                                        script =>
                                        {
                                            MessagePacket.SendNotice("Error compiling script: " + script, character);
                                        }
                                    );
                                }
                                return true;
                            }

                            #endregion
                    }
                }

                if (character.GMLevel >= 2) //Full GMs
                {
                    switch (Args.Command.ToLowerInvariant())
                    {
                        #region Create / Item

                        case "create":
                        case "item":
                            {
                                try
                                {
                                    if (Args.Count > 0 && Args[0].IsNumber())
                                    {
                                        short Amount = 1;
                                        int ItemID = Args[0].GetInt32();
                                        byte Inv = (byte)(ItemID / 1000000);

                                        if (Inv <= 0 || Inv > 5 ||
                                            (!DataProvider.Equips.ContainsKey(ItemID) &&
                                             !DataProvider.Items.ContainsKey(ItemID) &&
                                             !DataProvider.Pets.ContainsKey(ItemID)))
                                        {
                                            MessagePacket.SendNotice("Item not found :(", character);
                                            return true;
                                        }

                                        var FreeSlots = character.Inventory.ItemAmountAvailable(ItemID);
                                        if (Args.Count >= 2)
                                        {
                                            if (Args[1] == "max" || Args[1] == "fill" || Args[1] == "full")
                                                Amount = (short)(FreeSlots > short.MaxValue
                                                    ? short.MaxValue
                                                    : FreeSlots);
                                            else if (Args[1].IsNumber())
                                            {
                                                Amount = Args[1].GetInt16();
                                                if (Amount > FreeSlots)
                                                    Amount = (short)(FreeSlots > short.MaxValue
                                                        ? short.MaxValue
                                                        : FreeSlots);
                                            }
                                        }

                                        if (Amount == 0)
                                        {
                                            DropPacket.CannotLoot(character, -1);
                                            InventoryPacket.NoChange(character);
                                        }
                                        else
                                        {
                                            character.Inventory.AddNewItem(ItemID, Amount);
                                            CharacterStatsPacket.SendGainDrop(character, false, ItemID, Amount);
                                        }
                                    }
                                    else
                                        MessagePacket.SendNotice($"Command syntax: !{Args.Command} [itemid] {{amount}}",
                                            character);
                                    return true;
                                }
                                catch (Exception ex)
                                {
                                    MessagePacket.SendNotice($"Command syntax: !{Args.Command} [itemid] {{amount}}",
                                        character);
                                    if (character.IsGM)
                                    {
                                        MessagePacket.SendNotice(string.Format("LOLEXCEPTION: {0}", ex.ToString()),
                                            character);
                                    }
                                    return true;
                                }
                            }

                        #endregion

                        #region Summon / Spawn

                        case "summon":
                        case "spawn":
                            {
                                if (Args.Count > 0)
                                {
                                    var Amount = 1;
                                    var MobID = -1;

                                    if (Args[0].IsNumber())
                                        MobID = Args[0].GetInt32();

                                    if (Args.Count > 1 && Args[1].IsNumber())
                                        Amount = Args[1].GetInt32();

                                    Amount = character.IsAdmin ? Amount : Math.Min(Amount, 100);

                                    if (DataProvider.Mobs.ContainsKey(MobID))
                                    {
                                        for (int i = 0; i < Amount; i++)
                                        {
                                            character.Field.SpawnMobWithoutRespawning(MobID, character.Position,
                                                character.Foothold);
                                        }
                                    }
                                    else
                                        MessagePacket.SendText(MessagePacket.MessageTypes.RedText, "Mob not found.",
                                            character, MessagePacket.MessageMode.ToPlayer);
                                }
                                return true;
                            }

                        #endregion

                        #region VarSet

                        case "varset":
                            {
                                if (Args.Count == 0)
                                    MessagePacket.SendNotice(
                                        "Usable args are Hp, Mp, Exp, MaxHp, MaxMp, Ap, Sp, Str, Dex, Int, Luk, Job, Level, Gender, Skin, Face, and Hair for Users and Name, Level, Tameness, Hunger for Pets.",
                                        character);
                                else if (Args.Count == 2 ||
                                         (Args[0].Value.ToLower() == "skill" && (Args.Count == 3 || Args.Count == 4)))
                                    character.OnVarset(character, Args[0].Value, Args[1].Value,
                                        (Args.Count >= 3) ? Args[2].Value : null,
                                        (Args.Count == 4) ? Args[3].Value : null);
                                else if (Args.Count == 3 ||
                                         (Args[1].Value.ToLower() == "skill" && (Args.Count == 4 || Args.Count == 5)))
                                {
                                    if (Args[0].Value.ToLower() == "pet")
                                        character.OnPetVarset(Args[1].Value, Args[2].Value, true);
                                    else
                                    {
                                        var Player = character.Field.FindUser(Args[0].Value);
                                        if (Player != null)
                                            Player.OnVarset(character, Args[1].Value, Args[2].Value,
                                                (Args.Count >= 4) ? Args[3].Value : null,
                                                (Args.Count == 5) ? Args[4].Value : null);
                                        else
                                            MessagePacket.SendNotice($"Unable to find {Args.Args[0].Value}", character);
                                    }
                                }
                                else if (Args.Args.Count == 3)
                                {
                                    var Player = character.Field.FindUser(Args[0].Value);
                                    if (Player != null && Args[1].Value.ToLower() == "pet")
                                        Player.OnPetVarset(Args[2].Value, Args[3].Value, false);
                                    else
                                        MessagePacket.SendNotice("Unable to find the user or pet", character);
                                }
                                else
                                    MessagePacket.SendNotice("Too many or not enough args!", character);
                                return true;
                            }

                        #endregion

                        #region GetID

                        case "getid":
                            {
                                if (Args.Count > 0)
                                {
                                    string name = Args[0].Value.ToLower();
                                    Server.Instance.CharacterDatabase.RunQuery(
                                        "SELECT * FROM characters WHERE name = @name", "@name", name);
                                    MySqlDataReader data = Server.Instance.CharacterDatabase.Reader;
                                    data.Read();
                                    int id = data.GetInt32("ID");
                                    MessagePacket.SendText(MessagePacket.MessageTypes.RedText, "ID is " + id + ".",
                                        character, MessagePacket.MessageMode.ToPlayer);
                                }
                                return true;
                            }

                        #endregion

                        #region d / delete

                        case "d":
                        case "delete":
                            {
                                if (Args.Count == 1 && Args[0].IsNumber())
                                {
                                    var inv = Args[0].GetByte();

                                    if (inv >= 0 && inv <= 4)
                                    {
                                        // Find first item to delete
                                        var slot = character.Inventory.DeleteFirstItemInInventory(inv);
                                        if (slot != 0)
                                        {
                                            InventoryPacket.SwitchSlots(character, slot, 0, (byte)(inv + 1));
                                        }
                                        else
                                        {
                                            MessagePacket.SendNotice("No item to delete found.", character);
                                        }
                                        return true;
                                    }
                                }
                                MessagePacket.SendNotice("Usage: !delete <inventory, 0=equip, 1=use, etc>", character);
                                return true;
                            }

                        #endregion

                        #region ClearDrops

                        case "cleardrops":
                            {
                                character.Field.DropPool.Clear();
                                return true;
                            }

                        #endregion

                        #region KillMobs / KillAll

                        case "killmobs":
                        case "killall":
                            {
                                int amount = character.Field.KillAllMobs(character, false, 0);
                                MessagePacket.SendNotice("Amount of mobs killed: " + amount.ToString(), character);
                                return true;
                            }

                        #endregion

                        #region KillMobsDMG

                        case "killalldmg":
                        case "killmobsdmg":
                            {
                                int dmg = Args.Count == 0 ? 0 : Args[0].GetInt32();
                                int amount = character.Field.KillAllMobs(character, true, dmg);
                                MessagePacket.SendNotice("Amount of mobs killed: " + amount.ToString(), character);
                                return true;
                            }

                        #endregion

                        #region MapNotice

                        case "mapnotice":
                            {
                                if (Args.Count > 0)
                                    MessagePacket.SendText(MessagePacket.MessageTypes.PopupBox,
                                        $"[{character.Name}] : {Args.CommandText}", character,
                                        MessagePacket.MessageMode.ToMap);
                                return true;
                            }

                        #endregion

                        #region ditto/datto

                        case "ditto":
                            {
                                if (Args.Count == 1)
                                {
                                    int charid = Server.Instance.CharacterDatabase.CharacterIdByName(Args[0]);
                                    if (charid == -1)
                                    {
                                        if (int.TryParse(Args[0], out charid) == false)
                                        {
                                            MessagePacket.SendNotice(
                                                "Character " + Args[0] + " not found??", character);
                                            return true;
                                        }
                                    }

                                    RedisBackend.Instance.SetImitateID(character.ID, charid);
                                    MessagePacket.SendNoticeGMs($"[{character.Name}] Imitating character {Args[0]}.",
                                        MessagePacket.MessageTypes.RedText);
                                    // CC
                                    character.Player.Socket.DoChangeChannelReq(Server.Instance.ID);
                                    return true;
                                }
                                MessagePacket.SendNotice("Usage: !ditto <charactername or id>", character);
                                return true;
                            }
                        case "datto":
                            {
                                RedisBackend.Instance.SetImitateID(character.ID, 0);
                                MessagePacket.SendNoticeGMs(
                                    $"[{character.ImitatorName}] Stopped imitating {character.Name}. Glad to have you back",
                                    MessagePacket.MessageTypes.RedText);
                                return true;
                            }

                        #endregion

                        #region Notice

                        case "notice":
                            {
                                if (Args.Count > 0)
                                {
                                    MessagePacket.SendText(MessagePacket.MessageTypes.Notice, Args.CommandText, null,
                                        MessagePacket.MessageMode.ToChannel);
                                }
                                return true;
                            }

                        #endregion

                        #region SetSP

                        case "setsp":
                            {
                                if (Args.Count > 1 && Args[0].IsNumber())
                                {
                                    int SkillID = Args[0].GetInt32();
                                    byte Level = 1;
                                    byte MaxLevel = (byte)(DataProvider.Skills.TryGetValue(SkillID, out var sd)
                                        ? sd.MaxLevel
                                        : 0);

                                    if (MaxLevel > 0)
                                    {
                                        if (Args[1] == "max")
                                            Level = MaxLevel;
                                        else if (Args[1].IsNumber())
                                            Level = Args[1].GetByte();
                                        else
                                            Level = 1;

                                        character.Skills.SetSkillPoint(SkillID, Level);
                                    }
                                    else
                                        MessagePacket.SendNotice("Skill not found.", character);
                                }
                                return true;
                            }

                        #endregion

                        #region Job

                        case "job":
                            {
                                if (Args.Count > 0 && Args[0].IsNumber())
                                    character.SetJob(Args[0].GetInt16());
                                return true;
                            }

                        #endregion

                        #region Heal

                        case "heal":
                            {
                                int hpHealed = character.PrimaryStats.GetMaxHP(false) - character.PrimaryStats.HP;
                                character.ModifyHP(character.PrimaryStats.GetMaxHP(false));
                                character.ModifyMP(character.PrimaryStats.GetMaxMP(false));
                                // CharacterStatsPacket.SendCharacterDamage(character, 0, -hpHealed, 0, 0, 0, 0, null);
                                return true;
                            }

                        #endregion

                        #region AP

                        case "ap":
                            {
                                if (Args.Count > 0 && Args[0].IsNumber())
                                    character.SetAP(Args[0].GetInt16());
                                return true;
                            }

                        #endregion

                        #region SP

                        case "sp":
                            {
                                if (Args.Count > 0 && Args[0].IsNumber())
                                    character.SetSP(Args[0].GetInt16());
                                return true;
                            }

                        #endregion

                        #region AddSP

                        case "addsp":
                            {
                                if (Args.Count > 0 && Args[0].IsNumber())
                                    character.AddSP(Args[0].GetInt16());
                                return true;
                            }

                        #endregion

                        #region GiveEXP

                        case "giveexp":
                            {
                                if (Args.Count > 0 && Args[0].IsNumber())
                                    character.AddEXP(Args[0].GetInt32());
                                return true;
                            }

                        #endregion

                        #region Mesos

                        case "mesos":
                            {
                                if (Args.Count > 0 && Args[0].IsNumber())
                                    character.SetMesos(Args[0].GetInt32());
                                return true;
                            }

                        #endregion

                        #region pton/ptoff

                        case "pton":
                        case "ptoff":
                            {
                                if (Args.Count > 0)
                                {
                                    if (!character.Field.Portals.TryGetValue(Args[0], out var pt))
                                    {
                                        MessagePacket.SendNotice("Portal not found.", character);
                                    }
                                    else
                                    {
                                        var enabled = pt.Enabled = Args.Command.ToLowerInvariant() == "pton";
                                        MessagePacket.SendNotice(
                                            "Portal " + Args[0] + " is now " + (enabled ? "enabled" : "disabled"),
                                            character);
                                    }
                                }

                                return true;
                            }

                        #endregion

                        #region portals

                        case "portals":
                            {
                                var portalsInRange = character.Field.Portals.Values
                                    .OrderBy(x => new Pos(x.X, x.Y) - character.Position).Take(3).ToArray();
                                if (portalsInRange.Length == 0)
                                {
                                    MessagePacket.SendNotice("No portals found.", character);
                                }
                                else
                                {
                                    foreach (var portal in portalsInRange)
                                    {
                                        MessagePacket.SendNotice(
                                            $"Portal '{portal.Name}' id {portal.ID} script '{portal.Script}' enabled {portal.Enabled} Distance {new Pos(portal.X, portal.Y) - character.Position} ToMap {portal.ToMapID} ToName {portal.ToName} Type {portal.Type}",
                                            character);
                                    }
                                }
                                return true;
                            }

                        #endregion

                        //Event Stuff
                        #region EventReset
                        case "eventreset":
                            {
                                var ytd = new DateTime(2010, 1, 1);
                                Server.Instance.CharacterDatabase.RunQuery("UPDATE characters SET event = '" + ytd.ToString("yyyy-MM-dd HH:mm:ss") + "' WHERE ID = @charid", "@charid", character.ID);
                                MessagePacket.SendNotice("Reset event participation time.", character);
                                return true;
                            }
                        #endregion

                        #region EventHelp

                        case "event":
                        case "events":
                        case "eventhelp":
                            {
                                List<string> HelpMessages = new List<string>()
                            {
                                "============= GM Event Help Guide =============",
                                "Each event has its own help guide that can be brought up via command, and a lobby map.",
                                "In any event lobby map, use !eventdesc to display an event description message to all players.",
                                " ",
                                "AVAILABLE EVENTS:",
                                "Find the Jewel. Help: !ftjhelp Map: /map jewel",
                                "Snowball. Help: !snowballhelp Map: /map snowball for event map, !map 109060001 for lobby",
                                "Fitness. Help: !fitnesshelp Map: /map fitness",
                                "Quiz. Help: !quizhelp Map: /map quiz"
                            };

                                HelpMessages.ForEach(m => MessagePacket.SendNotice(m, character));
                                return true;
                            }

                        #endregion

                        #region eventdesc

                        case "eventdesc":
                            MapPacket.SendGMEventInstructions(character.Field);
                            MessagePacket.SendNotice("Sent event description to everybody", character);
                            return true;

                        #endregion

                        #region Find The Jewel

                        case "ftjhelp":
                            {
                                List<string> HelpMessages = new List<string>()
                            {
                                "============= Find The Jewel GM Help Guide =============",
                                "Treasure scroll item ID: 4031018. Devil Scroll: 4031019. Entry map is 109010000. Other maps are 109010100-3, 109010200-3, 109010300-3, 109010400-3.",
                                "Use '!spawn <mobid>' to create mobs at your location. Mob IDs: Super slime - 9100000, Super Jr. Necki - 9100001, Super Stirge - 9100002.",
                                "Spawn reactors by moving and using '!ftjreactorhere <id> <jewel>', where <id> is the reactor id, and <jewel> indicates if it contains the treasure: 1 for treasure, 0 for nothing",
                                "Make sure id does not exceed map limit, or client will crash. Example: !ftjreactorhere 1 1 places a reactor with id 1 and contains the treasure",
                                "Big maps will have 20 reactors (rid's 0 - 19) and small maps have 2 (rid's 0 - 1). Going past these limits will crash everything and require a server reboot",
                                "Use !ftjenable to allow players to enter the entry map (via NPC Billy) before the event starts.",
                                "Use !ftjstart to enable the portals, disably entry, and start the event. It will stop automatically when the timer runs out. Stop early with !ftjstop.",
                                "Tip: From here, the viking NPCs will take care of the rest. It may be worth going into hide and going into maps in case some try to cheat/hack.",
                                "Tip: Use AdminFly to get to platforms for mob and reactor placement, BUT, make sure to turn it off and land on the platform before placing things.",
                                "Tip: For the most authentic experience, stirges only go in the kerning map, while slimes and necki go in the others. Stirge in hene maps looks weird.",
                                "Tip: It is recommended (and GMS-like) to run the event with more than 1 hidden jewel. Put a handful, like 5-15."
                            };

                                HelpMessages.ForEach(m => MessagePacket.SendNotice(m, character));
                                return true;
                            }
                        case "ftjenable":
                            {
                                var jewelEvent = EventManager.Instance.EventInstances[EventType.Jewel];
                                if (jewelEvent.InProgress)
                                {
                                    MessagePacket.SendNoticeGMs("FTJ already in progress. Did not enable entry!");
                                }
                                else
                                {
                                    MessagePacket.SendNoticeGMs("Enabled joining FTJ. Portals Disabled until start.");
                                    jewelEvent.Prepare();
                                }
                                return true;
                            }
                        case "ftjstart":
                            {
                                var jewelEvent = EventManager.Instance.EventInstances[EventType.Jewel];
                                if (jewelEvent.InProgress)
                                {
                                    MessagePacket.SendNoticeGMs("FTJ already in progress. Did not start a new one!");
                                }
                                else
                                {
                                    MessagePacket.SendNoticeGMs(
                                        "Started FTJ. Portals enabled, and outsiders can no longer join the event.");
                                    jewelEvent.Start();
                                }
                                return true;
                            }
                        case "ftjstop":
                            {
                                MessagePacket.SendNoticeGMs(
                                    "Stopped FTJ early. Kicking everyone if event was in progress...");
                                var jewelEvent = EventManager.Instance.EventInstances[EventType.Jewel];
                                jewelEvent.Stop();
                                return true;
                            }
                        case "ftjreactorhere":
                            {
                                if (Args.Count < 2)
                                {
                                    MessagePacket.SendNotice(
                                        "Usage: !ftjreactorhere <reactor id> <jewel>, <jewel> = 0 for no treasure or 1 for treasure",
                                        character);
                                }
                                else
                                {
                                    int maxFTJReactors()
                                    {
                                        switch (character.MapID)
                                        {
                                            case 109010100:
                                            case 109010200:
                                            case 109010300:
                                            case 109010400:
                                                return 20;
                                            case 109010101:
                                            case 109010102:
                                            case 109010103:
                                            case 109010201:
                                            case 109010202:
                                            case 109010203:
                                            case 109010301:
                                            case 109010302:
                                            case 109010303:
                                            case 109010401:
                                            case 109010402:
                                            case 109010403:
                                                return 1;
                                            default:
                                                return -1;
                                        }
                                    }

                                    int rid = short.Parse(Args[0]);
                                    if (rid > maxFTJReactors() || rid < 0)
                                    {
                                        MessagePacket.SendNotice(
                                            "Exceeded max reactor limit for this map!!! Did not place.", character);
                                        return true;
                                    }

                                    Reactor r = new Reactor(character.Field, (short)rid, 0, character.Position.X,
                                        character.Position.Y, 0, 0);

                                    if (int.Parse(Args[1]) == 1)
                                    {
                                        r.ItemDrops.Add((4031018, 1));
                                    }

                                    character.Field.AddReactor(r);
                                }
                                return true;
                            }

                        #endregion

                        #region Snowball

                        case "snowballhelp":
                            {
                                List<string> HelpMessages = new List<string>()
                            {
                                "============= Snowball Event GM Help Guide =============",
                                "1. Use !snowballenable to allow entry to the hub map via event NPCs.",
                                "2. Use !snowballstart to begin the event. Teams are automatically assigned and warped to the right spots",
                                "3. To stop early, use !snowballstop. Otherwise, the event will stop and determine a winner",
                                "if the timer runs out or if a team reaches the finish line.",
                                "4. After the event ends, there will be a 10 second delay for players to be warped to win/lose maps before",
                                "another event can be started."
                            };

                                HelpMessages.ForEach(m => MessagePacket.SendNotice(m, character));
                                return true;
                            }
                        case "snowballenable":
                            {
                                var snowballEvent = EventManager.Instance.EventInstances[EventType.Snowball];
                                if (snowballEvent.InProgress)
                                {
                                    MessagePacket.SendNoticeGMs("Snowball already in progress. Did not enable entry!");
                                }
                                else
                                {
                                    MessagePacket.SendNoticeGMs(
                                        "Enabled joining Snowball. Portals Disabled until start.");
                                    snowballEvent.Prepare();
                                }
                                return true;
                            }
                        case "snowballstart":
                            {
                                var snowballEvent = EventManager.Instance.EventInstances[EventType.Snowball];
                                if (snowballEvent.InProgress)
                                {
                                    MessagePacket.SendNoticeGMs(
                                        "Snowball already in progress. Did not start a new one!");
                                }
                                else
                                {
                                    MessagePacket.SendNoticeGMs(
                                        "Started Snowball. Portals enabled, and outsiders can no longer join the event.");
                                    snowballEvent.Start();
                                }
                                return true;
                            }
                        case "snowballstop":
                            {
                                var snowballEvent = EventManager.Instance.EventInstances[EventType.Snowball];
                                snowballEvent.Stop();
                                MessagePacket.SendNoticeGMs(
                                    "Stopped Snowball early. Kicking everyone if event was in progress, and determining winner.");
                                return true;
                            }

                        #endregion

                        #region Fitness

                        case "fitnesshelp":
                        case "fithelp":
                            {
                                List<string> HelpMessages = new List<string>()
                            {
                                "============= Fitness Event GM Help Guide =============",
                                "1. Use !fitenable or !fitnessenable to allow entry to the hub map via event NPCs.",
                                "2. Use !fitstart or !fitnessstart to begin the event. Characters are warped to the starting spot automatically",
                                "3. To stop early, use !fitstop or !fitnessstop. Otherwise, the event will run until",
                                "the timer runs out. All who make it past stage 4 are automatically taken to the victory map by the portal."
                            };

                                HelpMessages.ForEach(m => MessagePacket.SendNotice(m, character));
                                return true;
                            }
                        case "fitnessenable":
                        case "fitenable":
                            {
                                var fitnessEvent = EventManager.Instance.EventInstances[EventType.Fitness];
                                if (fitnessEvent.InProgress)
                                {
                                    MessagePacket.SendNoticeGMs("Fitness already in progress. Did not enable entry!");
                                }
                                else
                                {
                                    MessagePacket.SendNoticeGMs(
                                        "Enabled joining Fitness. Portals Disabled until start.");
                                    fitnessEvent.Prepare();
                                }
                                return true;
                            }
                        case "fitnessstart":
                        case "fitstart":
                            {
                                var fitnessEvent = EventManager.Instance.EventInstances[EventType.Fitness];
                                if (fitnessEvent.InProgress)
                                {
                                    MessagePacket.SendNoticeGMs(
                                        "Fitness already in progress. Did not start a new one!");
                                }
                                else
                                {
                                    MessagePacket.SendNoticeGMs(
                                        "Started Fitness. Portals enabled, and outsiders can no longer join the event.");
                                    fitnessEvent.Start();
                                }
                                return true;
                            }
                        case "fitnessstop":
                        case "fitstop":
                            {
                                var fitnessEvent = EventManager.Instance.EventInstances[EventType.Fitness];
                                fitnessEvent.Stop();
                                MessagePacket.SendNoticeGMs(
                                    "Stopped Fitness early. Kicking everyone if event was in progress.");
                                return true;
                            }

                        #endregion

                        #region Quiz

                        case "quizhelp":
                            {
                                List<string> HelpMessages = new List<string>()
                            {
                                "============= Quiz Event GM Help Guide =============",
                                "1. Use !quizenable to allow entry to the hub map via event NPCs.",
                                "2. Use !quizstart to begin the event. Characters are warped to the starting spot automatically, and 10 questions are asked automatically.",
                                "3. To stop early, use !quizstop. Otherwise, the event will run until all questions have been asked."
                            };

                                HelpMessages.ForEach(m => MessagePacket.SendNotice(m, character));
                                return true;
                            }
                        case "quizenable":
                            {
                                var quizEvent = EventManager.Instance.EventInstances[EventType.Quiz];
                                if (quizEvent.InProgress)
                                {
                                    MessagePacket.SendNoticeGMs("Quiz already in progress. Did not enable joining!");
                                }
                                else
                                {
                                    MessagePacket.SendNoticeGMs("Enabled joining for Quiz.");
                                    quizEvent.Prepare();
                                }
                                return true;
                            }
                        case "quizstart":
                            {
                                var quizEvent = EventManager.Instance.EventInstances[EventType.Quiz];
                                if (quizEvent.InProgress)
                                {
                                    MessagePacket.SendNoticeGMs("Quiz already in progress. Did not start a new one!");
                                }
                                else
                                {
                                    MessagePacket.SendNoticeGMs(
                                        "Started Quiz. Portals enabled, and outsiders can no longer join the event.");
                                    quizEvent.Start();
                                }
                                return true;
                            }
                        case "quizstop":
                            {
                                var quizEvent = EventManager.Instance.EventInstances[EventType.Quiz];
                                ((MapleQuizEvent)quizEvent).StopEarly();
                                MessagePacket.SendNoticeGMs(
                                    "Stopped Quiz early. Kicking everyone if event was in progress.");
                                return true;
                            }

                            #endregion
                    }
                }

                if (character.GMLevel >= 3) //Admin
                {
                    switch (Args.Command.ToLowerInvariant())
                    {
                        #region Shutdown

                        case "shutdown":
                            {
                                if (!shuttingDown)
                                {
                                    int len = 10;
                                    if (Args.Count > 0 && Args[0].IsNumber())
                                    {
                                        len = Args[0].GetInt32();
                                        if (len == 0)
                                            len = 10;
                                    }

                                    MessagePacket.SendText(MessagePacket.MessageTypes.RedText,
                                        string.Format("Shutting down in {0} seconds", len), character,
                                        MessagePacket.MessageMode.ToPlayer);

                                    MasterThread.RepeatingAction.Start("Shutdown Thread",
                                        (a) => { Environment.Exit(9001); }, (long)len * 1000, 0);
                                    shuttingDown = true;
                                    return true;
                                }
                                else
                                {
                                    MessagePacket.SendText(MessagePacket.MessageTypes.RedText,
                                        "Unable to shutdown now!", character, MessagePacket.MessageMode.ToPlayer);
                                }
                                return true;
                            }

                        #endregion

                        #region Clock

                        case "clock":
                            {
                                if (Args.Count > 0 && Args[0].IsNumber())
                                    MapPacket.ShowMapTimerForMap(character.Field, Args[0].GetInt32());
                                return true;
                            }

                        #endregion

                        #region Header

                        case "header":
                            {
                                var txt = Args.Count == 0 ? "" : Args.CommandText;
                                Server.Instance.SetScrollingHeader(txt);
                                return true;
                            }
                        case "headernotice":
                            {
                                if (Args.Count == 0)
                                {
                                    var txt = Args.CommandText;
                                    Server.Instance.SetScrollingHeader(txt);
                                    MessagePacket.SendText(
                                        MessagePacket.MessageTypes.Notice,
                                        txt,
                                        null,
                                        MessagePacket.MessageMode.ToChannel
                                    );
                                }
                                return true;
                            }

                        #endregion

                        #region Packet

                        case "packet":
                            {
                                if (Args.Count > 0)
                                {
                                    Packet pw = new Packet();
                                    pw.WriteHexString(Args.CommandText);
                                    ////Console.WriteLine(packdata);
                                    character.SendPacket(pw);
                                }
                                return true;
                            }
                        case "typedpacket":
                            {
                                if (Args.Count % 2 != 0 || Args.Count == 0)
                                {
                                    MessagePacket.SendNotice(
                                        "Usage: !packet <type> <value> <type> <value> ... where type is int, short, long, string, byte",
                                        character);
                                    return true;
                                }

                                Packet pw = new Packet();

                                for (int i = 0; i < Args.Count; i += 2)
                                {
                                    switch (Args[i].Value.ToLowerInvariant())
                                    {
                                        case "opcode":
                                        case "op":
                                        case "byte":
                                            pw.WriteByte(Args[i + 1].GetByte());
                                            break;
                                        case "short":
                                            pw.WriteShort(Args[i + 1].GetInt16());
                                            break;
                                        case "int":
                                            pw.WriteInt(Args[i + 1].GetInt32());
                                            break;
                                        case "long":
                                            pw.WriteLong(Args[i + 1].GetInt64());
                                            break;
                                        case "string":
                                            pw.WriteString(Args[i + 1].Value);
                                            break;
                                        default:
                                            MessagePacket.SendNotice("Unknown type: " + Args[i].Value, character);
                                            return true;
                                    }
                                }

                                character.SendPacket(pw);
                                return true;
                            }

                        #endregion

                        #region Drop

                        case "drop":
                            {
                                try
                                {
                                    if (Args.Count > 0)
                                    {
                                        if (!Args[0].IsNumber())
                                        {
                                            MessagePacket.SendNotice("Command syntax: !drop [itemid] {amount}",
                                                character);
                                            return true;
                                        }

                                        short Amount = 1;
                                        var ItemID = Args[0].GetInt32();
                                        var Inv = (byte)(ItemID / 1000000);

                                        if (Inv <= 0 || Inv > 5 ||
                                            (!DataProvider.Equips.ContainsKey(ItemID) &&
                                             !DataProvider.Items.ContainsKey(ItemID) &&
                                             !DataProvider.Pets.ContainsKey(ItemID)))
                                        {
                                            MessagePacket.SendNotice("Item not found :(", character);
                                            return true;
                                        }

                                        if (Args.Count > 1 && Args[1].IsNumber())
                                            Amount = Args[1].GetInt16();

                                        var dropItem = BaseItem.CreateFromItemID(ItemID);
                                        dropItem.Amount = Amount;
                                        dropItem.GiveStats(ItemVariation.None);

                                        character.Field.DropPool.Create(Reward.Create(dropItem), character.ID, 0,
                                            DropType.Normal, 0, new Pos(character.Position), character.Position.X, 0,
                                            true, 0, false, true);
                                    }
                                    return true;
                                }
                                catch
                                {
                                    MessagePacket.SendNotice("Item not found :(", character);
                                    return true;
                                }
                            }
                        case "droptext":
                            {
                                if (Args.Count < 2 || !Args[0].IsNumber())
                                {
                                    MessagePacket.SendNotice("Command syntax: !droptext [0=red, 1=green] your text",
                                        character);
                                    return true;
                                }

                                var itemidNumber = 3990000;
                                var itemidAlphabet = 3991000;
                                var posTextNumbers = "";
                                var posTextAlphabet = "";

                                switch (Args[0].GetInt32())
                                {
                                    case 0: // Red
                                        posTextNumbers = "1234567890" + // red numbers
                                                         "~~~~~~~~~~" + // green numbers
                                                         "+-" +
                                                         "~~" + // green +-
                                                         "";
                                        posTextAlphabet = "abcdefghijklmnopqrstuvwxyz" +
                                                          "~~~~~~~~~~~~~~~~~~~~~~~~~~" +
                                                          "";
                                        break;
                                    case 1: // Green
                                        posTextNumbers = "~~~~~~~~~~" + // red numbers
                                                         "1234567890" + // green numbers
                                                         "~~" + // red numbers
                                                         "+-" + // green +-
                                                         "";
                                        posTextAlphabet = "~~~~~~~~~~~~~~~~~~~~~~~~~~" +
                                                          "abcdefghijklmnopqrstuvwxyz" +
                                                          "";
                                        break;
                                    default:
                                        MessagePacket.SendNotice("Command syntax: !droptext [0=red, 1=green] your text",
                                            character);
                                        return true;
                                }

                                var Rewards = string.Join(" ", Args.Args.Skip(1).Select(x => x.Value)).Select(x =>
                                {
                                    if ((x >= '0' && x <= '9') || (x == '+' || x == '-'))
                                    {
                                        return itemidNumber + posTextNumbers.IndexOf(x);
                                    }

                                    char lowerx = char.ToLower(x);
                                    if (lowerx >= 'a' && lowerx <= 'z')
                                    {
                                        return itemidAlphabet + posTextAlphabet.IndexOf(lowerx);
                                    }
                                    return 1;
                                }).Select(x => Reward.Create(BaseItem.CreateFromItemID(x, 1))).ToList();

                                var Pos = character.Position;

                                short Delay = 0;
                                int x2 = Pos.X + Rewards.Count * -10;
                                foreach (Reward Drop in Rewards)
                                {
                                    if (Drop.ItemID != 1 && character.Field.DropPool.Create(Drop, character.ID,
                                            int.MaxValue, DropType.Party, 0, Pos, x2, Delay, true, 0, true, false))
                                        continue;
                                    Delay += 200;
                                    x2 += 35;
                                }


                                return true;
                            }

                        #endregion

                        #region TogglePortal

                        case "toggleportal":
                            {
                                if (character.Field.PortalsOpen == false)
                                {
                                    MessagePacket.SendText(MessagePacket.MessageTypes.Notice,
                                        "You have toggled the portal on.", character,
                                        MessagePacket.MessageMode.ToPlayer);
                                    character.Field.PortalsOpen = true;
                                }
                                else
                                {
                                    MessagePacket.SendText(MessagePacket.MessageTypes.Notice,
                                        "You have toggled the portal off.", character,
                                        MessagePacket.MessageMode.ToPlayer);
                                    character.Field.PortalsOpen = false;
                                }
                                return true;
                            }

                        #endregion

                        #region PTInvite

                        case "ptinvite":
                            {
                                if (Args.Count > 0)
                                {
                                    string other = Args[0].Value.ToLower();
                                    foreach (KeyValuePair<int, Character> kvp in Server.Instance.CharacterList)
                                    {
                                        if (kvp.Value.Name.ToLower() == other)
                                        {
                                            //PartyPacket.partyInvite(kvp.Value);
                                            MessagePacket.SendText(MessagePacket.MessageTypes.RedText, "Hey", kvp.Value,
                                                MessagePacket.MessageMode.ToPlayer);
                                        }
                                    }
                                }
                                return true;
                            }

                        #endregion

                        #region MakeDonator

                        case "makedonator":
                            {
                                if (Args.Count > 0)
                                {
                                    string name = Args[0].Value.ToLower();
                                    int derp = Server.Instance.CharacterDatabase.UserIDByCharacterName(name);
                                    if (derp > 1)
                                    {
                                        Server.Instance.CharacterDatabase.RunQuery(
                                            $"UPDATE users SET donator = 1 WHERE ID = {derp}");
                                        MessagePacket.SendText(MessagePacket.MessageTypes.RedText,
                                            $"'{name} ' is now set as a donator on the AccountID : {derp}", character,
                                            MessagePacket.MessageMode.ToPlayer);
                                    }
                                    else if (derp <= 1)
                                        MessagePacket.SendText(MessagePacket.MessageTypes.RedText,
                                            "You have entered an incorrect name.", character,
                                            MessagePacket.MessageMode.ToPlayer);
                                }
                                return true;
                            }

                        #endregion

                        #region Participate

                        case "participate":
                            {
                                if (Args.Count > 0)
                                {
                                    string name = Args[0].Value.ToLower();
                                    /**
                                            if (EventManager.hasParticipated(name) == true)
                                            {
                                                MessagePacket.SendText(MessagePacket.MessageTypes.RedText, "HAS PARTICIPATED DUN DUN DUN.", character, MessagePacket.MessageMode.ToPlayer);
                                            }
                                            else if (EventManager.hasParticipated(name) == false)
                                            {
                                                MessagePacket.SendText(MessagePacket.MessageTypes.RedText, "NOT PARTICIPATED DUN DUN DUNNNN.", character, MessagePacket.MessageMode.ToPlayer);
                                            }
                                             * */
                                }
                                return true;
                            }

                        #endregion

                        #region GetID2

                        case "getid2":
                            {
                                if (Args.Count > 0)
                                {
                                    string name = Args[0].Value.ToLower();
                                    int id = Server.Instance.CharacterDatabase.CharacterIdByName(name);
                                    string name2 = character.Name;
                                    MessagePacket.SendText(MessagePacket.MessageTypes.RedText, $"ID is '{id}'.",
                                        character, MessagePacket.MessageMode.ToPlayer);
                                }
                                return true;
                            }

                        #endregion

                        #region Save

                        case "save":
                            {
                                character.Save();
                                MessagePacket.SendNotice("Saved!", character);
                                return true;
                            }

                        #endregion

                        #region SaveAll

                        case "saveall":
                            {
                                foreach (var kvp in Server.Instance.CharacterList)
                                {
                                    MasterThread.Instance.AddCallback(x =>
                                    {
                                        kvp.Value.Save();
                                        MessagePacket.SendNotice(kvp.Value.Name + " saved at : " + DateTime.Now + ".",
                                            character);
                                    }, "Saving message for " + kvp.Key);
                                }
                                return true;
                            }

                        #endregion

                        #region PetName

                        case "petname":
                            {
                                if (Args.Count > 0)
                                {
                                    string newname = Args[0].Value;
                                    if (newname.Length < 14)
                                    {
                                        //character.Pets.ChangePetname(newname);
                                        MessagePacket.SendNotice("Changed name lol", character);
                                    }
                                    else
                                        MessagePacket.SendNotice("Cannot change the name! It's too long :(", character);
                                }
                                return true;
                            }

                        #endregion

                        #region VAC

                        case "vac":
                            {
                                bool petLoot = false;
                                bool mobLoot = false;
                                if (Args.Count > 0)
                                {
                                    switch (Args[0].Value)
                                    {
                                        case "pet":
                                            petLoot = true;
                                            break;
                                        case "mob":
                                            mobLoot = true;
                                            break;
                                    }
                                }

                                var mobs = character.Field.Mobs.Values.ToList();
                                if (mobLoot && mobs.Count == 0) mobLoot = false;

                                var dropBackup = new Dictionary<int, Drop>(character.Field.DropPool.Drops);
                                foreach (var kvp in dropBackup)
                                {
                                    if (kvp.Value == null)
                                        continue;

                                    Drop drop = kvp.Value;
                                    short pickupAmount = drop.Reward.Amount;
                                    if (drop.Reward.Mesos)
                                    {
                                        character.AddMesos(drop.Reward.Drop);
                                    }
                                    else
                                    {
                                        if (character.Inventory.AddItem2(drop.Reward.GetData()) == drop.Reward.Amount)
                                        {
                                            continue;
                                        }
                                    }
                                    CharacterStatsPacket.SendGainDrop(character, drop.Reward.Mesos, drop.Reward.Drop,
                                        pickupAmount);
                                    if (mobLoot)
                                    {
                                        var mob = mobs[(int)(Rand32.Next() % mobs.Count)];
                                        character.Field.DropPool.RemoveDrop(drop, RewardLeaveType.Remove, mob.SpawnID);
                                    }
                                    else
                                    {
                                        character.Field.DropPool.RemoveDrop(drop,
                                            petLoot ? RewardLeaveType.PetPickup : RewardLeaveType.FreeForAll,
                                            character.ID);
                                    }
                                }
                                return true;
                            }

                        #endregion

                        #region MobInfo

                        case "mobinfo":
                            {
                                var Field = character.Field;
                                var Capacity = Field.GetCapacity();
                                var boosted = Field.IsBoostedMobGen;
                                var RemainCapacity = Capacity - Field.Mobs.Count;
                                MessagePacket.SendNotice(
                                    $"Min Limit {Field.MobCapacityMin}, Max Limit {Field.MobCapacityMax}, Count {Field.Mobs.Count}",
                                    character);
                                MessagePacket.SendNotice($"Capacity {Capacity}, RemainCapacity {RemainCapacity}",
                                    character);
                                MessagePacket.SendNotice(
                                    $"Boosted {boosted}, Boost trigger @ {Field.MobCapacityMin / 2} players (cur {Field.Characters.Count})",
                                    character);
                                return true;
                            }

                        #endregion

                        #region MobChase

                        case "mobchase":
                            {
                                if (Args.Count > 0)
                                {
                                    string victim = Args[0].Value.ToLower();
                                    Character who = Server.Instance.GetCharacter(victim);

                                    if (who != null)
                                    {
                                        who.Field.Mobs.ForEach(x => x.Value.SetController(who, true));
                                    }
                                    else
                                        MessagePacket.SendText(MessagePacket.MessageTypes.RedText,
                                            "You have entered an incorrect name.", character,
                                            MessagePacket.MessageMode.ToPlayer);
                                }
                                return true;
                            }

                        #endregion

                        #region npcreload

                        case "npcreload":
                        case "reloadnpc":
                        case "scriptreload":
                        case "reloadscript":
                            {
                                if (Args.Count > 0)
                                {
                                    var scriptName = Args[0];

                                    var fileName = Server.Instance.GetScriptFilename(scriptName);
                                    if (fileName == null)
                                    {
                                        MessagePacket.SendNotice(
                                            "Could not find a script with the name " + scriptName + "!", character);
                                        return true;
                                    }

                                    var toAllChannels = Args.Count > 1 && Args[1].GetBool();
                                    if (toAllChannels)
                                    {
                                        var p = new Packet(ISClientMessages.BroadcastPacketToGameservers);
                                        p.WriteByte((byte)ISServerMessages.ReloadNPCScript);
                                        p.WriteString(scriptName);
                                        Server.Instance.CenterConnection.SendPacket(p);

                                        MessagePacket.SendNotice("Sent request to reload the script to all channels.",
                                            character);
                                    }
                                    else
                                    {
                                        if (Server.Instance.ForceCompileScriptfile(
                                                fileName,
                                                (script) =>
                                                {
                                                    MessagePacket.SendNotice(
                                                        "Error while recompiling " + scriptName +
                                                        ". See logs. Script: " + script,
                                                        character
                                                    );
                                                }) != null)
                                        {
                                            MessagePacket.SendNotice("Recompiled the script.", character);
                                        }
                                    }
                                }
                                else
                                {
                                    MessagePacket.SendNotice(
                                        $"Usage: !{Args.Command} <script name or id> (1 here for all channels)",
                                        character);
                                }
                                return true;
                            }

                        #endregion

                        #region reload cashshop data

                        case "csreload":
                        case "cashshopreload":
                        case "reloadcs":
                        case "reloadcashshop":
                            {
                                var p = new Packet(ISClientMessages.BroadcastPacketToShopservers);
                                p.WriteByte((byte)ISServerMessages.ReloadCashshopData);
                                Server.Instance.CenterConnection.SendPacket(p);

                                MessagePacket.SendNotice("Sent request to reload the cashshop data.", character);
                            }
                            return true;

                        #endregion

                        #region reload world events

                        case "reloadevents":
                        case "eventsreload":
                            {
                                var p = new Packet(ISClientMessages.ReloadEvents);
                                Server.Instance.CenterConnection.SendPacket(p);

                                MessagePacket.SendNotice("Sent request to reload events.", character);
                            }
                            return true;

                        #endregion

                        #region Reactors

                        case "reactor":
                            {
                                if (Args.Count < 6)
                                {
                                    MessagePacket.SendNotice("Usage: <short id>, <byte state>, <short x>, <short y>, <bool z>, <byte zm>, [optional] item id", character);
                                }
                                else
                                {
                                    Reactor r = new Reactor(character.Field, short.Parse(Args[0]), byte.Parse(Args[1]), short.Parse(Args[2]), short.Parse(Args[3]), byte.Parse(Args[4]), byte.Parse(Args[5]));
                                    Program.MainForm.LogAppend("Added reactor with ID " + r.ID + " on map " + character.Field.ID);

                                    if (Args.Count > 6)
                                        r.ItemDrops.Add((int.Parse(Args[6]), 1));
                                    character.Field.AddReactor(r);
                                }
                                return true;
                            }

                            #endregion
                    }
                }

                MessagePacket.SendNotice($"Unknown command: {text}", character);
                return true;
            }
            catch (Exception ex)
            {
                ////Console.WriteLine(ex.ToString());
                MessagePacket.SendNotice("Something went wrong while processing this command.", character);
                if (character.IsGM)
                {
                    MessagePacket.SendNotice(ex.ToString(), character);
                }
                return true;
            }
        }




        public static void HandleAdminCommand(Character chr, Packet packet)
        {
            if (chr.AssertForHack(!chr.IsGM, "Tried to use slash GM command while not GM")) return;
            //  41 12 1E 00 00 00 
            byte opcode = packet.ReadByte();
            switch (opcode)
            {
                case 0x00: // /create (no idea what it does)
                    break;
                case 0x02:
                    {
                        // /exp (int amount) 
                        int exp = packet.ReadInt();
                        chr.AddEXP(exp);
                        break;
                    }
                case 0x03:
                    {
                        // /ban (user) (permanantly)
                        string name = packet.ReadString();
                        int charid = Server.Instance.CharacterDatabase.CharacterIdByName(name);
                        int ID = Server.Instance.CharacterDatabase.UserIDByCharacterName(name);
                        using (MySqlDataReader data = Server.Instance.CharacterDatabase.RunQuery("SELECT * FROM characters WHERE name = '" + MySqlHelper.EscapeString(name) + "'") as MySqlDataReader)
                        {
                            if (data.HasRows)
                            {
                                if (!Server.Instance.CharacterList.ContainsKey(charid))
                                {
                                    if (Server.Instance.CharacterList.ContainsKey(charid))
                                    {
                                        Character victim = Server.Instance.GetCharacter(name);
                                        victim.Player.Socket.Disconnect();
                                        Server.Instance.CharacterDatabase.RunQuery("UPDATE users SET ban_reason = 8 WHERE ID = " + ID); //8 : permanent ban
                                        AdminPacket.BanCharacterMessage(chr);
                                    }
                                    else
                                    {
                                        Server.Instance.CharacterDatabase.RunQuery("UPDATE users SET ban_reason = 0 WHERE ID = " + ID);
                                        AdminPacket.BanCharacterMessage(chr);
                                    }
                                }
                            }
                            else
                            {
                                AdminPacket.InvalidNameMessage(chr);
                            }
                        }
                        break;
                    }
                case 0x04:
                    {
                        string name = packet.ReadString();
                        byte type = packet.ReadByte();
                        int duration = packet.ReadInt();
                        string comment = packet.ReadString();

                        int charid = Server.Instance.CharacterDatabase.CharacterIdByName(name);
                        int ID = Server.Instance.CharacterDatabase.UserIDByCharacterName(name);
                        using (MySqlDataReader data = Server.Instance.CharacterDatabase.RunQuery("SELECT * FROM characters WHERE name = '" + MySqlHelper.EscapeString(name) + "'") as MySqlDataReader)
                        {
                            if (data.HasRows)
                            {
                                if (!Server.Instance.CharacterList.ContainsKey(charid))
                                {
                                    if (Server.Instance.CharacterList.ContainsKey(charid))
                                    {
                                        Character victim = Server.Instance.GetCharacter(name);
                                        victim.Player.Socket.Disconnect();
                                        Server.Instance.CharacterDatabase.RunQuery("UPDATE users SET ban_reason = " + type + " WHERE ID = " + ID); //8 : permanent ban
                                        AdminPacket.BanCharacterMessage(chr);
                                    }
                                    else
                                    {
                                        Server.Instance.CharacterDatabase.RunQuery("UPDATE users SET ban_reason = " + type + " WHERE ID = " + ID);
                                        AdminPacket.BanCharacterMessage(chr);
                                    }
                                }
                            }
                            else
                            {
                                AdminPacket.InvalidNameMessage(chr);
                            }
                        }
                        break;
                    }

                case 0x11: //not sure what this is supposed to do. The only thing that comes after the received string is an INT(0). the format is /send (something) (something)
                    {
                        // /send (user) (mapid)
                        string To = packet.ReadString();
                        break;
                    }
                case 0x12:
                    {
                        // /snow
                        TimeSpan time = new TimeSpan(0, packet.ReadInt(), 0);
                        chr.Field.MakeWeatherEffect(2090000, "", time, true);
                        //FileWriter.WriteLine(@"Logs\Admin Command Log.txt", string.Format("[{0}] Character {1} ({2}, UID: {3}) used admin command: /snow {4}", DateTime.Now.ToString(), chr.ID, chr.Name, chr.UserID, time.TotalMinutes));
                        break;
                    }
                case 0x0F:
                    {
                        // /hide 0/1
                        bool doHide = packet.ReadBool();
                        //if (doHide == chr.GMHideEnabled) return;
                        chr.SetHide(doHide, false);

                        //FileWriter.WriteLine(@"Logs\Admin Command Log.txt", string.Format("[{0}] Character {1} ({2}, UID: {3}) used admin command: /hide {4}", DateTime.Now.ToString(), chr.ID, chr.Name, chr.UserID, doHide));
                        break;
                    }
                case 0x0A:
                    {
                        // /block NAME TIME REASON
                        string name = packet.ReadString();
                        byte reason = packet.ReadByte();
                        int len = packet.ReadInt();
                        string reasonmsg = packet.ReadString();
                        break;
                    }
                default:
                    {
                        ////Console.WriteLine("Unhandled Command! Opcode: " + opcode);
                        //FileWriter.WriteLine(@"Logs\Admin Command Log.txt", string.Format("[{0}] Character {1} ({2}, UID: {3}) tried using an admin command. Packet: {4}", DateTime.Now.ToString(), chr.ID, chr.Name, chr.UserID, packet.ToString()));
                        break;
                    }
            }
        }

        public static void HandleAdminCommandLog(Character chr, Packet packet)
        {
            // 42 04 00 2F 70 6F 73 
            packet.ReadString();
        }

        public class CommandArgs
        {
            public string PlainText;
            public char Sign;
            public string Command;
            public string CommandText;
            public List<CommandArg> Args;

            public int Count => Args?.Count ?? 0;
            public CommandArg this[int Index] => GetArg(Index);

            public CommandArgs(string text)
            {
                var SplitText = text.Split(' ');
                PlainText = text;
                Sign = text[0];
                Command = SplitText[0].Remove(0, 1);
                CommandText = PlainText.Remove(0, 1).Replace($"{Command} ", "");
                SetArgs(SplitText);
            }

            public void Regenerate(string text)
            {
                SetArgs(text.Split(' '));
            }

            public CommandArg GetArg(int Index)
            {
                if (Index >= 0 && Index < Args.Count)
                    return Args[Index];
                else
                    throw new IndexOutOfRangeException($"Index must be greater then 0 and less then {Args.Count}.");
            }

            public void SetArgs(string[] Strings)
            {
                if (Args == null)
                    Args = new List<CommandArg>();
                else
                    Args.Clear();

                for (int i = 1; i < Strings.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(Strings[i]) && Strings[i] != Command)
                        Args.Add(new CommandArg(Strings[i]));
                }
            }
        }

        public class CommandArg
        {
            public string Value;

            public CommandArg(string Value)
            {
                this.Value = Value;
            }

            public bool IsNumber()
            {
                foreach (var Char in Value)
                {
                    if (Char < '0' || Char > '9')
                        return false;
                }
                return true;
            }

            public byte GetByte()
            {
                byte Result = 0;
                byte.TryParse(Value, out Result);

                return Result;
            }

            public short GetInt16()
            {
                short Result = 0;
                short.TryParse(Value, out Result);

                return Result;
            }

            public int GetInt32()
            {
                int Result = 0;
                int.TryParse(Value, out Result);

                return Result;
            }

            public long GetInt64()
            {
                long Result = 0;
                long.TryParse(Value, out Result);

                return Result;
            }

            public bool GetBool()
            {
                switch (Value.ToLowerInvariant())
                {
                    case "true":
                    case "t":
                    case "yes":
                    case "y":
                    case "1":
                        return true;
                    default:
                        return false;
                }
            }

            public static implicit operator string(CommandArg Arg) => Arg.Value;

            public override string ToString()
            {
                return Value;
            }
        }
    }
}