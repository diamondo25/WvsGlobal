using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using log4net;
using WvsBeta.Common;

namespace WvsBeta.Game
{
    public class Mob : MovableLife, IFieldObj
    {
        public int MobID { get; set; }
        public Map Field { get; set; }
        public int SpawnID { get; set; }
        public readonly short OriginalFoothold;
        public MobGenItem MobGenItem { get; set; } = null;

        public sbyte SummonType { get; set; }
        public int SummonOption { get; set; }

        public int EXP => Data.EXP;
        public int HP { get; set; }
        public int MaxHP => Data.MaxHP;
        public int MP { get; set; }
        public int MaxMP => Data.MaxMP;
        public int Level => Data.Level;
        public bool IsBoss => Data.Boss;
        public float AllowedSpeed => (100 + Data.Speed) / 100.0f;

        private MobStatus _status;
        public bool HasAnyStatus => _status != null;
        public MobStatus Status => _status ?? (_status = new MobStatus(this));
        public Mob Owner { get; set; } = null;
        public Character Controller { get; set; } = null;
        public bool IsControlled => Controller != null;

        public MobDamageLog DamageLog { get; set; }

        public bool DeadAlreadyHandled { get; set; }
        public long LastSkillUse { get; set; }
        public Dictionary<int, Mob> SpawnedMobs { get; } = new Dictionary<int, Mob>();
        public Dictionary<byte, long> SkillsInUse { get; } = new Dictionary<byte, long>();

        public bool NextAttackPossible { get; set; } = false;
        public long LastAttack { get; set; } = 0;
        public byte SkillCommand { get; set; } = 0;
        public int SummonCount { get; set; } = 0;

        public bool AlreadyStealed;
        public int ItemID_Stolen = -1;

        public int LastHitCharacterID { get; private set; }
        public byte HackReportCounter { get; set; } = 0;

        public MobData Data;
        public Pos OriginalPosition;

        internal Mob(int spawnId, Map field, int mobid, Pos position, short foothold, bool facesLeft = false) : base(foothold, position, (byte)(facesLeft ? 0 : 2))
        {
            DataProvider.Mobs.TryGetValue(mobid, out Data);
            if (Data.Flies)
            {
                // Yes, this is what they do
                OriginalFoothold = Foothold = 0;
            }
            else
            {
                OriginalFoothold = foothold;
            }
            OriginalPosition = new Pos(position);
            MobID = mobid;
            Field = field;
            SpawnID = spawnId;
            HP = MaxHP;
            MP = MaxMP;
            DamageLog = new MobDamageLog(field, MaxHP);
        }

        public bool IsShownTo(IFieldObj Object) => true;

        long lastHeal = MasterThread.CurrentTime;
        long lastPoisonDMG = MasterThread.CurrentTime;
        long lastStatusUpdate = MasterThread.CurrentTime;
        public long LastControllerAssignTime { get; set; }
        public int LastPoisonCharId { get; set; }

        public void UpdateDeads(long pNow)
        {
            if (DeadAlreadyHandled) return;



            if ((pNow - lastStatusUpdate) >= 3000)
            {
                lastStatusUpdate = pNow;
                if (HasAnyStatus) Status.Update(pNow);
            }

            if (HasAnyStatus &&
                Status.BuffPoison.IsSet(pNow) &&
                (pNow - lastPoisonDMG) >= 1000)
            {
                lastPoisonDMG = pNow;
                GiveDamage(null, Status.BuffPoison.N, true);
            }

            if (
                (
                    Data.HPRecoverAmount > 0 ||
                    Data.MPRecoverAmount > 0
                ) &&
                (pNow - lastHeal) >= 8000)
            {
                if (Data.HPRecoverAmount > 0 && HP < MaxHP)
                {
                    HP += Data.HPRecoverAmount;
                    if (HP > MaxHP) HP = MaxHP;

                    if (IsBoss && Data.HPTagBgColor > 0)
                    {
                        MapPacket.SendBossHPBar(Field, DeadAlreadyHandled ? -1 : HP, MaxHP, Data.HPTagBgColor, Data.HPTagColor);
                    }
                }

                if (Data.MPRecoverAmount > 0 && MP < MaxMP)
                {
                    MP += Data.MPRecoverAmount;
                    if (MP > MaxMP) MP = MaxMP;
                }
                lastHeal = pNow;
            }
        }


