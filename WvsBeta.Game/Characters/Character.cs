using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using log4net;
using MySql.Data.MySqlClient;
using WvsBeta.Common;
using WvsBeta.Common.Sessions;

namespace WvsBeta.Game
{
    public partial class Character : CharacterBase, IFieldObj
    {
        private static ILog _characterLog = LogManager.GetLogger("CharacterLog");

        // WorldServer Event (EXP rate)
        public static int ms_nIncExpRate_WSE = 100;

        // Regular EXP rate
        public static int ms_nIncEXPRate => (int)(Server.Instance.RateMobEXP * 100);

        // When married
        public static int ms_nIncExpRate_Wedding = 100;

        // User-specific exp rate
        public double m_dIncExpRate = 1.0;
        // No exp rate ticket
        public double m_dIncDropRate = 1.0;
        public double m_dIncDropRate_Ticket = 1.0;

        public static int ms_nPartyBonusEventRate = 0;


        public int UserID { get; set; }
        public short MapChair { get; set; } = -1;
        private DateTime LastSavepoint;
        private long LastPlayTimeSave;

        public Map Field { get; set; }
        public override int MapID => Field.ID;
        public byte MapPosition { get; set; }
        public byte PortalCount { get; set; } = 0;

        public bool GMHideEnabled { get; private set; }
        public bool Donator { get; private set; }
        public bool BetaPlayer { get; private set; }

        public MiniRoomBase Room { get; set; }
        public byte RoomSlotId { get; set; }
        public bool UsingTimer { get; set; }

        public CharacterInventory Inventory { get; private set; }
        public CharacterSkills Skills { get; private set; }
        public CharacterBuffs Buffs { get; private set; }
        public CharacterPrimaryStats PrimaryStats { get; private set; }
        public Rand32 CalcDamageRandomizer { get; private set; }
        public Rand32 RndActionRandomizer { get; private set; }
        public CharacterSummons Summons { get; private set; }
        public CharacterStorage Storage { get; private set; }
        public CharacterQuests Quests { get; private set; }
        public CharacterVariables Variables { get; private set; }
        public CharacterGameStats GameStats { get; private set; }
        public long PetCashId { get; set; }
        public long PetLastInteraction { get; set; }

        public Ring pRing { get; set; }

        public List<int> Wishlist { get; private set; }

        public NpcChatSession NpcSession { get; set; } = null;
        public int ShopNPCID { get; set; } = 0;
        public int TrunkNPCID { get; set; } = 0;

        public Player Player { get; set; }

        public bool Undercover { get; set; }
        public string ImitatorName { get; private set; }
        public DateTime MutedUntil { get; set; }
        public byte MuteReason { get; set; }

        public long LastChat { get; set; }

        public bool IsInNPCChat => ShopNPCID != 0 || TrunkNPCID != 0 || NpcSession != null;
        public bool IsInMiniRoom => Room != null;
        public bool CanAttachAdditionalProcess
        {
            get
            {
                var ret = !IsInNPCChat && !IsInMiniRoom;

                if (ret == false)
                {
                    HackLog.Warn($"CanAttachAdditionalProcess: {ShopNPCID}, {TrunkNPCID}, {NpcSession}, {Room}");
                }
                return ret;
            }
        }

        public int DoorMapId = Constants.InvalidMap;
        public long tLastDoor = 0;


        public Character(int CharacterID)
        {
            ID = CharacterID;
        }

        public bool IsAFK => (MasterThread.CurrentTime - LastMove) > 120000 &&
                             (MasterThread.CurrentTime - LastChat) > 120000;

        public void SendPacket(byte[] pw)
        {
            Player?.Socket?.SendData(pw);
        }

        public void SendPacket(Packet pw)
        {
            Player?.Socket?.SendPacket(pw);
        }

        public PetItem GetSpawnedPet()
        {
            if (PetCashId == 0) return null;
            return Inventory.GetItemByCashID(PetCashId, 5) as PetItem;
        }

        public void HandleDeath()
        {
            HackLog.Info("Player will be moved back to town/return map");
            ModifyHP(50, false);

            // Remove all buffs
            PrimaryStats.Reset(true);

            // There's only 1 map that has this. Its the pharmacy map in kerning
            if (Field.ReturnMap == Constants.InvalidMap)
            {
                ChangeMap(Field.ID);
            }
            else
            {
                ChangeMap(Field.ReturnMap);
            }
        }


        public void SetIncExpRate()
        {
            var currentDateTime = MasterThread.CurrentDate;
            SetIncExpRate(currentDateTime.Day, currentDateTime.Hour);
        }

