using System.Collections.Generic;
using WvsBeta.Game;

public class SkillData
{
    public int ID { get; set; }
    public Dictionary<int, byte> RequiredSkills { get; set; }
    public byte Type { get; set; }
    public SkillLevelData[] Levels { get; set; }
    public SkillElement Element { get; set; }
    public byte Weapon { get; set; }
    public byte MaxLevel { get; set; }
}