        public bool GiveDamage(Character fucker, int amount, bool pWasPoison = false)
        {
            if (DeadAlreadyHandled || HP == 0) return false;

            if (fucker != null && pWasPoison == false)
            {
                if (!fucker.IsGM)
                {
                    if (amount >= 60000)
                    {
                        fucker.PermaBan($"Impossible damage ({amount})");
                        return false;
                    }
                    if (amount >= 50000)
                    {
                        // DAMAGE HAX DETECTION AND LULZ PUNISHMENT
                        fucker.SetAP(0);
                        fucker.SetSP(0);
                        fucker.SetStr(0);
                        fucker.SetDex(0);
                        fucker.SetInt(0);
                        fucker.SetHPAndMaxHP(1);
                        fucker.SetMPAndMaxMP(0);
                        fucker.ChangeMap(0); // Back to the start with you.

                        MessagePacket.SendNoticeGMs(
                            $"{fucker.Name} : I was just p0wned by the Anti-Hack system. Damage hax ({amount})! :mavi:",
                            MessagePacket.MessageTypes.Notice
                        );
                        return false;
                    }

                    if (amount >= 30000)
                    {
                        MessagePacket.SendNoticeGMs(
                            $"{fucker.Name} : Possible damage hack: {amount} damage given!",
                            MessagePacket.MessageTypes.Notice
                        );
                    }
                }

                // Normalize damage
                amount = Math.Min(amount, HP);

                DamageLog.AddLog(fucker.ID, amount, MasterThread.CurrentDate);

                HP = Math.Max(0, HP - amount);

                if (IsBoss && Data.HPTagBgColor > 0) // There's no way to hide this :|
                {
                    MapPacket.SendBossHPBar(Field, DeadAlreadyHandled ? -1 : HP, MaxHP, Data.HPTagBgColor, Data.HPTagColor);
                }
            }
            else
            {
                // Normalize damage
                amount = Math.Min(amount, HP);

                // You cannot kill mobs with poison.
                if (pWasPoison)
                {
                    DamageLog.AddLog(LastPoisonCharId, amount, MasterThread.CurrentDate);
                    HP = Math.Max(1, HP - amount);
                    if (HP == 1)
                    {
                        // Remove poison debuff
                        Status.BuffPoison.Reset();
                        MobPacket.SendMobStatsTempReset(this, MobStatus.MobStatValue.Poison);
                    }
                }
                else
                {
                    HP = Math.Max(0, HP - amount);
                    //DamageLog.AddLog(-1, amount, MasterThread.CurrentDate);
                }
            }

            // Switch controller if required
            if (fucker != null &&
                HP > 0 &&
                Controller != fucker &&
                (MasterThread.CurrentTime - LastAttack) > 5000 &&
                !NextAttackPossible &&
                fucker.IsShownTo(this))
            {
                Trace.WriteLine("Switching controller!");
                SetController(fucker, true);
            }

            return true;
        }

        private static readonly int[] weightedSpawnTimes = { 0, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 9, 10, 11, 12, 13, 14, 15, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31 };
        public bool CheckDead(Pos killPos = null, short Delay = 0, int MesoUp = 0)
        {
            // Just make sure we are not trying to handle a dead body.
            if (HP != 0 || DeadAlreadyHandled)
            {
                return false;
            }

            DeadAlreadyHandled = true;
            if (killPos == null)
            {
                killPos = new Pos(Position);
            }

            int OwnerID = DistributeExp(out DropType OwnType, out int PartyID);

            if (OwnerID > 0)
            {
                switch (OwnType)
                {
                    case DropType.Normal:
                        {
                            var User = Field.GetPlayer(OwnerID);
                            SetMobCountQuestInfo(User);
                            break;
                        }
                    case DropType.Party:
                        {
                            var Party = Field.GetInParty(PartyID);
                            if (Party != null)
                            {
                                foreach (var User in Party)
                                {
                                    SetMobCountQuestInfo(User);
                                }
                            }
                            break;
                        }
                    case DropType.FreeForAll:
                    case DropType.Explosive:
                        foreach (var User in Field.Characters)
                        {
                            SetMobCountQuestInfo(User);
                        }
                        break;
                }
                GiveReward(OwnerID, PartyID, OwnType, killPos, Delay, MesoUp, false);
            }


            FinishDeath(false, killPos);

            Field.MobKillCount.TryGetValue(MobID, out var currentKillCount);
            currentKillCount += 1;
            Field.MobKillCount[MobID] = currentKillCount;

            return true;
        }

        public void ForceDead()
        {
            if (DeadAlreadyHandled)
            {
                return;
            }

            DeadAlreadyHandled = true;

            FinishDeath(true, Position);
        }

        private void FinishDeath(bool wasForced, Pos killPos)
        {
            RemoveController(false);

            if (!wasForced && Data.Revive != null)
            {
                // Oh damn, this mob spawns other mobs!

                foreach (var mobid in Data.Revive)
                {
                    Field.SpawnMobWithoutRespawning(
                        mobid,
                        killPos,
                        Foothold,
                        null,
                        -3,
                        SpawnID,
                        !IsFacingRight()
                    );
                }
            }

            MobPacket.SendMobDeath(this, 1); // He ded.


            DamageLog.Clear();

            if (MobGenItem != null)
            {
                var mgi = MobGenItem;

                var regenInterval = mgi.RegenInterval;
                if (regenInterval != 0)
                {
                    mgi.MobCount--;
                    if (mgi.MobCount == 0)
                    {
                        mgi.RegenAfter = 0;

                        var min = 7 * regenInterval / 10;
                        var max = 6 * regenInterval / 10;

                        mgi.RegenAfter += (min + Rand32.Next() % max);
                        Trace.WriteLine($"Setting regeninterval for mobid {MobID} to {mgi.RegenAfter} ({min} {max})");

                        mgi.RegenAfter += MasterThread.CurrentTime;
                    }
                }
            }

            Field.RemoveMob(this);
        }

        public int OnMobMPSteal(int Prop, int Percent)
        {
            if (Data.Boss)
                return 0;
            else
            {
                int Result = Percent * Data.MaxMP / 100;

                if (Result >= MP)
                    Result = MP;

                if (Result < 0 || Rand32.Next() % 100 >= Prop)
                    Result = 0;

                MP -= Result;

                return Result;
            }
        }

        private static ILog _hackLog = LogManager.GetLogger("HackLog");

        private bool AssertForHack(bool isHack, string hackType)
        {
            if (isHack)
            {
                _hackLog.Warn(hackType);
                Trace.WriteLine(hackType);
                MessagePacket.SendNoticeGMs($"Mob Check '{hackType}' triggered! Map: '{Field.ID}', controller '{Controller?.Name}'.", MessagePacket.MessageTypes.Megaphone);
            }
            return isHack;
        }

