using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;



namespace WvsBeta.Game.Handlers.Commands
{
    using CommandHandleFunc = Func<Character, string, CommandHandling.CommandArgs, bool>;

    static class MainCommandHandler
    {

        private struct CommandInfo
        {
            public UserAdminLevels Ranks { get; set; }
            public CommandHandleFunc Handler { get; set; }
        }

        private static readonly Dictionary<string, CommandInfo> _commands = new Dictionary<string, CommandInfo>();

        public static void ReloadCommands()
        {
            var methods =
                Assembly.GetExecutingAssembly()
                    .GetTypes()
                    .SelectMany(x => x.GetMethods(BindingFlags.Static | BindingFlags.Public))
                    .Where(x => Attribute.IsDefined(x, typeof(CommandHandlerAttribute)));

            _commands.Clear();

            foreach (var methodInfo in methods)
            {
                var attr = (CommandHandlerAttribute)methodInfo.GetCustomAttribute(typeof(CommandHandlerAttribute));

                var cmdHandler = (CommandHandleFunc)methodInfo.CreateDelegate(typeof(CommandHandleFunc), null);

                var cmdInfo = new CommandInfo
                {
                    Handler = cmdHandler,
                    Ranks = attr.UserRanks
                };

                _commands[attr.CommandName.ToLowerInvariant()] = cmdInfo;

                foreach (var alias in attr.Aliases)
                {
                    _commands[alias.ToLowerInvariant()] = cmdInfo;
                }
            }

            foreach (var rank in Enum.GetValues(typeof(UserAdminLevels)))
            {
                var actualRank = (UserAdminLevels)rank;
                var commandsForRank = _commands.Count(x => x.Value.Ranks.HasFlag(actualRank));
                Trace.WriteLine($"Loaded {commandsForRank} commands for {actualRank}");
            }
        }


        public static Character CommandCharacter = null;

        public static bool HandleCommand(Character chr, CommandHandling.CommandArgs args)
        {
            if (args.Sign != '!' && args.Sign != '/') return false;
            try
            {
                var initialCommand = args.Command.ToLowerInvariant();
                if (_commands.TryGetValue(initialCommand, out var handler))
                {
                    var rank = UserAdminLevels.NormalPlayer;
                    if (Server.Tespia) rank |= UserAdminLevels.Tespian;
                    if (chr.BetaPlayer) rank |= UserAdminLevels.BetaPlayer;

                    if (chr.GMLevel >= 1) rank |= UserAdminLevels.Intern;
                    if (chr.GMLevel >= 2) rank |= UserAdminLevels.GM;
                    if (chr.GMLevel >= 3) rank |= UserAdminLevels.Admin;

                    Trace.WriteLine($"User ranks: {rank}");
                    CommandCharacter = chr;
                    if ((handler.Ranks & rank) != 0)
                    {
                        return handler.Handler(chr, initialCommand, args);
                    }
                    else
                    {
                        HandlerHelpers.ShowError("You cannot use this command.");
                    }
                }
            }
            catch
            {

            }
            finally
            {
                CommandCharacter = null;

            }

            return false;
        }
    }
}
