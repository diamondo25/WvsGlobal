using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using WvsBeta.Common;
using WvsBeta.Common.Sessions;
using WvsBeta.Common.Tracking;

namespace WvsBeta.Game
{
    public class CharacterSkills
    {
        private Character Character { get; }
        public Dictionary<int, byte> Skills { get; } = new Dictionary<int, byte>();

        public CharacterSkills(Character character)
        {
            Character = character;
        }

        public void SaveSkills()
        {
            var id = Character.ID;
            var query = "DELETE FROM skills WHERE charid = " + id + "; ";

            if (Skills.Count > 0)
            {
                query += "INSERT INTO skills (charid, skillid, points) VALUES ";
                query += string.Join(", ", Skills.Select(kvp => "(" + id + ", " + kvp.Key + ", " + kvp.Value + ")"));
                query += ";";
            }

            Server.Instance.CharacterDatabase.RunQuery(query);
        }

        public void LoadSkills()
        {
            using (var reader = Server.Instance.CharacterDatabase.RunQuery(
                    "SELECT skillid, points FROM skills WHERE charid = @charid",
                    "@charid", Character.ID) as MySqlDataReader)
            {
                while (reader.Read())
                {
                    Skills.Add(reader.GetInt32("skillid"), (byte)reader.GetInt16("points"));
                }
            }
        }

        public void AddSkillPoint(int skillid)
        {
            byte newLevel;
            if (Skills.TryGetValue(skillid, out newLevel))
                newLevel++;
            else
                newLevel = 1;
            Skills[skillid] = newLevel;

            SkillPacket.SendAddSkillPoint(Character, skillid, newLevel);

            Character.FlushDamageLog();
        }

        public void SetSkillPoint(int skillid, byte level, bool packet = true)
        {
            Skills[skillid] = level;
            if (packet)
                SkillPacket.SendAddSkillPoint(Character, skillid, Skills[skillid]);

            Character.FlushDamageLog();
        }

        public void AddSkills(Packet packet)
        {
            packet.WriteShort((short)Skills.Count);

            foreach (var kvp in Skills)
            {
                packet.WriteInt(kvp.Key);
                packet.WriteInt(kvp.Value);
            }
        }

        public void DoSkillCost(int skillid, byte level)
        {
            var data = GetSkillLevelData(skillid, level);
            if (data == null)
                return;

            if (skillid == Constants.DragonKnight.Skills.DragonRoar)
            {
                var lefthp = (int)(Character.PrimaryStats.MaxHP * (data.XValue / 100.0d));
                Character.DamageHP((short)lefthp);
            }

            var mp = data.MPUsage;
            var hp = data.HPUsage;
            var cash = data.MesosUsage;
            var item = data.ItemIDUsage;

            if (mp > 0)
            {
                mp = GetElemAmpInc(skillid, mp);
                if (Character.AssertForHack(Character.PrimaryStats.MP < mp, "MP Hack (no MP left)")) return;
                Character.ModifyMP((short)-mp, true);
            }
            if (hp > 0)
            {
                if (Character.AssertForHack(Character.PrimaryStats.HP < hp, "HP Hack (no HP left)")) return;
                Character.ModifyHP((short)-hp, true);
            }
            if (item > 0)
            {
                var slotsAvailable = Character.Inventory.ItemAmountAvailable(item);
                if (Character.AssertForHack(slotsAvailable < data.ItemAmountUsage,
                    "Player tried to use skill with item consumption. Not enough items to consume.")) return;

                Character.Inventory.TakeItem(item, data.ItemAmountUsage);
                ItemTransfer.PlayerUsedSkill(Character.ID, skillid, item, data.ItemAmountUsage);
            }

            if (cash > 0 && false)
            {
                var min = (short)(cash - (80 + level * 5));
                var max = (short)(cash + (80 + level * 5));
                var realAmount = (short)Character.CalcDamageRandomizer.ValueBetween(min, max);
                if (Character.AssertForHack(((long)Character.Inventory.Mesos - realAmount) < 0,
                    "Player tried to use skill with meso consumption. Not enough mesos.")) return;

                Character.AddMesos(-realAmount);
                MesosTransfer.PlayerUsedSkill(Character.ID, realAmount, skillid);
            }
        }