        public bool CheckVacHack(long timeInMilliseconds, Pos startPos, Pos endPos, Character chr)
        {
            var distance = startPos - endPos;
            if (distance == 0) return false;
            if (timeInMilliseconds > 10 * 1000) return false;
            if (timeInMilliseconds < 1000) return false;

            // Green snail has -65 speed, max distance 48
            // Pixie has         0 speed, max distance 135
            double maxDistance = Math.Ceiling((Data.Speed + 105.0d) * 1.4d) * 1.3; //1.3 to reduce false positive spam

            const double flushTime = 1000.0;

            var heightDiff = startPos.Y - endPos.Y;
            maxDistance += Math.Abs(heightDiff) / 5.0d;
            maxDistance *= (timeInMilliseconds / flushTime);
            maxDistance *= 1.4; // Because we need even more space

            if (distance > maxDistance)
            {
                var mobIsNotSimple = Data.Jumps || Data.Flies;
                if (mobIsNotSimple) return false;

                // Vacs are most likely going UP, so check for lower.

                var autoban = Math.Abs(heightDiff) > 500;

                if (autoban && chr != null)
                {
                    chr.PermaBan($"Mob vac on map {Field.ID} (heightDiff: {heightDiff}), " +
                                 $"Mobid {MobID} loc {startPos.X} {startPos.Y} -> {endPos.X} {endPos.Y}", Character.BanReasons.Hack);
                    return true;
                }

                if (heightDiff < -50)
                {
                    return AssertForHack(true,
                        $"MVAC: Falling mob {-heightDiff} px down, distance {distance} > {maxDistance}, delay {timeInMilliseconds / 1000.0}s; " +
                        $"Mobid {MobID} loc {startPos.X} {startPos.Y} -> {endPos.X} {endPos.Y}");
                }
                if (heightDiff > 50)
                {
                    return AssertForHack(true,
                        $"MVAC: Flying mob {heightDiff} px up, distance {distance} > {maxDistance}, delay {timeInMilliseconds / 1000.0}s; " +
                        $"Mobid {MobID} loc {startPos.X} {startPos.Y} -> {endPos.X} {endPos.Y}");
                }

                return AssertForHack(true,
                    $"MVAC: Distance {distance} > {maxDistance}, delay {timeInMilliseconds / 1000.0}s; " +
                    $"Mobid {MobID} loc {startPos.X} {startPos.Y} -> {endPos.X} {endPos.Y}");

            }
            return false;
        }

        private int DistributeExp___(out DropType OwnType, out int OwnPartyID)
        {
            OwnType = 0;
            OwnPartyID = 0;
            double Rate = 1.0;
            //if (Stat.nShowdown_ > 0)
            //    Rate = (Stat.nShowdown_ * 100) * 0.01;

            int MostDamage = 0;
            Character Chr = null;
            int MaxDamageCharacterID = 0;
            long DamageSum = DamageLog.VainDamage;
            var TotalDamages = new Dictionary<int, int>();
            var CharactersTmp = new Dictionary<int, Character>();

            foreach (var Log in DamageLog.Log)
            {
                if (!TotalDamages.ContainsKey(Log.CharacterID))
                    TotalDamages.Add(Log.CharacterID, Log.Damage);
                else
                    TotalDamages[Log.CharacterID] += Log.Damage;

                DamageSum += Log.Damage;

                Chr = CharactersTmp.ContainsKey(Log.CharacterID) ? CharactersTmp[Log.CharacterID] : Field.GetPlayer(Log.CharacterID);
                CharactersTmp[Log.CharacterID] = Chr;
                if (Chr == null) continue;

                if (Log.Damage > MostDamage)
                {
                    MaxDamageCharacterID = Log.CharacterID;
                    MostDamage = Log.Damage;
                }
            }

            if (MaxDamageCharacterID != 0)
            {
                Chr = CharactersTmp[MaxDamageCharacterID];
            }

            if (DamageSum >= DamageLog.InitHP)
            {
                Dictionary<int, PartyDamage> PartyDamages = new Dictionary<int, PartyDamage>();

                if ((OwnPartyID = Chr.PartyID) != 0)
                    OwnType = DropType.Party;
                if (Data.PublicReward)
                    OwnType = DropType.FreeForAll;
                if (Data.ExplosiveReward)
                    OwnType = DropType.Explosive;

                if (Data.EXP != 0 && DamageLog.Log.Count > 0)
                {
                    foreach (var Log in TotalDamages)
                    {
                        if (Log.Value > 0)
                        {
                            var User = Field.GetPlayer(Log.Key);
                            if (User != null && User.Field.ID == Field.ID)
                            {
                                int PartyID = User.PartyID;
                                if (PartyID == 0 || Field.GetInParty(PartyID).Count() == 1)
                                {
                                    double IncEXP =
                                        (Data.EXP * Log.Value) * 0.8 / DamageSum
                                        + ((User.ID == Chr.ID) ? (Data.EXP * 0.2) : 0);

                                    IncEXP *= (User.PrimaryStats.BuffHolySymbol.N * 0.2 + 100.0) * 0.01;

                                    IncEXP = Server.Instance.RateMobEXP * AlterEXPbyLevel(User.Level, IncEXP) * Rate;

                                    if (User.PrimaryStats.BuffCurse.IsSet())
                                        IncEXP *= 0.5;

                                    User.SetupLogging();
                                    User.AddEXP(IncEXP, User.ID == Chr.ID);
                                }
                                else
                                {
                                    if (!PartyDamages.TryGetValue(PartyID, out PartyDamage Damage))
                                    {
                                        PartyDamages.Add(PartyID, new PartyDamage()
                                        {
                                            PartyID = PartyID,
                                            Damage = Log.Value,
                                            MinLevel = User.Level,
                                            MaxDamage = Log.Value,
                                            MaxDamageCharacter = User.ID,
                                            MaxDamageLevel = User.Level
                                        });
                                    }
                                    else
                                    {
                                        Damage.Damage += Log.Value;
                                        if (Log.Value > Damage.MaxDamage)
                                        {
                                            Damage.MaxDamage = Log.Value;
                                            Damage.MaxDamageCharacter = User.UserID;
                                            Damage.MaxDamageLevel = User.Level;
                                        }

                                        if (Damage.MinLevel > User.Level)
                                            Damage.MinLevel = User.Level;
                                    }
                                }
                            }
                        }
                    }

                    // Distribute EXP over parties

                    if (PartyDamages.Count > 0)
                    {
                        foreach (PartyDamage damage in PartyDamages.Values)
                        {
                            damage.MinLevel = Math.Min(Data.Level, damage.MinLevel);
                            damage.MinLevel -= Constants.PartyMinLevelOffset;

                            var Party = Field.GetInParty(damage.PartyID);
                            var PartyMapCount = Party.Count();
                            double PartyBonusEventRate = PartyMapCount * Constants.PartyPerUserBonus + Constants.PartyTotalBonus;

                            double IncEXP = (Data.EXP * damage.Damage) * 0.8 / DamageSum;

                            // Extra bonus
                            if (Party.Any(c => c.ID == Chr.ID))
                            {
                                IncEXP += Data.EXP * 0.2;
                            }

                            IncEXP *= PartyBonusEventRate;
                            IncEXP = AlterEXPbyLevel(damage.MaxDamageLevel, IncEXP < 1 ? 1 : IncEXP);

                            foreach (var User in Party)
                            {
                                if (User != null &&
                                    User.Field.ID == Field.ID &&
                                    User.Level >= damage.MinLevel)
                                {
                                    double IncExpUser = (IncEXP - IncEXP * 0.2) / PartyMapCount;
                                    if (Chr.ID == User.ID)
                                        IncExpUser += IncEXP * 0.4;

                                    if (User.PrimaryStats.BuffHolySymbol.IsSet())
                                    {
                                        var hs1 = (User.PrimaryStats.BuffHolySymbol.N + 100.0) * IncExpUser * 0.01;
                                        var hs2 = (User.PrimaryStats.BuffHolySymbol.N * 0.2 + 100.0) * Data.EXP * 0.01;
                                        if (hs2 < hs1)
                                            IncExpUser = hs2;
                                    }

                                    IncExpUser *= Server.Instance.RateMobEXP * Rate;

                                    // Bug in GMS: No Curse effect!

                                    byte PartyBonusPercentage = Convert.ToByte(PartyBonusEventRate * 100 - 100);
                                    if (PartyBonusEventRate < 0) PartyBonusEventRate = 0;
                                    IncExpUser = IncExpUser - (IncExpUser * 0.1666666666666667);

                                    User.SetupLogging();
                                    User.AddEXP(IncExpUser, User.ID == Chr.ID);
                                }
                            }
                        }
                    }
                }

                return Chr.ID;
            }
            return 0;
        }


