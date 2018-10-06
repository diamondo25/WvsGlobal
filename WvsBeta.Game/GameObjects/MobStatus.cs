using System;
using WvsBeta.Common.Sessions;

namespace WvsBeta.Game
{
    public class MobStatus
    {
        public MobStatus(Mob pMob)
        {
            Mob = pMob;
        }
        public Mob Mob { get; private set; }


        public class MobBuffStat
        {
            // Number. Most of the time, this is the X or Y value of the skill/buff
            public short N { get; set; }
            // Reference ID. For Item IDs, use a negative number
            public int R { get; set; }
            // Expire Time. Extended version of T (full time in millis)
            public long TM { get; set; }
            public MobStatValue Flag { get; set; }

            public bool IsSet(long? time = null)
            {
                if (N == 0) return false;
                if (time == null) time = MasterThread.CurrentTime;
                return TM > time;
            }

            public bool HasReferenceId(int referenceId, long? currenTime = null)
            {
                return IsSet(currenTime) && R == referenceId;
            }

            public MobBuffStat(MobStatValue flag)
            {
                Flag = flag;
                N = 0;
                R = 0;
                TM = 0;
            }

            public MobStatValue Reset()
            {
                if (N == 0) return 0;
                N = 0;
                R = 0;
                TM = 0;
                return Flag;
            }

            public void TryReset(long currentTime, ref MobStatValue flag)
            {
                if (N == 0 || TM >= currentTime) return;
                Reset();
                flag |= Flag;
            }

            public void TryResetByReference(int reference, ref MobStatValue flag)
            {
                if (N == 0 || R != reference) return;
                Reset();
                flag |= Flag;
            }

            public MobStatValue Set(int referenceId, short nValue, long expireTime)
            {
                R = referenceId;
                N = nValue;
                TM = expireTime;
                return Flag;
            }

            public void EncodeForRemote(ref MobStatValue flag, long currentTime, Action<MobBuffStat> func, MobStatValue specificFlag = MobStatValue.ALL)
            {
                if (!IsSet(currentTime) || !specificFlag.HasFlag(Flag)) return;

                flag |= Flag;
                func?.Invoke(this);
            }

            public void EncodeForLocal(Packet pw, ref MobStatValue flag, long currentTime, MobStatValue specificFlag = MobStatValue.ALL)
            {
                if (!IsSet(currentTime) || !specificFlag.HasFlag(Flag)) return;

                flag |= Flag;
                pw.WriteShort(N);
                pw.WriteInt(R);
                pw.WriteShort((short)((TM - currentTime) / 100)); // Not sure what value this should be...
            }
        }

        public MobBuffStat BuffPhysicalDamage { get; } = new MobBuffStat(MobStatValue.PhysicalDamage);
        public MobBuffStat BuffPhysicalDefense { get; } = new MobBuffStat(MobStatValue.PhysicalDefense);
        public MobBuffStat BuffMagicDamage { get; } = new MobBuffStat(MobStatValue.MagicDamage);
        public MobBuffStat BuffMagicDefense { get; } = new MobBuffStat(MobStatValue.MagicDefense);
        public MobBuffStat BuffAccurrency { get; } = new MobBuffStat(MobStatValue.Accurrency);
        public MobBuffStat BuffEvasion { get; } = new MobBuffStat(MobStatValue.Evasion);
        public MobBuffStat BuffSpeed { get; } = new MobBuffStat(MobStatValue.Speed);
        public MobBuffStat BuffStun { get; } = new MobBuffStat(MobStatValue.Stun);
        public MobBuffStat BuffFreeze { get; } = new MobBuffStat(MobStatValue.Freeze);
        public MobBuffStat BuffPoison { get; } = new MobBuffStat(MobStatValue.Poison);
        public MobBuffStat BuffSeal { get; } = new MobBuffStat(MobStatValue.Seal);
        public MobBuffStat BuffDarkness { get; } = new MobBuffStat(MobStatValue.Darkness);
        public MobBuffStat BuffPowerUp { get; } = new MobBuffStat(MobStatValue.PowerUp);
        public MobBuffStat BuffMagicUp { get; } = new MobBuffStat(MobStatValue.MagicUp);
        public MobBuffStat BuffPowerGuardUp { get; } = new MobBuffStat(MobStatValue.PowerGuardUp);
        public MobBuffStat BuffMagicGuardUp { get; } = new MobBuffStat(MobStatValue.MagicGuardUp);
        public MobBuffStat BuffDoom { get; } = new MobBuffStat(MobStatValue.Doom);
        public MobBuffStat BuffWeb { get; } = new MobBuffStat(MobStatValue.Web);
        public MobBuffStat BuffPhysicalImmune { get; } = new MobBuffStat(MobStatValue.PhysicalImmune);
        public MobBuffStat BuffMagicImmune { get; } = new MobBuffStat(MobStatValue.MagicImmune);
        public MobBuffStat BuffHardSkin { get; } = new MobBuffStat(MobStatValue.HardSkin);
        public MobBuffStat BuffAmbush { get; } = new MobBuffStat(MobStatValue.Ambush);
        public MobBuffStat BuffVenom { get; } = new MobBuffStat(MobStatValue.Venom);
        public MobBuffStat BuffBlind { get; } = new MobBuffStat(MobStatValue.Blind);
        public MobBuffStat BuffSealSkill { get; } = new MobBuffStat(MobStatValue.SealSkill);


