using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using WvsBeta.Common;
using WvsBeta.Common.Sessions;
using WvsBeta.Database;
using MySql;
using MySql.Data;
using MySql.Data.MySqlClient;


namespace WvsBeta.Game
{
    public class Map
    {
        public enum SpawnTypes
        {
            Npc,
            Mob,
            Reactor,
            MaxSpawnTypes
        }


        public int ID { get; set; }
        public int ForcedReturn { get; set; }
        public int ReturnMap { get; set; }
        public bool Town { get; set; }

        public float MobRate { get; set; }
        public bool HasClock { get; set; }
        public byte FieldType { get; set; }

        public int mWeatherID { get; set; }
        public string mWeatherMessage { get; set; }
        public bool mWeatherIsAdmin { get; set; }

        public int mJukeboxID { get; set; }
        public string mJukeboxUser { get; set; }

        const uint NpcStart = 100;
        const uint ReactorStart = 200;

        private LoopingID _objectIDs { get; set; }

        public List<Foothold> Footholds { get; private set; }
        public List<Life>[] Life { get; private set; }
        public Dictionary<string, Portal> Portals { get; private set; }
        public Dictionary<int, Portal> SpawnPoints { get; private set; }
        public Dictionary<int, Seat> Seats { get; private set; }

        public List<short> UsedSeats { get; private set; }

        public List<Mob> Mobs { get; private set; }
        public Dictionary<int, Drop> Drops { get; private set; }
        public List<Character> Characters { get; private set; }

        public const double MAP_PREMIUM_EXP = 1.5;


        public Map(int id)
        {
            ID = id;
            _objectIDs = new LoopingID();

            Footholds = new List<Foothold>();
            Portals = new Dictionary<string, Portal>();
            SpawnPoints = new Dictionary<int, Portal>();
            Seats = new Dictionary<int, Seat>();

            Mobs = new List<Mob>();
            Drops = new Dictionary<int, Drop>();
            Characters = new List<Character>();

            Life = new List<Game.Life>[(int)SpawnTypes.MaxSpawnTypes];
            Life[(int)SpawnTypes.Npc] = new List<Game.Life>();
            Life[(int)SpawnTypes.Mob] = new List<Game.Life>();
            Life[(int)SpawnTypes.Reactor] = new List<Game.Life>();

            UsedSeats = new List<short>();
        }

        public void MapTimer()
        {
            DateTime exp = DateTime.Now.Subtract(new TimeSpan(0, 3, 0));
            Dictionary<int, Drop> dropBackup = new Dictionary<int, Drop>(Drops);
            lock (dropBackup)
            {
                foreach (KeyValuePair<int, Drop> kvp in dropBackup)
                {
                    if (kvp.Value != null && exp > kvp.Value.Droptime)
                    {
                        kvp.Value.RemoveDrop(true);
                    }
                }
            }
        }

        public void ClearDrops()
        {
            Dictionary<int, Drop> dropBackup = new Dictionary<int, Drop>(Drops);
            lock (dropBackup)
            {
                foreach (KeyValuePair<int, Drop> kvp in dropBackup)
                {
                    if (kvp.Value != null)
                    {
                        kvp.Value.RemoveDrop(true);
                    }
                }
            }
        }

        internal Mob GetMob(int SpawnID) { return Mobs.Find(m => m.SpawnID == SpawnID); }
        internal Life GetNPC(int SpawnID) { return Life[(int)SpawnTypes.Npc].Find(n => n.SpawnID == SpawnID); }
        internal Life GetReactor(int SpawnID) { return Life[(int)SpawnTypes.Reactor].Find(r => r.SpawnID == SpawnID); }



        public void AddDrop(Drop drop)
        {
            int id = _objectIDs.NextValue();
            Drops.Add(id, drop);
            drop.ID = id;
        }
        public void RemoveDrop(Drop drop)
        {
            Drops.Remove(drop.ID);
            drop = null;
        }

        public void AddSeat(Seat ST)
        {
            Seats.Add(ST.ID, ST);
        }
        /**
        public void AddMessageBox(Character ch, int itemid, string msg, string name, short x, short y)
        {
            int id = mObjectIDs.NextValue();
            CMessageBox m = new CMessageBox();
            m.ID = id;
            m.ItemID = itemid;
            m.Message = msg;
            m.Owner = name;
            m.X = x;
            m.Y = y;
            _MessageBoxes.Add(id, m);
            ch.mMessageBoxOID = id;
            SendPacket(MapPacket.MessageBoxEnterField(id, itemid, msg, name, x, y));
        }

        public void AddMinigame(Character ch, string name, byte function, int x, int y, byte piecetype)
        {
            int id = mObjectIDs.NextValue();
            Minigame m = new Minigame();
            m.Name = name;
            m.Type = function;
            m.Mapid = ch.mMap;
            m.X = x;
            m.Y = y;
            m.Piecetype = piecetype;
            m.ID = id;
            ch.mMinigameOID = id;
            _Minigames.Add(id, m);
        }
         *
         * */