        private int DistributeExp(out DropType OwnType, out int OwnPartyID)
        {
            Trace.WriteLine($"Distributing EXP. EXP: {Data.EXP}");

            OwnType = 0;
            OwnPartyID = 0;
            double Rate = 1.0;
            //if (Stat.nShowdown_ > 0)
            //    Rate = (Stat.nShowdown_ * 100) * 0.01;

            var currentHour = MasterThread.CurrentDate.Hour;

            int MostDamage = 0;
            Character Chr = null;
            int MaxDamageCharacterID = 0;
            long DamageSum = DamageLog.VainDamage;
            var CharactersTmp = new Dictionary<int, Character>();

            int idx = 0;
            foreach (var Log in DamageLog.Log)
            {
                DamageSum += Log.Damage;

                Chr = CharactersTmp.ContainsKey(Log.CharacterID) ? CharactersTmp[Log.CharacterID] : Field.GetPlayer(Log.CharacterID);
                CharactersTmp[Log.CharacterID] = Chr;
                if (Chr == null) continue;

                if (Log.Damage > MostDamage)
                {
                    MaxDamageCharacterID = Log.CharacterID;
                    MostDamage = Log.Damage;
                }
                idx++;
            }

            if (MaxDamageCharacterID != 0)
            {
                Chr = CharactersTmp[MaxDamageCharacterID];
                Trace.WriteLine($"{Chr.Name} did most damage with {MostDamage}");
            }

            if (DamageSum >= DamageLog.InitHP)
            {
                var PartyDamages = new List<PartyDamage>();

                if ((OwnPartyID = Chr.PartyID) != 0)
                    OwnType = DropType.Party;
                if (Data.PublicReward)
                    OwnType = DropType.FreeForAll;
                if (Data.ExplosiveReward)
                    OwnType = DropType.Explosive;

                if (Data.EXP == 0 || DamageLog.Log.Count <= 0) return Chr.ID;


                int lastLogElement = idx - 1; // Because we increase _after_ the loop
                idx = 0;
                foreach (var Log in DamageLog.Log)
                {
                    var bLast = lastLogElement == idx;
                    idx++;
                    if (Log.Damage <= 0) continue;

                    var User = CharactersTmp[Log.CharacterID];
                    if (User == null || User.Field.ID != Field.ID) continue;

                    int PartyID = User.PartyID;
                    if (PartyID == 0 || Field.GetInParty(PartyID).Count() == 1)
                    {
                        double lastDamageBuff = 0.0;
                        if (bLast) lastDamageBuff = Data.EXP * 0.2;

                        Trace.WriteLine("Last damage buff: " + lastDamageBuff);
                        double expByDamage = Data.EXP * (double)Log.Damage;

                        Trace.WriteLine("expByDamage: " + expByDamage);
                        double IncEXP = (expByDamage * 0.8 / (double)DamageSum + lastDamageBuff);


                        Trace.WriteLine("IncEXP: " + IncEXP);

                        if (User.PrimaryStats.BuffHolySymbol.IsSet())
                        {
                            var hsBuff = (User.PrimaryStats.BuffHolySymbol.N * 0.2 + 100.0) * 0.01;
                            Trace.WriteLine("Holy Symbol buffed: " + hsBuff);
                            IncEXP *= hsBuff;
                        }

                        IncEXP = User.m_dIncExpRate * AlterEXPbyLevel(User.Level, IncEXP) * Rate;

                        Trace.WriteLine("IncEXP: " + IncEXP);

                        if (!(currentHour < 13 && currentHour >= 19))
                        {
                            // Note: this is an int, set to 100 for 1.0x
                            IncEXP = ((double)Character.ms_nIncExpRate_WSE * IncEXP * 0.01);

                            Trace.WriteLine("WS event: " + IncEXP);
                        }

                        IncEXP *= Field.m_dIncRate_Exp;
                        Trace.WriteLine("Field EXP rate IncEXP: " + IncEXP);

                        if (User.PrimaryStats.BuffCurse.IsSet())
                        {
                            IncEXP *= 0.5;
                            Trace.WriteLine("Curse debuffed IncEXP: " + IncEXP);
                        }

                        Trace.WriteLine("IncEXP before Max(_, 1.0) " + IncEXP);
                        IncEXP = Math.Max(IncEXP, 1.0);

                        Trace.WriteLine($"{User.Name} gets {IncEXP} EXP for {Log.Damage} damage");

                        User.SetupLogging();
                        User.AddEXP(IncEXP, true);
                    }
                    else
                    {
                        var Damage = PartyDamages.FirstOrDefault(x => x.PartyID == PartyID);
                        if (Damage == null)
                        {
                            Damage = new PartyDamage
                            {
                                PartyID = PartyID,
                                Damage = Log.Damage,
                                MinLevel = User.Level,
                                MaxDamage = Log.Damage,
                                MaxDamageCharacter = User.ID,
                                MaxDamageLevel = User.Level
                            };
                            PartyDamages.Add(Damage);
                        }
                        else
                        {
                            Damage.Damage += Log.Damage;
                            if (Log.Damage > Damage.MaxDamage)
                            {
                                Damage.MaxDamage = Log.Damage;
                                Damage.MaxDamageCharacter = User.UserID;
                                Damage.MaxDamageLevel = User.Level;
                            }

                            if (Damage.MinLevel > User.Level)
                                Damage.MinLevel = User.Level;
                        }
                        Damage.bLast = bLast;
                    }
                }

                // Distribute EXP over parties
                // Basically CMob::GiveExp
                if (PartyDamages.Count > 0)
                {
                    foreach (var damage in PartyDamages)
                    {
                        Trace.WriteLine($"[party {damage.PartyID}] Damage {damage.Damage}, minLevel {damage.MinLevel}, maxDamage {damage.MaxDamage} character {damage.MaxDamageCharacter}");

                        var Party = Field.GetInParty(damage.PartyID)
                            .Where(User =>
                                User != null &&
                                User.PrimaryStats.HP > 0 &&
                                User.Field.ID == Field.ID
                            );

                        damage.MinLevel = Math.Min(Data.Level, damage.MinLevel);
                        damage.MinLevel -= Constants.PartyMinLevelOffset;

                        var partyMembersHigherThanMinLevel = Party.Where(x => x.Level >= damage.MinLevel).ToArray();
                        var partyMemberCountHigherThanMinLevel = partyMembersHigherThanMinLevel.Length;
                        var partyMemberLevelSumHigherThanMinLevel = partyMembersHigherThanMinLevel.Sum(x => x.Level);

                        double partyBonusEventRate = 0.05;
                        if (Character.ms_nPartyBonusEventRate > 0)
                        {
                            partyBonusEventRate = Character.ms_nPartyBonusEventRate * 0.01 * 0.05;
                        }
                        Trace.WriteLine("PartyBonus event rate: " + partyBonusEventRate);



                        double wholePartyBuff, individualUserBuff;
                        if (partyMemberCountHigherThanMinLevel == 1)
                        {
                            individualUserBuff = wholePartyBuff = 1.0;
                        }
                        else
                        {
                            individualUserBuff = wholePartyBuff = partyMemberCountHigherThanMinLevel * partyBonusEventRate + 1.0;
                        }

                        Trace.WriteLine("Individual user buff: " + individualUserBuff);

                        var isLastPartyBonus = 0.0;
                        if (damage.bLast)
                        {
                            isLastPartyBonus = (double)Data.EXP * 0.2;
                        }

                        Trace.WriteLine("isLastPartyBonus: " + isLastPartyBonus);

                        Trace.WriteLine($"((double){damage.Damage} * (double){Data.EXP} * 0.8 / {DamageSum} + {isLastPartyBonus}) * {wholePartyBuff}");
                        double IncEXP = ((double)damage.Damage * (double)Data.EXP * 0.8 / DamageSum + isLastPartyBonus) * wholePartyBuff;

                        Trace.WriteLine("IncEXP: " + IncEXP);
                        IncEXP = Math.Max(IncEXP, 1.0);

                        IncEXP = AlterEXPbyLevel(damage.MaxDamageLevel, IncEXP);

                        var twentyPercentEXP = (double)IncEXP * 0.2;
                        var startExpPerUser = IncEXP - twentyPercentEXP;

                        foreach (var User in partyMembersHigherThanMinLevel)
                        {
                            Trace.WriteLine($"[party {damage.PartyID}] {User.Name} gets exp...");
                            double IncExpUser = User.Level * startExpPerUser / (double)partyMemberLevelSumHigherThanMinLevel;

                            Trace.WriteLine("IncExpUser: " + IncExpUser);
                            if (Chr.ID == User.ID)
                                IncExpUser += twentyPercentEXP;

                            if (User.PrimaryStats.BuffHolySymbol.IsSet())
                            {
                                if (partyMemberCountHigherThanMinLevel == 1)
                                {
                                    IncExpUser = (User.PrimaryStats.BuffHolySymbol.N * 0.2 + 100.0) * IncExpUser * 0.01;
                                }
                                else if (partyMemberLevelSumHigherThanMinLevel > 1)
                                {
                                    IncExpUser = (User.PrimaryStats.BuffHolySymbol.N + 100.0) * IncExpUser * 0.01;
                                    var maxHSBuff = (User.PrimaryStats.BuffHolySymbol.N * 0.2 + 100.0) * Data.EXP * 0.01;


                                    if (maxHSBuff < IncExpUser)
                                        IncExpUser = maxHSBuff;
                                }
                            }
                            Trace.WriteLine("HS buff: " + IncExpUser);

                            IncExpUser = User.m_dIncExpRate * IncExpUser * Rate;
                            Trace.WriteLine("IncExpUser: " + IncExpUser);

                            if (!(currentHour < 13 && currentHour >= 19))
                            {
                                IncExpUser = ((double)Character.ms_nIncExpRate_WSE * IncExpUser * 0.01);
                                Trace.WriteLine("WS event: " + IncExpUser);
                            }

                            IncExpUser *= Field.m_dIncRate_Exp;
                            Trace.WriteLine("Field increase rate: " + IncExpUser);

                            var marriageExpBuff = Character.ms_nIncExpRate_Wedding - 100;

                            var marriedAndBothInThisParty = false;
                            if (false)
                            {
                                // Marriage stuff, check if this User is in a party with its Partner
                            }

                            if (User.PrimaryStats.BuffCurse.IsSet())
                            {
                                IncExpUser *= 0.5;
                                Trace.WriteLine("Cursed: " + IncExpUser);
                            }


                            byte PartyBonusPercentage = (byte)Math.Max(individualUserBuff * 100.0 - 100.0, 0.0);

                            Trace.WriteLine($"[party {damage.PartyID}] {User.Name} gets {IncExpUser} EXP");
                            User.SetupLogging();
                            User.AddEXP(IncExpUser, User.ID == Chr.ID);
                        }
                    }
                }

                return Chr.ID;
            }
            return 0;
        }