        private short GetElemAmpInc(int skillId, short mp)
        {
            double inc = 100.0;
            if (Character.PrimaryStats.Job == 211) //f/p
            {
                var level = Character.Skills.GetSkillLevel(Constants.FPMage.Skills.ElementAmplification, out SkillLevelData sld);
                if (level != 0)
                    inc = sld.XValue;
            }
            else if (Character.PrimaryStats.Job == 221) //i/l
            {
                var level = Character.Skills.GetSkillLevel(Constants.ILMage.Skills.ElementAmplification, out SkillLevelData sld);
                if (level != 0)
                    inc = sld.XValue;
            }
            
            switch(skillId)
            {
                case Constants.Magician.Skills.EnergyBolt:
                case Constants.Magician.Skills.MagicClaw:
                //case 2001006: DNE

                case Constants.FPWizard.Skills.FireArrow:
                case Constants.FPWizard.Skills.PoisonBreath:
                case Constants.FPMage.Skills.Explosion:
                case Constants.FPMage.Skills.ElementComposition:
                //case 2121001:
                //case 2121002:
                //case 2121003:
                //case 2121006:
                //case 2121007:

                case Constants.ILWizard.Skills.ColdBeam:
                case Constants.ILWizard.Skills.ThunderBolt:
                case Constants.ILMage.Skills.IceStrike:
                case Constants.ILMage.Skills.Lightening:
                case Constants.ILMage.Skills.ElementComposition:
                //case 2221001:
                //case 2221003:
                //case 2221006:
                //case 2221007:
                    return (short)((inc / 100.0) * mp);
            }
            return mp;
        }

        public void UseMeleeAttack(int skillid, AttackData attackData)
        {
            if (!DataProvider.Skills.TryGetValue(skillid, out var skillData)) return;

            var level = (byte)(Skills.ContainsKey(skillid) ? Skills[skillid] : 0);
            if (level == 0) return;

            if (skillid != Constants.Cleric.Skills.Heal) //fix heal double consumption
                DoSkillCost(skillid, level);

            var sld = skillData.Levels[level];
            if (skillid == Constants.FPMage.Skills.PoisonMyst)
            {
                short delay = 700;
                if (attackData.Attacks.Count > 0)
                    delay = attackData.Attacks[0].HitDelay;

                Character.Field.CreateMist(Character, Character.ID, skillid, level, sld.BuffTime, sld.LTX, sld.LTY, sld.RBX, sld.RBY, delay);
            }
        }


        public void UseRangedAttack(int skillid, short pos)
        {
            Program.MainForm.LogDebug("Using ranged. Skill: " + skillid);
            byte level = 0;
            if (skillid != 0)
            {
                if (!DataProvider.Skills.ContainsKey(skillid)) return;

                level = (byte)(Skills.ContainsKey(skillid) ? Skills[skillid] : 0);
                if (level == 0) return;
                DoSkillCost(skillid, level);
            }
            short hits = 1;
            if (skillid != 0)
            {
                var bullets = DataProvider.Skills[skillid].Levels[level].BulletUsage;
                if (bullets > 0)
                    hits = bullets;
            }
            if (Character.PrimaryStats.HasBuff((int)BuffValueTypes.ShadowPartner) && (Character.PrimaryStats.Job == 411 || Character.PrimaryStats.Job == 500))
            {
                hits *= 2;
            }
            if (pos > 0 && !Character.PrimaryStats.HasBuff((int)BuffValueTypes.SoulArrow))
            {
                Character.Inventory.TakeItemAmountFromSlot(2, pos, hits, false);
            }
        }

        public byte GetSkillLevel(int skillid)
        {
            if (Skills.TryGetValue(skillid, out byte level)) return level;
            return 0;
        }

        public byte GetSkillLevel(int skillid, out SkillLevelData data)
        {
            data = GetSkillLevelData(skillid, out byte level);
            return level;
        }

        public double GetSpellAttack(int spellId)
        {
            return DataProvider.Skills[spellId].Levels[Character.Skills.GetSkillLevel(spellId)].MagicAttack;
        }