        public void SetIncExpRate(int day, int hour)
        {
            const int Exp_Normal = 100;
            const int Exp_Premium = 100;
            const int Drop_Normal = 100;
            const int Drop_Premium = 100;

            bool isPremium = false;
            double expRate = 1.0;
            double dropRate = 1.0;

            // TODO: check inventories

            if (ms_nIncEXPRate != 100)
            {
                // Check player range, we don't care lol
                expRate = ms_nIncEXPRate * expRate * 0.01;
            }

            if (isPremium)
            {
                expRate *= 1.2;
            }
            
            // Check inventories for droprate tickets

            if (isPremium)
            {
                expRate *= Exp_Premium * 0.01;
                dropRate *= Drop_Premium * 0.01;
            }
            else
            {
                expRate *= Exp_Normal * 0.01;
                dropRate *= Drop_Normal * 0.01;
            }

            m_dIncDropRate_Ticket = 1.0;

            m_dIncDropRate = dropRate;
            m_dIncExpRate = expRate;

            Trace.WriteLine($"Rates: EXP {m_dIncExpRate}, Drop {m_dIncDropRate}, Drop ticket {m_dIncDropRate_Ticket}");
        }

        public bool IsShownTo(IFieldObj Object)
        {
            if (GMHideEnabled)
            {
                var player = Object as Character;
                if (player != null && player.IsGM) return true;
                return false;
            }

            return true;
        }

        public void CleanupInstances()
        {
            if (Room != null)
            {
                if (Room.Type == MiniRoomBase.RoomType.PersonalShop)
                {
                    Room.RemovePlayerFromShop(this);
                }
                else
                {
                    Room.RemovePlayer(this, 0);
                }
                Room = null;
            }

            ShopNPCID = 0;
            TrunkNPCID = 0;

            NpcSession?.Stop();
            NpcSession = null;
        }

        public void TryActivateHide()
        {
            if (!IsGM || GMHideEnabled) return;

            var hideSkill = Constants.Gm.Skills.Hide;
            // Make sure that the user has the skill
            if (Skills.GetSkillLevel(hideSkill) == 0)
                Skills.AddSkillPoint(hideSkill);

            if (!Undercover)
            {
                Buffs.AddBuff(hideSkill, 1);
                SetHide(true, true);
            }
        }

        public void Save()
        {
            if (ImitatorName != null) return;

            _characterLog.Debug("Saving character...");
            Server.Instance.CharacterDatabase.RunTransaction(comm =>
            {
                var saveQuery = new StringBuilder();

                saveQuery.Append("UPDATE characters SET ");
                saveQuery.Append("skin = '" + Skin + "', ");
                saveQuery.Append("hair = '" + Hair + "', ");
                saveQuery.Append("gender = '" + Gender + "', ");
                saveQuery.Append("eyes = '" + Face + "', ");
                saveQuery.Append("map = '" + MapID + "', ");
                saveQuery.Append("pos = '" + MapPosition + "', ");
                saveQuery.Append("level = '" + PrimaryStats.Level + "', ");
                saveQuery.Append("job = '" + PrimaryStats.Job + "', ");
                saveQuery.Append("chp = '" + PrimaryStats.HP + "', ");
                saveQuery.Append("cmp = '" + PrimaryStats.MP + "', ");
                saveQuery.Append("mhp = '" + PrimaryStats.MaxHP + "', ");
                saveQuery.Append("mmp = '" + PrimaryStats.MaxMP + "', ");
                saveQuery.Append("`int` = '" + PrimaryStats.Int + "', ");
                saveQuery.Append("dex = '" + PrimaryStats.Dex + "', ");
                saveQuery.Append("str = '" + PrimaryStats.Str + "', ");
                saveQuery.Append("luk = '" + PrimaryStats.Luk + "', ");
                saveQuery.Append("ap = '" + PrimaryStats.AP + "', ");
                saveQuery.Append("sp = '" + PrimaryStats.SP + "', ");
                saveQuery.Append("fame = '" + PrimaryStats.Fame + "', ");
                saveQuery.Append("exp = '" + PrimaryStats.EXP + "', ");
                saveQuery.Append($"pet_cash_id = 0x{PetCashId:X16},");
                // saveQuery.Append($"playtime = playtime + 0x{(MasterThread.CurrentTime - LastPlayTimeSave):X16}, ");
                saveQuery.Append("last_savepoint = '" + LastSavepoint.ToString("yyyy-MM-dd HH:mm:ss") + "' ");
                saveQuery.Append("WHERE ID = " + ID);

                comm.CommandText = saveQuery.ToString();
                comm.ExecuteNonQuery();
            }, Program.MainForm.LogAppend);

            LastPlayTimeSave = MasterThread.CurrentTime;

            Server.Instance.CharacterDatabase.RunTransaction(comm =>
            {
                comm.CommandText = "DELETE FROM character_wishlist WHERE charid = " + ID;
                comm.ExecuteNonQuery();

                if (Wishlist.Count > 0)
                {
                    var wishlistQuery = new StringBuilder();

                    wishlistQuery.Append("INSERT INTO character_wishlist VALUES ");
                    wishlistQuery.Append(string.Join(", ", Wishlist.Select(serial => "(" + ID + ", " + serial + ")")));

                    comm.CommandText = wishlistQuery.ToString();
                    comm.ExecuteNonQuery();
                }
            }, Program.MainForm.LogAppend);

            Inventory.SaveInventory();
            Inventory.SaveCashItems(null);
            Skills.SaveSkills();
            Storage.Save();
            Quests.SaveQuests();
            Variables.Save();
            GameStats.Save();

            _characterLog.Debug("Saving finished!");
        }