        public bool DoSkill(byte skillId, byte level, short delay)
        {
            if (HasAnyStatus && Status.BuffSealSkill.IsSet() ||
                SkillCommand != skillId)
            {
                SkillCommand = 0;
                return false;
            }

            var mobSkills = Data.Skills;
            if (mobSkills == null)
                return false;

            // Bug: level == 0 in packet
            bool FIX_ZERO_LVL_BUG = level == 0;
            var mobSkill = mobSkills.FirstOrDefault(x => x.SkillID == skillId && (FIX_ZERO_LVL_BUG || x.Level == level));

            if (mobSkill == null ||
                !DataProvider.MobSkills.TryGetValue(skillId, out var msdLevels))
            {
                SkillCommand = 0;
                return false;
            }
            else if (FIX_ZERO_LVL_BUG)
            {
                level = mobSkill.Level;
            }

            if (!msdLevels.ContainsKey(level))
            {
                SkillCommand = 0;
                return false;
            }

            var actualSkill = msdLevels[level];
            // Validate skill use here? EG HPLimit and such
            MP = Math.Max(0, MP - actualSkill.MPConsume);
            LastSkillUse = MasterThread.CurrentTime;

            SkillsInUse[skillId] = LastSkillUse;

            SkillCommand = 0;

            short nValue = (short)actualSkill.X;
            int rValue = skillId | (level << 16);
            long tValue = actualSkill.Time * 1000;
            bool AOE = false;

            var skillIdAsEnum = (Constants.MobSkills.Skills)skillId;

            switch (skillIdAsEnum)
            {
                case Constants.MobSkills.Skills.MagicAttackUpAoe:
                case Constants.MobSkills.Skills.MagicDefenseUpAoe:
                case Constants.MobSkills.Skills.WeaponAttackUpAoe:
                case Constants.MobSkills.Skills.WeaponDefenseUpAoe:
                    AOE = true;
                    break;
            }

            MobStatus.MobBuffStat buffStat = null;
            switch (skillIdAsEnum)
            {
                case Constants.MobSkills.Skills.WeaponAttackUpAoe:
                case Constants.MobSkills.Skills.WeaponAttackUp: buffStat = Status.BuffPowerUp; break;

                case Constants.MobSkills.Skills.MagicAttackUpAoe:
                case Constants.MobSkills.Skills.MagicAttackUp: buffStat = Status.BuffMagicUp; break;

                case Constants.MobSkills.Skills.WeaponDefenseUpAoe:
                case Constants.MobSkills.Skills.WeaponDefenseUp: buffStat = Status.BuffPowerGuardUp; break;

                case Constants.MobSkills.Skills.MagicDefenseUpAoe:
                case Constants.MobSkills.Skills.MagicDefenseUp: buffStat = Status.BuffMagicGuardUp; break;

                case Constants.MobSkills.Skills.WeaponImmunity: buffStat = Status.BuffPhysicalImmune; break;
                case Constants.MobSkills.Skills.MagicImmunity: buffStat = Status.BuffMagicImmune; break;

                case Constants.MobSkills.Skills.PoisonMist:
                    // Smoke them boys out!
                    Field.CreateMist(
                        this,
                        SpawnID,
                        skillId,
                        level,
                        actualSkill.Time,
                        actualSkill.LTX,
                        actualSkill.LTY,
                        actualSkill.RBX,
                        actualSkill.RBY,
                        delay
                    );
                    break;

                case Constants.MobSkills.Skills.Seal:
                case Constants.MobSkills.Skills.Darkness:
                case Constants.MobSkills.Skills.Weakness:
                case Constants.MobSkills.Skills.Stun:
                case Constants.MobSkills.Skills.Curse:
                    int left = actualSkill.LTX;
                    int right = actualSkill.RBX;

                    if (!IsFacingRight())
                    {
                        left = -actualSkill.RBX;
                        right = -actualSkill.LTX;
                    }

                    var chance = actualSkill.Prop;
                    if (chance == 0) chance = 100;

                    Field.GetCharactersInRange(
                        Position,
                        new Pos((short)left, actualSkill.LTY),
                        new Pos((short)right, actualSkill.RBY)
                    ).ForEach(character =>
                    {
                        if (character.GMHideEnabled) return;
                        if (Rand32.Next() % 100 >= chance)
                            // Lucky bastard
                            return;

                        if (character.PrimaryStats.BuffHolySymbol.IsSet()) return;

                        BuffStat bs = null;
                        switch (skillIdAsEnum)
                        {
                            case Constants.MobSkills.Skills.Seal: bs = character.PrimaryStats.BuffSeal; break;
                            case Constants.MobSkills.Skills.Darkness: bs = character.PrimaryStats.BuffDarkness; break;
                            case Constants.MobSkills.Skills.Weakness: bs = character.PrimaryStats.BuffWeakness; break;
                            case Constants.MobSkills.Skills.Stun: bs = character.PrimaryStats.BuffStun; break;
                            case Constants.MobSkills.Skills.Curse: bs = character.PrimaryStats.BuffCurse; break;
                        }

                        if (bs != null)
                        {
                            var stat = bs.Set(rValue, 1, BuffStat.GetTimeForBuff(tValue + delay));
                            character.Buffs.FinalizeBuff(stat, delay);
                        }
                    });
                    break;
                case Constants.MobSkills.Skills.Summon:
                    {
                        SummonCount += actualSkill.Summons.Count;

                        short miny = (short)(Position.Y + actualSkill.LTY);
                        short maxy = (short)(Position.Y + actualSkill.RBY);
                        short minx = (short)(Position.X + actualSkill.LTX);
                        short maxx = (short)(Position.X + actualSkill.RBX);
                        short d = 0;
                        Random rnd = new Random();

                        foreach (var summonId in actualSkill.Summons)
                        {
                            short summony = (short)rnd.Next(miny, maxy);

                            short summonx = (short)(Position.X + ((d % 2) == 1 ? (35 * (d + 1) / 2) : -(40 * (d / 2))));

                            Pos tehfloor = Field.FindFloor(new Pos(summonx, summony));
                            if (tehfloor.Y == summony)
                            {
                                tehfloor.Y = Position.Y;
                            }
                            var fh = Field.GetFootholdUnderneath(tehfloor.X, tehfloor.Y, out int maxY);
                            Field.SpawnMobWithoutRespawning(
                                summonId,
                                tehfloor,
                                (short)(fh?.ID ?? 0),
                                this,
                                (sbyte)actualSkill.SummonEffect,
                                delay,
                                !IsFacingRight()
                            );
                            d++;
                        }
                        break;
                    }
            }

            // TODO: Handle AOE

            if (buffStat != null)
            {
                var stat = buffStat.Set(rValue, nValue, MasterThread.CurrentTime + tValue);

                MobPacket.SendMobStatsTempSet(this, delay, stat);
            }

            return true;
        }