        public double GetSpellMastery(int spellId)
        {
            return DataProvider.Skills[spellId].Levels[Character.Skills.GetSkillLevel(spellId)].Mastery;
        }

        public ushort GetRechargeableBonus()
        {
            ushort bonus = 0;
            switch (Character.PrimaryStats.Job)
            {
                case Constants.Assassin.ID:
                case Constants.Hermit.ID:
                    bonus = (ushort)(GetSkillLevel(Constants.Assassin.Skills.ClawMastery) * 10);
                    break;
            }
            return bonus;
        }

        public int GetMastery()
        {
            var masteryid = 0;
            switch (Constants.getItemType(Character.Inventory.GetEquippedItemId((short)Constants.EquipSlots.Slots.Weapon, false)))
            {
                case (int)Constants.Items.Types.ItemTypes.Weapon1hSword:
                case (int)Constants.Items.Types.ItemTypes.Weapon2hSword:
                    switch (Character.PrimaryStats.Job)
                    {
                        case Constants.Fighter.ID:
                        case Constants.Crusader.ID:
                            masteryid = Constants.Fighter.Skills.SwordMastery;
                            break;
                        case Constants.Page.ID:
                        case Constants.WhiteKnight.ID:
                            masteryid = Constants.Page.Skills.SwordMastery;
                            break;
                    }
                    break;
                case (int)Constants.Items.Types.ItemTypes.Weapon1hAxe:
                case (int)Constants.Items.Types.ItemTypes.Weapon2hAxe:
                    masteryid = Constants.Fighter.Skills.AxeMastery;
                    break;
                case (int)Constants.Items.Types.ItemTypes.Weapon1hMace:
                case (int)Constants.Items.Types.ItemTypes.Weapon2hMace:
                    masteryid = Constants.Page.Skills.BwMastery;
                    break;
                case (int)Constants.Items.Types.ItemTypes.WeaponSpear: masteryid = Constants.Spearman.Skills.SpearMastery; break;
                case (int)Constants.Items.Types.ItemTypes.WeaponPolearm: masteryid = Constants.Spearman.Skills.PolearmMastery; break;
                case (int)Constants.Items.Types.ItemTypes.WeaponDagger: masteryid = Constants.Bandit.Skills.DaggerMastery; break;
                case (int)Constants.Items.Types.ItemTypes.WeaponBow: masteryid = Constants.Hunter.Skills.BowMastery; break;
                case (int)Constants.Items.Types.ItemTypes.WeaponCrossbow: masteryid = Constants.Crossbowman.Skills.CrossbowMastery; break;
                case (int)Constants.Items.Types.ItemTypes.WeaponClaw: masteryid = Constants.Assassin.Skills.ClawMastery; break;
            }
            return masteryid;
        }

        public int GetMpStealSkillData(int attackType, out int prop, out int precent, out byte level)
        {
            SkillLevelData data = null;
            if (attackType == 2)
            {
                if ((level = GetSkillLevel(Constants.FPWizard.Skills.MpEater, out data)) > 0)
                {
                    prop = data.Property;
                    precent = data.XValue;
                    return Constants.FPWizard.Skills.MpEater;
                }

                else if ((level = GetSkillLevel(Constants.ILWizard.Skills.MpEater, out data)) > 0)
                {
                    prop = data.Property;
                    precent = data.XValue;
                    return Constants.ILWizard.Skills.MpEater;
                }

                else if ((level = GetSkillLevel(Constants.Cleric.Skills.MpEater, out data)) > 0)
                {
                    prop = data.Property;
                    precent = data.XValue;
                    return Constants.Cleric.Skills.MpEater;
                }
            }
            return prop = precent = level = 0;
        }

        public SkillLevelData GetSkillLevelData(int skill) => GetSkillLevelData(skill, out byte level);

        public SkillLevelData GetSkillLevelData(int skill, out byte level)
        {
            if (Skills.TryGetValue(skill, out level))
            {
                return GetSkillLevelData(skill, level);
            }
            return null;
        }

        public static SkillLevelData GetSkillLevelData(int skill, byte level)
        {
            if (DataProvider.Skills.TryGetValue(skill, out var skillData))
            {
                if (skillData.MaxLevel >= level) return skillData.Levels[level];
            }

            return null;
        }
    }
}