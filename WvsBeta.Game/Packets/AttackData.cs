using System.Collections.Generic;
using WvsBeta.Common;

namespace WvsBeta.Game
{
    public class AttackData
    {
        public struct AttackInfo
        {
            public int MobMapId { get; set; }
            public byte HitAction { get; set; }
            public byte ForeAction { get; set; }
            public bool FacesLeft { get; set; }
            public byte FrameIndex { get; set; } // Hit frame
            public byte CalcDamageStatIndex { get; set; }
            public bool Doomed { get; set; }
            public Pos HitPosition { get; set; }
            public Pos PreviousMobPosition { get; set; }
            public short HitDelay { get; set; } // Effect from Meso Explosion (no instant hits)
            public List<int> Damages { get; set; }
        }

        public int SkillID { get; set; }
        public byte SkillLevel { get; set; }
        public byte Option { get; set; }
        public byte Targets { get; set; }
        public byte Hits { get; set; }
        public byte Action { get; set; }
        public bool FacesLeft { get; set; }
        public byte WeaponSpeed { get; set; }
        public byte AttackType { get; set; }
        public byte WeaponClass { get; set; }
        public short StarItemSlot { get; set; }
        public byte ShootRange { get; set; }
        public int Charge { get; set; }
        public int StarID { get; set; }
        public int SummonID { get; set; }
        public long TotalDamage { get; set; }

        public Pos ProjectilePosition { get; set; }
        public Pos PlayerPosition { get; set; }

        public List<AttackInfo> Attacks { get; set; } = new List<AttackInfo>();

        public bool IsMesoExplosion { get; set; }

        public uint RandomNumber { get; set; }

        public AttackData()
        {
            IsMesoExplosion = false;
        }
    }
}