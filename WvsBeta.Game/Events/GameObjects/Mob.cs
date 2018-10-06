using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;
using WvsBeta.Common.Sessions;
using WvsBeta.Common;

namespace WvsBeta.Game
{
    public enum MobControlStatus
    {
        ControlNormal,
        ControlNone
    }

    public class Mob : MovableLife
    {
        public Life SpawnData { get; set; }
        public int MobID { get; set; }
        public int MapID { get; set; }
        public int SpawnID { get; set; }
        public int EXP { get; set; }
        public int HP { get; set; }
        public int MP { get; set; }
        public Mob Owner { get; set; }
        public Character Controller { get; set; }
        public short OriginalFoothold { get; set; }
        public Dictionary<int, ulong> Damages { get; set; }
        public MobControlStatus ControlStatus { get; set; }
        public float AllowedSpeed { get; set; }
        public DateTime lastMove { get; set; }
        public bool DoesRespawn { get; set; }
        public bool IsDead { get; set; }
        public Pos OriginalPosition { get; set; }
        public Dictionary<byte, DateTime> LastSkillUse { get; set; }
        public Dictionary<int, Mob> SpawnedMobs { get; set; }

        public LoopingID mObjectIDs { get; set; }


        private int DeadsInFiveMinutes { get; set; }

        internal Mob(int id, int mapid, int mobid, Pos position, int spawnid, byte direction, short foothold, MobControlStatus controlStatus, bool pDoesRespawn = true) :
            base(foothold, position, 2)
        {
            Damages = new Dictionary<int, ulong>();
            OriginalFoothold = foothold;
            MobID = mobid;
            MapID = mapid;
            SpawnID = id;
            ControlStatus = controlStatus;
            DoesRespawn = pDoesRespawn;
            OriginalPosition = position;
            DeadsInFiveMinutes = 0;
            Init();

            if (pDoesRespawn)
                InitData();
        }

        internal Mob(int id, int mapid, int mobid, Pos position, short foothold, MobControlStatus controlStatus, bool pDoesRespawn = true) :
            base(foothold, position, 2)
        {
            Damages = new Dictionary<int, ulong>();
            OriginalFoothold = foothold;
            MobID = mobid;
            MapID = mapid;
            SpawnID = id;
            ControlStatus = controlStatus;
            DoesRespawn = pDoesRespawn;
            OriginalPosition = position;
            DeadsInFiveMinutes = 0;
            Init();
            if (pDoesRespawn)
                InitData();
        }

        public void Clearup()
        {

            //RunTimedFunction.RemoveTimer(BetterTimerTypes.MobKillTimer, MapID, SpawnID);

            OriginalPosition = null;
            Owner = null;
            Controller = null;
            Damages.Clear();
        }

        public void SetSpawnData(Life sd) { SpawnData = sd; }

        public void InitAndSpawn()
        {
            Init();

            MobPacket.SendMobSpawn(null, this, 0, null, true, false);

            DataProvider.Maps[MapID].UpdateMobControl(this, true, null);
        }

        private void InitData()
        {


            MasterThread.MasterThread.Instance.AddRepeatingAction(new MasterThread.MasterThread.RepeatingAction(
                "Monster DeadsInFiveMinutes Decreaser",
                (date) =>
                {
                    if (DeadsInFiveMinutes > 0)
                        DeadsInFiveMinutes--;
                },
                0, 5 * 60 * 1000));
        }

        public void Init()
        {
            IsDead = false;

            if (LastSkillUse != null)
                LastSkillUse.Clear();
            else
                LastSkillUse = new Dictionary<byte, DateTime>();

            if (SpawnedMobs != null)
                SpawnedMobs.Clear();
            else
                SpawnedMobs = new Dictionary<int, Mob>();

            MobData data = DataProvider.Mobs[MobID];
            HP = data.MaxHP;
            MP = data.MaxMP;
            EXP = data.EXP;
            Owner = null;
            Controller = null;
            AllowedSpeed = (100 + data.Speed) / 100.0f;
            lastMove = DateTime.Now;
        }

