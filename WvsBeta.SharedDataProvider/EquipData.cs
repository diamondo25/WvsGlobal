public class EquipData
{
    public string Name { get; set; }
    public int ID { get; set; }
    public bool Cash { get; set; }
    public byte HealHP { get; set; }
    public byte Slots { get; set; }
    public byte RequiredLevel { get; set; }
    public ushort RequiredStrength { get; set; }
    public ushort RequiredDexterity { get; set; }
    public ushort RequiredIntellect { get; set; }
    public ushort RequiredLuck { get; set; }
    public ushort RequiredJob { get; set; }
    public int Price { get; set; }
    public byte RequiredFame { get; set; }
    public short HP { get; set; }
    public short MP { get; set; }
    public short Strength { get; set; }
    public short Dexterity { get; set; }
    public short Intellect { get; set; }
    public short Luck { get; set; }
    public byte Hands { get; set; }
    public byte WeaponAttack { get; set; }
    public byte MagicAttack { get; set; }
    public byte WeaponDefense { get; set; }
    public byte MagicDefense { get; set; }
    public byte Accuracy { get; set; }
    public byte Avoidance { get; set; }
    public byte Speed { get; set; }
    public byte Jump { get; set; }
    public byte AttackSpeed { get; set; }
    public byte KnockbackRate { get; set; }
    public bool TimeLimited { get; set; }
    public float RecoveryRate { get; set; } = 1.0f;
}