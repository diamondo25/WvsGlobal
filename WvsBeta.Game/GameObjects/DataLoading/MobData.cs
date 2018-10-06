using System;
using System.Collections.Generic;
using WvsBeta.Game;

public class MobData
{
    public int ID { get; set; }
    public byte Level { get; set; }
    public bool Boss { get; set; }
    public bool Undead { get; set; }
    public bool BodyAttack { get; set; }
    public int EXP { get; set; }
    public int MaxHP { get; set; }
    public int MaxMP { get; set; }
    public int HPRecoverAmount { get; set; }
    public int MPRecoverAmount { get; set; }
    public int HPTagColor { get; set; }
    public int HPTagBgColor { get; set; }
    public short Speed { get; set; }
    public byte SummonType { get; set; }
    public bool Flies { get; set; }
    public bool Jumps { get; set; }
    public bool PublicReward { get; set; }
    public bool ExplosiveReward { get; set; }
    public List<int> Revive { get; set; }
    public Dictionary<byte, MobAttackData> Attacks { get; set; }
    public List<MobSkillData> Skills { get; set; }
    public float FS { get; set; }
    public int Eva { get; set; }
    public int Acc { get; set; }
    public int PAD { get; set; }
    public int PDD { get; set; }
    public int MAD { get; set; }
    public int MDD { get; set; }
    public string Name { get; set; }


    private string _elemAttr;
    public string elemAttr
    {
        get => _elemAttr;
        set
        {
            _elemAttr = value;
            SkillElement GetElemByName(char name)
            {
                switch (name)
                {
                    case 'F':
                        return SkillElement.Fire;
                    case 'S':
                        return SkillElement.Poison;
                    case 'I':
                        return SkillElement.Ice;
                    case 'L':
                        return SkillElement.Lightning;
                    case 'H':
                        return SkillElement.Holy;
                    default:
                        Program.MainForm.LogAppend("Unknown element when calculating damage: " + name);
                        return SkillElement.Normal;
                }
            }
            try
            {
                for (int i = 0; i < _elemAttr.Length; i += 2)
                {
                    elemModifiers.Add(GetElemByName(_elemAttr[i]), int.Parse(_elemAttr[i + 1].ToString()));
                }
            }
            catch (Exception ex)
            {
                Program.MainForm.LogAppend(ex.ToString());
                // ¯\_(ツ)_/¯
            }
        }
    }
    public Dictionary<SkillElement, int> elemModifiers { get; private set; } = new Dictionary<SkillElement, int>();
    public bool Pushed { get; set; }
    public bool NoRegen { get; set; }
    public bool Invincible { get; set; }
    public bool FirstAttack { get; set; }
    public bool SelfDestruction { get; set; }
}