        public double AlterEXPbyLevel(int Level, double IncEXP) => IncEXP;

        public void GiveMoney(Character User, AttackData.AttackInfo Attack, int AttackCount)
        {
            if (User.Skills.GetSkillLevel(Constants.ChiefBandit.Skills.Pickpocket, out SkillLevelData SkillData) > 0)
            {
                if (AttackCount > 0)
                {
                    List<Reward> Rewards = new List<Reward>();
                    double Rate = User.Level / Data.Level;
                    double DamageRate = 0;
                    if (Rate > 1.0) Rate = 1.0;

                    foreach (int Damage in Attack.Damages)
                    {
                        if (Damage != 0 && SkillData.Property * Rate >= Rand32.Next() % 100)
                        {
                            DamageRate = Damage / Data.MaxHP;
                            if (DamageRate > 1.0) DamageRate = 1.0;
                            if (DamageRate < 0.5) DamageRate = 0.5;
                            DamageRate = Data.Level * SkillData.XValue * DamageRate * 0.006666666666666667;
                            if (DamageRate < 1.0) DamageRate = 1.0;
                            int Mesos = Convert.ToInt32(Rand32.NextBetween(1, int.MaxValue) % DamageRate);
                            Rewards.Add(Reward.Create(Mesos));
                        }
                    }

                    int x2 = Position.X - 10 * Rewards.Count + 10;
                    int Delay = 0;
                    foreach (Reward Reward in Rewards)
                    {
                        if (!Field.DropPool.Create(Reward, User.ID, 0, DropType.Normal, SpawnID, Position, x2, (short)(Attack.HitDelay + Delay), false, 0, false, false))
                        {
                            Delay += 120;
                            x2 += 20;
                        }
                    }
                }
            }
        }