        [Flags]
        public enum MobStatValue : uint
        {
            PhysicalDamage = 0x1,
            PhysicalDefense = 0x2,
            MagicDamage = 0x4,
            MagicDefense = 0x8,
            Accurrency = 0x10,
            Evasion = 0x20,
            Speed = 0x40,
            Stun = 0x80,
            Freeze = 0x100,
            Poison = 0x200,
            Seal = 0x400,
            Darkness = 0x800,
            PowerUp = 0x1000,
            MagicUp = 0x2000,
            PowerGuardUp = 0x4000,
            MagicGuardUp = 0x8000,
            Doom = 0x10000,
            Web = 0x20000,
            PhysicalImmune = 0x40000,
            MagicImmune = 0x80000,
            HardSkin = 0x200000,
            Ambush = 0x400000,
            Venom = 0x1000000,
            Blind = 0x2000000,
            SealSkill = 0x4000000,

            ALL = 0xFFFFFFFF,
        }



        public void Encode(Packet pPacket, MobStatValue pSpecificFlag = MobStatValue.ALL)
        {
            long currentTime = MasterThread.CurrentTime;
            int tmpBuffPos = pPacket.Position;
            MobStatValue endFlag = 0;
            pPacket.WriteUInt((uint)endFlag);

            BuffPhysicalDamage.EncodeForLocal(pPacket, ref endFlag, currentTime, pSpecificFlag);
            BuffPhysicalDefense.EncodeForLocal(pPacket, ref endFlag, currentTime, pSpecificFlag);
            BuffMagicDamage.EncodeForLocal(pPacket, ref endFlag, currentTime, pSpecificFlag);
            BuffMagicDefense.EncodeForLocal(pPacket, ref endFlag, currentTime, pSpecificFlag);
            BuffAccurrency.EncodeForLocal(pPacket, ref endFlag, currentTime, pSpecificFlag);
            BuffEvasion.EncodeForLocal(pPacket, ref endFlag, currentTime, pSpecificFlag);
            BuffSpeed.EncodeForLocal(pPacket, ref endFlag, currentTime, pSpecificFlag);
            BuffStun.EncodeForLocal(pPacket, ref endFlag, currentTime, pSpecificFlag);
            BuffFreeze.EncodeForLocal(pPacket, ref endFlag, currentTime, pSpecificFlag);
            BuffPoison.EncodeForLocal(pPacket, ref endFlag, currentTime, pSpecificFlag);
            BuffSeal.EncodeForLocal(pPacket, ref endFlag, currentTime, pSpecificFlag);
            BuffDarkness.EncodeForLocal(pPacket, ref endFlag, currentTime, pSpecificFlag);
            BuffPowerUp.EncodeForLocal(pPacket, ref endFlag, currentTime, pSpecificFlag);
            BuffMagicUp.EncodeForLocal(pPacket, ref endFlag, currentTime, pSpecificFlag);
            BuffPowerGuardUp.EncodeForLocal(pPacket, ref endFlag, currentTime, pSpecificFlag);
            BuffMagicGuardUp.EncodeForLocal(pPacket, ref endFlag, currentTime, pSpecificFlag);
            BuffPhysicalImmune.EncodeForLocal(pPacket, ref endFlag, currentTime, pSpecificFlag);
            BuffMagicImmune.EncodeForLocal(pPacket, ref endFlag, currentTime, pSpecificFlag);
            BuffDoom.EncodeForLocal(pPacket, ref endFlag, currentTime, pSpecificFlag);
            BuffWeb.EncodeForLocal(pPacket, ref endFlag, currentTime, pSpecificFlag);
            BuffHardSkin.EncodeForLocal(pPacket, ref endFlag, currentTime, pSpecificFlag);
            BuffAmbush.EncodeForLocal(pPacket, ref endFlag, currentTime, pSpecificFlag);
            BuffVenom.EncodeForLocal(pPacket, ref endFlag, currentTime, pSpecificFlag);
            BuffBlind.EncodeForLocal(pPacket, ref endFlag, currentTime, pSpecificFlag);
            BuffSealSkill.EncodeForLocal(pPacket, ref endFlag, currentTime, pSpecificFlag);

            int tmpBuffPos2 = pPacket.Position;
            pPacket.Position = tmpBuffPos;
            pPacket.WriteUInt((uint)endFlag);
            pPacket.Position = tmpBuffPos2;
        }

