using System;

namespace WvsBeta.SharedDataProvider
{
    [Flags]
    public enum CharacterDataFlag
    {
        None = 0x00,
        Stats = 0x01,
        Money = 0x02,
        Equips = 0x04,
        Consume = 0x08,
        Install = 0x10,
        Etc = 0x20,
        Pet = 0x40,
        // Note: slot count is inside the inventories in this version
        Skills = 0x80,
        Quests = 0x100,
        MinigameStats = 0x200,
        FriendRing = 0x400,
        TeleportRock = 0x800,
        All = 0xFFFF,

        Items = Equips | Consume | Install | Etc | Pet
    }
}