        public void GiveReward(int OwnerID, int OwnPartyID, DropType OwnType, Pos Pos, short Delay, int MesoUp, bool Steal)
        {
            if (AlreadyStealed && Steal) return;

            var User = Server.Instance.GetCharacter(OwnerID);
            double Showdown = 1.0;
            //if (Stat.nShowdown_ > 0)
            //    Showdown = (Stat.nShowdown_ + 100.0) * 0.01;


            var Rewards = Reward.GetRewards(
                User,
                Field,
                MobID,
                'm',
                Field.ID / 10000000 == 19,
                Showdown
            );
            if (Rewards != null)
            {
                Rewards.Shuffle();

                OwnerID = (Data.PublicReward ? 0 : OwnerID);
                OwnPartyID = (Data.PublicReward ? 0 : OwnPartyID);

                if (Steal && Rewards.Count > 0)
                {
                    Reward StolenDrop = null;
                    int Limit = 0;
                    while (StolenDrop == null || DataProvider.QuestItems.Contains(StolenDrop.ItemID))
                    {
                        StolenDrop = Rewards[(int)(Rand32.Next() % Rewards.Count)];
                        if (Limit++ > 100)
                        {
                            StolenDrop = null;
                            break;
                        }
                    }

                    if (StolenDrop != null)
                    {
                        // NOTE: if its money it should drop half.
                        Field.DropPool.Create(StolenDrop, OwnerID, OwnPartyID, OwnType, SpawnID, Pos, Pos.X, Delay, false, 0, false, false);
                        ItemID_Stolen = StolenDrop.ItemID;
                        AlreadyStealed = true;
                    }
                }
                else
                {
                    int i = 0;
                    int x2 = Pos.X + Rewards.Count * (Data.ExplosiveReward ? -20 : -10);

                    foreach (Reward Drop in Rewards)
                    {
                        if (/*(DataProvider.QuestItems.Contains(Drop.ItemID) && !User.Quests.ItemCheck(Drop.ItemID)) || */(ItemID_Stolen == Drop.ItemID && !Drop.Mesos))
                            continue;
                        if (Drop.Mesos)
                        {
                            if (MesoUp > 0)
                                Drop.Drop = (Drop.Drop * MesoUp / 100);
                            Drop.Drop = Convert.ToInt32(Drop.Drop * Server.Instance.RateMesoAmount);
                        }
                        if (!Field.DropPool.Create(Drop, OwnerID, OwnPartyID, OwnType, SpawnID, Pos, x2, Delay, false, 0, false, false))
                        {
                            i++;
                            Delay += 200;
                            x2 += Data.ExplosiveReward ? 40 : 20;
                        }
                    }
                }
            }

        }

