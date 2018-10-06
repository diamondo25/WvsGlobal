using System;

[Flags]
public enum FieldLimit
{
    MoveLimit = 0x01,
    SkillLimit = 0x02,
    SummonLimit = 0x04,
    MysticDoorLimit = 0x08,

    MigrateLimit = 0x10,
    PortalScrollLimit = 0x20,
    TeleportItemLimit = 0x40,
    MinigameLimit = 0x80,

    SpecificPortalScrollLimit = 0x0100,
    TamingMobLimit = 0x0200,
    StatChangeItemConsumeLimit = 0x0400,
    PartyBossChangeLimit = 0x0800,

    NoMobCapacityLimit = 0x1000,
    WeddingInvitationLimit = 0x2000,
}