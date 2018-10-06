using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;

namespace WvsBeta.Game.Handlers.Commands
{
    [AttributeUsage(AttributeTargets.Method)]
    public class CommandHandlerAttribute : Attribute
    {
        public string CommandName { get; set; }
        
        public string[] Aliases { get; set; }

        public string Help { get; set; } = "";

        public UserAdminLevels UserRanks { get; set; } = 0;
    }
}
