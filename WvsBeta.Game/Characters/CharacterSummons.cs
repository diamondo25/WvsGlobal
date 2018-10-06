using WvsBeta.Common;
using System.Collections.Generic;
using WvsBeta.Game.GameObjects;
using System.Linq;
using WvsBeta.Common.Sessions;

namespace WvsBeta.Game
{
    public class CharacterSummons
    {
        public readonly Character Chr;
        private readonly Dictionary<int, Summon> Summons = new Dictionary<int, Summon>();

        public CharacterSummons(Character c)
        {
            Chr = c;
        }

        public void RemoveSummon(int skillid)
        {
            if (GetSummon(skillid, out var summon))
            {
                Chr.Field.Summons.DeregisterSummon(summon, 1);
                Summons.Remove(skillid);
            }
        }

        public void SetSummon(Summon sum)
        {
            RemoveSummon(sum.SkillId);
            Summons[sum.SkillId] = sum;
            Chr.Field.Summons.RegisterSummon(sum);
        }

        public bool GetSummon(int skillid, out Summon summon)
        {
            return Summons.TryGetValue(skillid, out summon);
        }

        public void RemovePuppet()
        {
            RemoveSummon(Constants.Ranger.Skills.Puppet);
            RemoveSummon(Constants.Sniper.Skills.Puppet);
        }

        public void MigrateSummons(Map oldField, Map newField)
        {
            foreach (var summon in Summons.Values)
            {
                if (summon != null)
                {
                    oldField.Summons.DeregisterSummon(summon, 0);
                    if (!(summon is Puppet))
                    {
                        newField.Summons.RegisterSummon(summon);
                    }
                }
            }
        }

        public void RemoveAllSummons()
        {
            foreach (var summon in Summons.Values.ToList())
            {
                if (summon != null)
                {
                    RemoveSummon(summon.SkillId);
                }
            }
        }

        public void Update(long tCur)
        {
            foreach (var summon in Summons.Values.ToList())
            {
                if (summon != null && tCur > summon.ExpireTime)
                {
                    RemoveSummon(summon.SkillId);
                }
            }
        }

        public void EncodeForCC(Packet pw)
        {
            //puppet doesnt transfer channels. Also, doesn't require summoning rock, so just recast it
            var summonsList = Summons.Values.Where(s => !(s is Puppet)).ToList();

            pw.WriteInt(summonsList.Count);

            foreach (var summon in summonsList)
            {
                pw.WriteInt(summon.SkillId);
                pw.WriteByte(summon.SkillLevel);
                pw.WriteBool(summon.MoveAction);
                pw.WriteUShort(summon.FootholdSN);
                pw.WriteLong(summon.ExpireTime);
                pw.WriteShort(summon.Position.X);
                pw.WriteShort(summon.Position.Y);
            }
        }

        public void DecodeForCC(Packet pw)
        {
            int numSummons = pw.ReadInt();

            for (int i = 0; i < numSummons; i++)
            {
                var skillId = pw.ReadInt();
                var skillLevel = pw.ReadByte();
                var moveAction = pw.ReadBool();
                var footholdSN = pw.ReadUShort();
                var expireTime = pw.ReadLong();
                var x = pw.ReadShort();
                var y = pw.ReadShort();

                var summon = new Summon(Chr, skillId, skillLevel, x, y, moveAction, footholdSN, expireTime);
                SetSummon(summon);
            }
        }
    }
}