        public uint makeNPCID(uint id)
        {
            return (id - NpcStart);
        }

        public uint makeNPCID()
        {
            return (uint)(Life[(int)SpawnTypes.Npc].Count - 1 + NpcStart);
        }

        public uint makeReactorID(uint id)
        {
            return (id - ReactorStart);
        }

        public uint makeReactorID()
        {
            return (uint)(Life[(int)SpawnTypes.Reactor].Count - 1 + ReactorStart);
        }

        public void AddFoothold(Foothold FH)
        {
            Footholds.Add(FH);
        }

        public void AddLife(Life LF)
        {
            if (LF.Type == "n") {
                Life[(int)SpawnTypes.Npc].Add(LF);
                LF.SpawnID = makeNPCID();
            }
            else if (LF.Type == "r")
            {
                Life[(int)SpawnTypes.Reactor].Add(LF);
                LF.SpawnID = makeReactorID();
            }
            else
            {
                Life[(int)SpawnTypes.Mob].Add(LF);
                LF.SpawnID = (uint)(Life[(int)SpawnTypes.Mob].Count + 2);
                spawnMob((int)LF.SpawnID, LF);
            }
        }

        public void AddPortal(Portal PT)
        {
            if (PT.Name == "sp")
            {
                SpawnPoints.Add(PT.ID, PT);
            }
            else if (PT.Name == "tp")
            {
                // TownPortal: Mystic Door
            }
            else
            {
                if (Portals.ContainsKey(PT.Name))
                {
                    Console.WriteLine("Duplicate portal, Name: {0} MapID: {1}", PT.Name, ID);
                }
                else
                {
                    Portals.Add(PT.Name, PT);
                }
            }
        }

        public void RemovePortal(Portal PT)
        {
            if (PT.Name == "sp")
            {
                SpawnPoints.Remove(PT.ID);
            }
            else if (PT.Name == "tp")
            {
                // TownPortal: Mystic Door
            }
            else
            {
                if (Portals.ContainsKey(PT.Name))
                {
                    Console.WriteLine("Duplicate portal, Name: {0} MapID: {1}", PT.Name, ID);
                }
                else
                {
                    Portals.Remove(PT.Name);
                }
            }
        }

        public Character GetPlayer(int id)
        {
            return Characters.FirstOrDefault(a => { return a.ID == id; });
        }




