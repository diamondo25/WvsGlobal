using WvsBeta.Game;

public class SkillLevelData
{
    public byte MobCount { get; set; }
    public byte HitCount { get; set; }

    public int BuffTime { get; set; }
    public short Damage { get; set; }
    public short AttackRange { get; set; }
    public byte Mastery { get; set; }

    public short HPProperty { get; set; }
    public short MPProperty { get; set; }
    public short Property { get; set; }

    public short HPUsage { get; set; }
    public short MPUsage { get; set; }
    public int ItemIDUsage { get; set; }
    public short ItemAmountUsage { get; set; }
    public short BulletUsage { get; set; }
    public short MesosUsage { get; set; }

    public short XValue { get; set; }
    public short YValue { get; set; }

    public short Speed { get; set; }
    public short Jump { get; set; }
    public short WeaponAttack { get; set; }
    public short MagicAttack { get; set; }
    public short WeaponDefense { get; set; }
    public short MagicDefense { get; set; }
    public short Accurancy { get; set; }
    public short Avoidability { get; set; }

    public SkillElement ElementFlags { get; set; }

    public short LTX { get; set; }
    public short LTY { get; set; }
    public short RBX { get; set; }
    public short RBY { get; set; }


}