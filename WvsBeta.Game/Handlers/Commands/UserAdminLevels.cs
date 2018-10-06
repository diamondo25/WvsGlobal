using System;

namespace WvsBeta.Game.Handlers.Commands
{
    [Flags]
    public enum UserAdminLevels
    {
        NormalPlayer = 0x1,
        Tespian = 0x2,
        BetaPlayer = 0x3,

        Intern = 0x10,
        GM = 0x20,
        Admin = 0x40,

        ALL = Int32.MaxValue,
        GmIntern = GM | Intern,
        AdminGmIntern = Admin | GmIntern,
    }

}