        public void RemoveController(bool sendPacket)
        {
            if (!IsControlled) return;
            // Make sure we are not bugging people
            if (Controller.Field == Field && sendPacket)
            {
                MobPacket.SendMobRequestEndControl(Controller, SpawnID);
            }
            Controller = null;
        }

        public void SetMobCountQuestInfo(Character User)
        {
            if (User != null && User.PrimaryStats.HP > 0 && User.Field.ID == Field.ID)
            {
                //TODO
            }
        }

        public void SetController(Character controller, bool chasing = false, bool sendStopControlPacket = true)
        {
            if (HP == 0) return;
            RemoveController(sendStopControlPacket);

            HackReportCounter = 0;
            NextAttackPossible = false;
            SkillCommand = 0;

            LastAttack = MasterThread.CurrentTime;
            LastMove = LastAttack;
            LastControllerAssignTime = MasterThread.CurrentTime;
            Controller = controller;
            MobPacket.SendMobRequestControl(Controller, this, chasing);
        }

        public void DoPoison(int charid, int poisonSLV, long buffTime, int skillId, short magicAttack, short delay)
        {
            if (IsBoss || (Data.elemModifiers.TryGetValue(SkillElement.Poison, out var resistance) && resistance == 1)) return;
            Mob mob = this;
            mob.LastPoisonCharId = charid;
            var stat = mob.Status.BuffPoison.Set(
                skillId,
                Math.Max((short)(mob.MaxHP / (70 - poisonSLV)), magicAttack),
                MasterThread.CurrentTime + buffTime
            );

            MobPacket.SendMobStatsTempSet(mob, delay, stat);
        }
    }

    public class MobDamageLog
    {
        public Map Field;
        public int InitHP;
        public int VainDamage;
        public List<MobDamageInfo> Log;

        public MobDamageLog(Map Map, int HP)
        {
            Field = Map;
            InitHP = HP;
            Log = new List<MobDamageInfo>();
        }

        private int GetNextDamageValue(int currentDamage, int extraDamage)
        {
            long newDamage = (long)currentDamage + extraDamage;
            if (newDamage > int.MaxValue) newDamage = int.MaxValue;

            return (int)newDamage;
        }

        public void AddLog(int CharacterID, int Damage, DateTime tCur)
        {
            var existingItem = Log.FirstOrDefault(x => x.CharacterID == CharacterID);

            if (existingItem != null)
            {
                existingItem.Damage = GetNextDamageValue(existingItem.Damage, Damage);
            }
            else
            {

                if (Log.Count >= 32)
                {
                    var firstDamageElem = Log.First();
                    VainDamage = GetNextDamageValue(VainDamage, firstDamageElem.Damage);
                    Log.Remove(firstDamageElem);
                }

                Log.Add(new MobDamageInfo
                {
                    CharacterID = CharacterID,
                    Damage = Damage,
                    Time = tCur
                });
            }
        }

        public void Clear()
        {
            VainDamage = 0;
            Log.Clear();
        }
    }

    public class MobDamageInfo
    {
        public int CharacterID;
        public int Damage;
        public DateTime Time;
    }

    public class PartyDamage
    {
        public int PartyID;
        public int Damage;
        public int MinLevel;
        public int MaxDamage;
        public int MaxDamageCharacter;
        public int MaxDamageLevel;
        public bool bLast;
    }
}