        //This checks to see if the User in a Premium Map (Internet Cafe)
        public static bool isPremium(int pMapID)
        {
            if (pMapID >= 190000000 && pMapID <= 195030000)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void RemovePlayer(Character chr, bool gmhide = false)
        {
            if (gmhide || Characters.Contains(chr))
            {
                if (!gmhide) Characters.Remove(chr);

                UpdateMobControl(chr);
                PetsPacket.SendRemovePet(chr, gmhide);
                Characters.ForEach(p => { MapPacket.SendCharacterLeavePacket(chr.ID, p); });
            }
        }

        public void AddPlayer(Character chr, bool gmhide = false)
        {
            if (!gmhide) Characters.Add(chr);
            ShowObjects(chr);
        }

        public void SendPacket(Packet packet, Character skipme = null, bool log = false)
        {
            Characters.ForEach(p =>
            {
                if (p != skipme)
                    p.sendPacket(packet);
                else if (log)
                    Console.WriteLine("Not sending packet to charid {0} (skipme: {1})", p.ID, skipme.ID);
            });
        }

        public void Disconnect(Character skipme = null)
        {
            skipme.mPlayer.Socket.Disconnect();
        }


        public void ShowPlayer(Character chr)
        {
            Characters.ForEach(p =>
            {
                if (p != chr && !p.Buffs.HasGMHide())
                {
                    MapPacket.SendCharacterEnterPacket(p, chr);
                    MapPacket.SendCharacterEnterPacket(chr, p);
                    p.Pets.SpawnPet(chr);
                    if (p.Summons.mSummon != null) SummonPacket.SendShowSummon(p, p.Summons.mSummon, false, chr);
                    if (p.Summons.mPuppet != null) SummonPacket.SendShowSummon(p, p.Summons.mPuppet, false, chr);
                }
            });
        }

        public void ShowObjects(Character chr)
        {
            if (HasClock)
                MapPacket.SendMapClock(chr, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);

            if (chr.Pets.GetEquippedPet() != null)
            {
                Pet pet = chr.Pets.GetEquippedPet();
                pet.Position = new Pos(chr.Position);
                pet.Foothold = chr.Foothold;
                pet.Stance = 0;
            }
            chr.Pets.SpawnPet();
            Characters.ForEach(p =>
            {
                if (p != chr)
                {
                    MapPacket.SendCharacterEnterPacket(p, chr);
                    MapPacket.SendCharacterEnterPacket(chr, p);
                    p.Pets.SpawnPet(chr);
                    if (p.Summons.mSummon != null) SummonPacket.SendShowSummon(p, p.Summons.mSummon, false, chr);
                    if (p.Summons.mPuppet != null) SummonPacket.SendShowSummon(p, p.Summons.mPuppet, false, chr);
                }
            });
            Life[(int)SpawnTypes.Npc].ForEach(n => { MapPacket.ShowNPC(n, chr); });

            Mobs.ForEach(m =>
            {
                if (m.HP != 0)
                {
                    if (m.ControlStatus == MobControlStatus.ControlNone)
                    {
                        UpdateMobControl(m, true, chr);
                    }
                    else
                    {
                        MobPacket.SendMobSpawn(chr, m, 0, null, false, true);
                        UpdateMobControl(m, false, null);
                    }
                }
            });

            foreach (KeyValuePair<int, Drop> drop in Drops)
            {
                drop.Value.ShowDrop(chr);
            }



            if (mWeatherID != 0) MapPacket.SendWeatherEffect(ID, chr);
        }

        public void sendBoat(Character chr)
        {
            MapPacket.sendBoat(chr);
        }

        public bool MakeJukeboxEffect(int itemID, string user)
        {
            if (mWeatherID != 0) return false;
            mJukeboxID = itemID;
            mJukeboxUser = user;
            MapPacket.SendJukebox(ID);

            MasterThread.MasterThread.Instance.AddRepeatingAction(new MasterThread.MasterThread.RepeatingAction(
                "JukeBox Remove Effect",
                (date) => { StopJukeboxEffect(); },
                30 * 1000, 0));
            return true;
        }

        public bool MakeWeatherEffect(int itemID, string message, TimeSpan time, bool admin = false)
        {
            if (mWeatherID != 0) return false;
            mWeatherID = itemID;
            mWeatherMessage = message;
            mWeatherIsAdmin = admin;
            MapPacket.SendWeatherEffect(ID);
            MasterThread.MasterThread.Instance.AddRepeatingAction(new MasterThread.MasterThread.RepeatingAction(
                "Weather Remove Effect",
                (date) => { StopWeatherEffect(); },
                (ulong)time.TotalMilliseconds, 0));
            return true;
        }

        public void StopJukeboxEffect()
        {
            mJukeboxID = 0;
            mJukeboxUser = "";
            MapPacket.SendWeatherEffect(ID);
        }

        public void StopWeatherEffect()
        {
            mWeatherID = 0;
            mWeatherMessage = "";
            mWeatherIsAdmin = false;
            MapPacket.SendWeatherEffect(ID);
        }

        public int KillAllMobs(Character chr, bool damage)
        {
            int amount = 0;

            try
            {
                List<Mob> mobsBackup = new List<Mob>(Mobs);

                lock (mobsBackup)
                {
                    foreach (Mob mob in mobsBackup)
                    {
                        if (mob != null)
                        {
                            if (damage)
                                MobPacket.SendMobDamageOrHeal(chr, mob.SpawnID, mob.HP, false);
                            if (mob.giveDamage(chr, mob.HP))
                            {
                                mob.CheckDead();
                                amount++;
                            }
                        }
                    }
                }
                mobsBackup.Clear();
                mobsBackup = null;
            }
            catch { }
            return amount;
        }

        public int KillAllMobs(int mapid, bool damage)
        {
            int amount = 0;

            try
            {
                List<Mob> mobsBackup = new List<Mob>(Mobs);

                lock (mobsBackup)
                {
                    foreach (Mob mob in mobsBackup)
                    {
                        if (mob != null)
                        {
                            if (damage)
                                MobPacket.SendMobDamageOrHeal(mapid, mob.SpawnID, mob.HP, false);
                            mob.HP = 0;
                            mob.CheckDead();
                            amount++;

                        }
                    }
                }
                mobsBackup.Clear();
                mobsBackup = null;
            }
            catch { }
            return amount;
        }


        public void RemoveMob(Mob mob)
        {
            Mobs.Remove(mob);
        }

        public int spawnMobNoRespawn(int mobid, Pos position, short foothold)
        {
            int id = _objectIDs.NextValue();
            Mob mob = new Mob(id, ID, mobid, position, foothold, MobControlStatus.ControlNormal, false);
            Mobs.Add(mob);

            MobPacket.SendMobSpawn(null, mob, 0, null, true, false);
            UpdateMobControl(mob, true, null);

            return id;
        }


        public int spawnMobNoRespawn(int mobid, Pos position, short foothold, Mob owner, byte summonEffect)
        {
            int id = _objectIDs.NextValue();
            Mob mob = new Mob(id, ID, mobid, position, foothold, MobControlStatus.ControlNormal, summonEffect != 0 && owner != null);
            Mobs.Add(mob);
            if (summonEffect != 0)
            {
                mob.Owner = owner;
                if (owner != null)
                {
                    owner.SpawnedMobs.Add(id, mob);
                }
            }
            MobPacket.SendMobSpawn(null, mob, summonEffect, owner, (owner == null), false);

            UpdateMobControl(mob, true, null);
            return id;
        }

        public int spawnMob(int mobid, Pos position, short foothold, Mob owner, byte summonEffect)
        {
            int id = _objectIDs.NextValue();
            Mob mob = new Mob(id, ID, mobid, position, foothold, MobControlStatus.ControlNormal, summonEffect != 0 && owner != null);
            Mobs.Add(mob);
            if (summonEffect != 0)
            {
                mob.Owner = owner;
                if (owner != null)
                {
                    owner.SpawnedMobs.Add(id, mob);
                }
            }
            MobPacket.SendMobSpawn(null, mob, summonEffect, owner, (owner == null), false);

            UpdateMobControl(mob, true, null);
            return id;
        }

        public int spawnMob(int spawnid, Life life)
        {
            int id = _objectIDs.NextValue();
            Mob mob = new Mob(id, ID, life.ID, new Pos(life.X, life.Y), (short)life.Foothold, MobControlStatus.ControlNormal);
            mob.SetSpawnData(life);
            Mobs.Add(mob);
            MobPacket.SendMobSpawn(null, mob, 0, null, true, false);
            UpdateMobControl(mob, true, null);
            return id;
        }


        public void UpdateMobControl(Character who)
        {
            Mobs.ForEach(m => { if (m.Controller == who) UpdateMobControl(m, false, null); });
        }

        public void UpdateMobControl(Mob mob, bool spawn, Character chr)
        {
            int maxpos = 200000;
            foreach (Character c in Characters)
            {
                int curpos = mob.Position - c.Position;
                if (curpos < maxpos)
                {
                    mob.setControl(c, spawn, chr);
                    return;
                }
            }
            mob.setControl(null, spawn, chr);
        }


        public Pos FindFloor(Pos mainPos)
        {
            short x = mainPos.X;
            short y = (short)(mainPos.Y - 100);
            short maxy = mainPos.Y;

            bool firstCheck = true;

            for (int i = 0; i < Footholds.Count; i++)
            {
                Foothold fh = Footholds[i];
                if ((x > fh.X1 && x <= fh.X2) || (x > fh.X2 && x <= fh.X1))
                {
                    short cmax = (short)((float)(fh.Y1 - fh.Y2) / (fh.X1 - fh.X2) * (x - fh.X1) + fh.Y1);
                    if ((cmax <= maxy || (maxy == mainPos.Y && firstCheck)) && cmax >= y)
                    {
                        maxy = cmax;
                        firstCheck = false;
                    }
                }
            }
            return new Pos(x, maxy);
        }

    }

    public class Foothold
    {
        public ushort ID { get; set; }
        //public ushort PreviousIdentifier { get; set; }
        //public ushort NextIdentifier { get; set; }
        public short X1 { get; set; }
        public short Y1 { get; set; }
        public short X2 { get; set; }
        public short Y2 { get; set; }
    }

    public class Life
    {
        public int ID { get; set; }
        public uint SpawnID { get; set; }
        public int RespawnTime { get; set; }
        public string Type { get; set; }
        public ushort Foothold { get; set; }
        public bool FacesLeft { get; set; }
        public short X { get; set; }
        public short Y { get; set; }
        public short Cy { get; set; }
        public short Rx0 { get; set; }
        public short Rx1 { get; set; }
    }

    public class Portal
    {
        public byte ID { get; set; }
        public short X { get; set; }
        public short Y { get; set; }
        public string Name { get; set; }
        public int ToMapID { get; set; }
        public string ToName { get; set; }
    }

    public class Seat
    {
        public byte ID { get; set; }
        public short X { get; set; }
        public short Y { get; set; }
    }
}
