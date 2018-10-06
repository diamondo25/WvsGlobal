using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WvsBeta.Common
{
    public abstract class MainFormConsole : IMainForm
    {
        private static MainFormConsole _instance = null;

        protected MainFormConsole()
        {
            _instance = this;
            AddShutdownHook();
        }

        public void LogAppend(string what)
        {
            what = what.Trim('\r', '\n', '\t', ' ');
            var keys = log4net.ThreadContext.Properties.GetKeys() ?? new string[0] { };
            var copy = keys.Select(x => new Tuple<string, object>(x, log4net.ThreadContext.Properties[x])).ToArray();
            MasterThread.Instance.AddCallback((date) =>
            {
                copy.ForEach(x => log4net.ThreadContext.Properties[x.Item1] = x.Item2);
                Console.WriteLine(what);
                LogToFile(what);
                copy.ForEach(x => log4net.ThreadContext.Properties.Remove(x.Item1));
            }, "LogAppend");
        }

        public void LogAppend(string pFormat, params object[] pParams)
        {
            LogAppend(string.Format(pFormat, pParams));
        }

        public void LogDebug(string pFormat, params object[] pParams)
        {
#if DEBUG
            LogAppend(string.Format(pFormat, pParams));
#endif
        }

        public abstract void LogToFile(string what);

        public void AddShutdownHook()
        {

            Console.CancelKeyPress += (sender, args) =>
            {
                Shutdown(args);
            };
            if (!IsMono)
            {
                handler = ConsoleEventCallback;
                SetConsoleCtrlHandler(handler, true);
            }
        }

        public abstract void ChangeLoad(bool up);

        public abstract void Shutdown();

        public abstract void InitializeServer();

        public abstract void Shutdown(ConsoleCancelEventArgs args);

        public abstract void HandleCommand(string name, string[] args);

        public void ReadInput()
        {
            
            while (true)
            {
                var line = Console.ReadLine();

                // Might use many CPU cycles, but the server could be shutting down. (ctrl+c sets null)
                if (line == null) continue;

                var str = line.Split(' ');

                switch (str[0])
                {
                    case "shutdown": Shutdown(null); break;
                    default: HandleCommand(str[0], str.Skip(1).ToArray()); break;
                }
            }
        }


        static bool ConsoleEventCallback(CtrlTypes eventType)
        {
            Console.WriteLine("Console window closing, death imminent");
            _instance.Shutdown(null);
            return true;
        }


        public bool IsMono => Type.GetType ("Mono.Runtime") != null;


        static HandlerRoutine handler;

        #region unmanaged

        // Declare the SetConsoleCtrlHandler function
        // as external and receiving a delegate.
        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);



        // A delegate type to be used as the handler routine
        // for SetConsoleCtrlHandler.
        public delegate bool HandlerRoutine(CtrlTypes CtrlType);

        // An enumerated type for the control messages
        // sent to the handler routine.
        public enum CtrlTypes
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }
        
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hwnd, int message, int wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetConsoleWindow();

        public const int WM_SETICON = 0x80;
        public const int ICON_SMALL = 0;
        public const int ICON_BIG = 1;
        #endregion


    }
}