        public void Clear()
        {
            BuffPhysicalDamage.Reset();
            BuffPhysicalDefense.Reset();
            BuffMagicDamage.Reset();
            BuffMagicDefense.Reset();
            BuffAccurrency.Reset();
            BuffEvasion.Reset();
            BuffSpeed.Reset();
            BuffStun.Reset();
            BuffFreeze.Reset();
            BuffPoison.Reset();
            BuffSeal.Reset();
            BuffDarkness.Reset();
            BuffPowerUp.Reset();
            BuffMagicUp.Reset();
            BuffPowerGuardUp.Reset();
            BuffMagicGuardUp.Reset();
            BuffPhysicalImmune.Reset();
            BuffMagicImmune.Reset();
            BuffDoom.Reset();
            BuffWeb.Reset();
            BuffHardSkin.Reset();
            BuffAmbush.Reset();
            BuffVenom.Reset();
            BuffBlind.Reset();
            BuffSealSkill.Reset();
        }

        public void Update(long currentTime)
        {
            MobStatValue endFlag = 0;

            BuffPhysicalDamage.TryReset(currentTime, ref endFlag);
            BuffPhysicalDefense.TryReset(currentTime, ref endFlag);
            BuffMagicDamage.TryReset(currentTime, ref endFlag);
            BuffMagicDefense.TryReset(currentTime, ref endFlag);
            BuffAccurrency.TryReset(currentTime, ref endFlag);
            BuffEvasion.TryReset(currentTime, ref endFlag);
            BuffSpeed.TryReset(currentTime, ref endFlag);
            BuffStun.TryReset(currentTime, ref endFlag);
            BuffFreeze.TryReset(currentTime, ref endFlag);
            BuffPoison.TryReset(currentTime, ref endFlag);
            BuffSeal.TryReset(currentTime, ref endFlag);
            BuffDarkness.TryReset(currentTime, ref endFlag);
            BuffPowerUp.TryReset(currentTime, ref endFlag);
            BuffMagicUp.TryReset(currentTime, ref endFlag);
            BuffPowerGuardUp.TryReset(currentTime, ref endFlag);
            BuffMagicGuardUp.TryReset(currentTime, ref endFlag);
            BuffPhysicalImmune.TryReset(currentTime, ref endFlag);
            BuffMagicImmune.TryReset(currentTime, ref endFlag);
            BuffDoom.TryReset(currentTime, ref endFlag);
            BuffWeb.TryReset(currentTime, ref endFlag);
            BuffHardSkin.TryReset(currentTime, ref endFlag);
            BuffAmbush.TryReset(currentTime, ref endFlag);
            BuffVenom.TryReset(currentTime, ref endFlag);
            BuffBlind.TryReset(currentTime, ref endFlag);
            BuffSealSkill.TryReset(currentTime, ref endFlag);

            if (endFlag > 0)
                MobPacket.SendMobStatsTempReset(Mob, endFlag);
        }
    }
}