        public void PartyHPUpdate()
        {
            if (PartyID == 0) return;

            Field
                .GetInParty(PartyID)
                .Where(p => p.ID != ID)
                .ForEach(p => p.SendPacket(PartyPacket.SendHpUpdate(PrimaryStats.HP, PrimaryStats.GetMaxHP(), ID)));

        }

        public void FullPartyHPUpdate()
        {
            if (PartyID == 0) return;

            Field
                .GetInParty(PartyID)
                .Where(p => p.ID != ID)
                .Select(p => Tuple.Create(this, p))
                .ForEach(pair =>
                {
                    pair.Item1.SendPacket(PartyPacket.SendHpUpdate(pair.Item2.PrimaryStats.HP, pair.Item2.PrimaryStats.GetMaxHP(), pair.Item2.ID));
                    pair.Item2.SendPacket(PartyPacket.SendHpUpdate(pair.Item1.PrimaryStats.HP, pair.Item1.PrimaryStats.GetMaxHP(), pair.Item1.ID));
                });
        }

        public enum LoadFailReasons
        {
            None,
            UnknownCharacter,
            NotFromPreviousIP,
            UserAlreadyOnline,
            TransitionTimeout
        }

        public LoadFailReasons Load(string IP)
        {

            var imitateId = RedisBackend.Instance.GetImitateID(ID);
            var imitating = imitateId.HasValue;
            var originalId = ID;
            if (imitating)
            {
                ID = imitateId.Value;
                _characterLog.Debug($"Loading character {ID} from IP {IP}... (IMITATION from ID {originalId})");
            }
            else
            {
                _characterLog.Debug($"Loading character {ID} from IP {IP}...");
            }

            // Initial load

            using (var data = (MySqlDataReader)Server.Instance.CharacterDatabase.RunQuery(
                "SELECT " +
                "characters.*, users.admin, users.superadmin, users.donator, users.beta, users.last_ip, users.online " +
                "FROM characters " +
                "LEFT JOIN users ON users.id = characters.userid " +
                "WHERE characters.id = @id",
                "@id", originalId))
            {
                if (!data.Read())
                {
                    _characterLog.Debug("Loading failed: unknown character.");
                    return LoadFailReasons.UnknownCharacter;
                }

                if (data.GetString("last_ip") != IP && !imitating)
                {
#if DEBUG
                    Program.MainForm.LogAppend("Allowed player " + this.ID +
                                               " to log in from different IP because source is running in debug mode!");
#else
                    _characterLog.Debug("Loading failed: not from previous IP.");
                    return LoadFailReasons.NotFromPreviousIP;
#endif
                }
                UserID = data.GetInt32("userid");
                Name = data.GetString("name");
                GMLevel = data.GetByte("admin");
                Donator = data.GetBoolean("donator");
                BetaPlayer = data.GetBoolean("beta");


                if (imitating) ImitatorName = Name;
                else ImitatorName = null;
            }

            var tmpUserId = UserID;

            using (var data = (MySqlDataReader)Server.Instance.CharacterDatabase.RunQuery(
                "SELECT " +
                "characters.*, users.last_ip, users.online, users.quiet_ban_expire, users.quiet_ban_reason " +
                "FROM characters " +
                "LEFT JOIN users ON users.id = characters.userid " +
                "WHERE characters.id = @id",
                "@id", ID))
            {
                if (!data.Read())
                {
                    _characterLog.Debug("Loading failed: unknown character.");
                    if (imitating)
                    {
                        // Reset!
                        RedisBackend.Instance.SetImitateID(originalId, 0);
                    }

                    return LoadFailReasons.UnknownCharacter;
                }

                UserID = data.GetInt32("userid"); // For cashitem loading
                Name = data.GetString("name");

                Gender = data.GetByte("gender");
                Skin = data.GetByte("skin");
                Hair = data.GetInt32("hair");
                Face = data.GetInt32("eyes");
                PetCashId = data.GetInt64("pet_cash_id");
                MutedUntil = data.GetDateTime("quiet_ban_expire");
                MuteReason = data.GetByte("quiet_ban_reason");
                LastSavepoint = data.GetDateTime("last_savepoint");
                LastPlayTimeSave = MasterThread.CurrentTime;

                var _mapId = data.GetInt32("map");

                Map field;
                if (!DataProvider.Maps.TryGetValue(_mapId, out field))
                {
                    Program.MainForm.LogAppend(
                        "The map of {0} is not valid (nonexistant)! Map was {1}. Returning to 0", ID, _mapId);
                    field = DataProvider.Maps[0];
                    MapPosition = 0;
                }
                Field = field;

                // Push back player when there's a forced return value
                if (field.ForcedReturn != Constants.InvalidMap)
                {
                    _mapId = field.ForcedReturn;
                    if (!DataProvider.Maps.TryGetValue(_mapId, out field))
                    {
                        Program.MainForm.LogAppend(
                            "The map of {0} is not valid (nonexistant)! Map was {1}. Returning to 0", ID, _mapId);
                        // Note: using Field here
                        Field = DataProvider.Maps[0];
                    }
                    else
                    {
                        Field = DataProvider.Maps[_mapId];
                    }
                    MapPosition = 0;
                }
                else
                {
                    MapPosition = (byte)data.GetInt16("pos");
                }

                // Select portal to spawn on.
                {
                    Portal portal = Field.SpawnPoints.Find(x => x.ID == MapPosition);
                    if (portal == null) portal = Field.GetRandomStartPoint();
                    Position = new Pos(portal.X, portal.Y);
                }
                Stance = 0;
                Foothold = 0;

                CalcDamageRandomizer = new Rand32();
                RndActionRandomizer = new Rand32();


                PrimaryStats = new CharacterPrimaryStats(this)
                {
                    Level = data.GetByte("level"),
                    Job = data.GetInt16("job"),
                    Str = data.GetInt16("str"),
                    Dex = data.GetInt16("dex"),
                    Int = data.GetInt16("int"),
                    Luk = data.GetInt16("luk"),
                    HP = data.GetInt16("chp"),
                    MaxHP = data.GetInt16("mhp"),
                    MP = data.GetInt16("cmp"),
                    MaxMP = data.GetInt16("mmp"),
                    AP = data.GetInt16("ap"),
                    SP = data.GetInt16("sp"),
                    EXP = data.GetInt32("exp"),
                    Fame = data.GetInt16("fame"),
                    BuddyListCapacity = data.GetInt32("buddylist_size")
                };

                // Make sure we don't update too many times
                lastSaveStep = CalculateSaveStep();
            }

            Inventory = new CharacterInventory(this);
            Inventory.LoadInventory();

            UserID = tmpUserId;

            Ring.LoadRings(this);

            Skills = new CharacterSkills(this);
            Skills.LoadSkills();

            Storage = new CharacterStorage(this);
            Storage.Load();

            Buffs = new CharacterBuffs(this);

            Summons = new CharacterSummons(this);

            Quests = new CharacterQuests(this);
            Quests.LoadQuests();

            Variables = new CharacterVariables(this);
            Variables.Load();

            GameStats = new CharacterGameStats(this);
            GameStats.Load();

            Wishlist = new List<int>();
            using (var data = (MySqlDataReader)Server.Instance.CharacterDatabase.RunQuery("SELECT serial FROM character_wishlist WHERE charid = " + ID))
            {
                while (data.Read())
                {
                    Wishlist.Add(data.GetInt32(0));
                }
            }

            // Loading done, switch back ID
            ID = originalId;

            InitDamageLog();

            SetIncExpRate();

            var muteTimeSpan = RedisBackend.Instance.GetCharacterMuteTime(ID);
            if (muteTimeSpan.HasValue)
                HacklogMuted = MasterThread.CurrentDate.Add(muteTimeSpan.Value);
            else
                HacklogMuted = DateTime.MinValue;

            Undercover = RedisBackend.Instance.IsUndercover(ID);

            RedisBackend.Instance.SetPlayerOnline(
                UserID,
                Server.Instance.GetOnlineId()
            );

            _characterLog.Debug("Loaded!");
            return LoadFailReasons.None;
        }


        public void SetupLogging()
        {
            ThreadContext.Properties["UserID"] = UserID;
            ThreadContext.Properties["CharacterID"] = ID;
            ThreadContext.Properties["CharacterName"] = Name;
            ThreadContext.Properties["MapID"] = MapID;
        }

        public static void RemoveLogging()
        {
            ThreadContext.Properties.Remove("UserID");
            ThreadContext.Properties.Remove("CharacterID");
            ThreadContext.Properties.Remove("CharacterName");
            ThreadContext.Properties.Remove("MapID");
        }

    }
}