        public bool giveDamage(Character fucker, int amount)
        {
            if (HP == 0) return false;
            if (!Damages.ContainsKey(fucker.ID))
                Damages.Add(fucker.ID, 0);
            Damages[fucker.ID] += (ulong)amount;

            if (HP < amount)
                HP = 0;
            else
                HP -= amount;

            return true;
        }

        public void timer(Pos killPos = null)
        {
            MobData md = DataProvider.Mobs[MobID];
            foreach (int mobid in md.Revive)
            {
                DataProvider.Maps[MapID].spawnMobNoRespawn(mobid, killPos, Foothold);
            }
        }
        public void CheckDead(Pos killPos = null)
        {
            if (HP == 0 && !IsDead)
            {
                IsDead = true;
                if (killPos != null) Position = killPos;
                setControl(null, false, null);
                MobPacket.SendMobDeath(this, 1);
                Character maxDmgChar = null;
                ulong maxDmgAmount = 0;
                MobData md = DataProvider.Mobs[MobID];
                DeadsInFiveMinutes++;

                foreach (KeyValuePair<int, ulong> dmg in Damages)
                {
                    if (maxDmgAmount < dmg.Value && Server.Instance.CharacterList.ContainsKey(dmg.Key))
                    {
                        Character chr = Server.Instance.CharacterList[dmg.Key];
                        if (chr.Map == MapID)
                        {
                            maxDmgAmount = dmg.Value;
                            maxDmgChar = chr;
                        }
                    }
                }

                if (maxDmgChar != null)
                {
                    if (Server.Instance.CharacterDatabase.isDonator(maxDmgChar.UserID) || Map.isPremium(maxDmgChar.Map))
                    {
                        maxDmgChar.AddEXP((double)EXP * Server.Instance.mobExpRate * Map.MAP_PREMIUM_EXP); //Premium Maps, also known as Internet Cafe maps
                    }
                    else
                    {
                        maxDmgChar.AddEXP((uint)EXP * Server.Instance.mobExpRate);
                    }
                    DropPacket.HandleDrops(maxDmgChar, MapID, Constants.getDropName(MobID, true), SpawnID, Position, false, false, false);
                }

                foreach (int mobid in md.Revive)
                {
                    DataProvider.Maps[MapID].spawnMobNoRespawn(mobid, killPos, Foothold);
                }



                Damages.Clear();

                if (DoesRespawn)
                {
                    Position = OriginalPosition;
                    Foothold = OriginalFoothold;
                    int derp = (int)(((((SpawnData == null ? 10 : SpawnData.RespawnTime + 1) / DataProvider.Maps[MapID].MobRate)) * DeadsInFiveMinutes) * 5);

                    MasterThread.MasterThread.Instance.AddRepeatingAction(new MasterThread.MasterThread.RepeatingAction("Mob Remover", (date) =>
                    {
                        InitAndSpawn();
                    }, (ulong)derp * 1000, 0));

                }
                else
                {
                    Clearup();
                    DataProvider.Maps[MapID].RemoveMob(this);
                }
            }
        }

        public void setControl(Character control, bool spawn, Character display)
        {
            Controller = control;
            if (HP == 0) return;
            if (control != null)
            {
                MobPacket.SendMobRequestControl(control, this, spawn, null);
            }
            else if (ControlStatus == MobControlStatus.ControlNone)
            {
                MobPacket.SendMobRequestControl(control, this, spawn, display);
            }
        }

        public void endControl()
        {
            if (Controller != null && Controller.Map == MapID)
            {
                MobPacket.SendMobRequestEndControl(Controller, this);
            }
        }

        public void setControlStatus(MobControlStatus mcs)
        {
            MobPacket.SendMobRequestEndControl(null, this);
            MobPacket.SendMobSpawn(null, this, 0, null, false, false);
            ControlStatus = mcs;
            DataProvider.Maps[MapID].UpdateMobControl(this, false, null);
        }
    }
}