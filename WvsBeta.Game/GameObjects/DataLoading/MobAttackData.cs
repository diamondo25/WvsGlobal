public class MobAttackData
{
    public byte ID { get; set; }
    public short MPConsume { get; set; }
    public short PADamage { get; set; }
    public byte Type { get; set; }
    public char ElemAttr { get; set; }
    public short RangeLTX { get; set; }
    public short RangeLTY { get; set; }
    public short RangeRBX { get; set; }
    public short RangeRBY { get; set; }
    public short RangeR { get; set; }
    public short RangeSPX { get; set; }
    public short RangeSPY { get; set; }
    public bool Magic { get; set; }
    // Actually skillid
    public byte Disease { get; set; }
    public byte SkillLevel { get; set; }
}