using System;
using System.Collections.Generic;

public class ItemData
{
    [Flags]
    public enum CureFlags
    {
        Poison = 0x1,
        Weakness = 0x2,
        Curse = 0x4,
        Darkness = 0x8,
        Seal = 0x10,
    }

    public string Name { get; set; }
    public int ID { get; set; }
    public int Price { get; set; }
    public bool Cash { get; set; }
    public ushort MaxSlot { get; set; }
    public bool IsQuest { get; set; }
    public short HP { get; set; }
    public short MP { get; set; }
    public short HPRate { get; set; }
    public short MPRate { get; set; }
    public short WeaponAttack { get; set; }
    public short WeaponDefense { get; set; }
    public short MagicAttack { get; set; }
    public short Accuracy { get; set; }
    public short Avoidance { get; set; }
    public short Speed { get; set; }
    public int BuffTime { get; set; }
    public short Thaw { get; set; }
    public CureFlags Cures { get; set; }

    public int MoveTo { get; set; }
    public int Mesos { get; set; }

    public byte ScrollSuccessRate { get; set; }
    public byte ScrollCurseRate { get; set; }
    public byte IncStr { get; set; }
    public byte IncDex { get; set; }
    public byte IncInt { get; set; }
    public byte IncLuk { get; set; }
    public byte IncMHP { get; set; }
    public byte IncMMP { get; set; }
    public byte IncWAtk { get; set; }
    public byte IncMAtk { get; set; }
    public byte IncWDef { get; set; }
    public byte IncMDef { get; set; }
    public byte IncAcc { get; set; }
    public byte IncAvo { get; set; }
    public byte IncJump { get; set; }
    public byte IncSpeed { get; set; }
    public byte Rate { get; set; }
    public bool TimeLimited { get; set; }

    // Summon type
    public sbyte Type { get; set; }
    public List<ItemSummonInfo> Summons { get; set; }

    public Dictionary<byte, List<KeyValuePair<byte, byte>>> RateTimes { get; set; } = null;

    public const byte HOLIDAY_DAY = 20;
    public static bool RateCardEnabled(ItemData pItemData, bool pIsHoliday = false)
    {
        if (pItemData.RateTimes == null) return false;

        DateTime now = DateTime.Now;
        byte currentDay = pIsHoliday && pItemData.RateTimes.ContainsKey(HOLIDAY_DAY) ? HOLIDAY_DAY : (byte)now.DayOfWeek;


        if (!pItemData.RateTimes.ContainsKey(currentDay)) return false;

        foreach (var kvp in pItemData.RateTimes[currentDay])
        {
            if (kvp.Key <= now.Hour && kvp.Value >= now.Hour)
            {
                return true;
            }
        }
        return false;
    }
}