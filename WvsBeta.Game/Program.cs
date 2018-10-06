using System;
using System.IO;
using System.Windows.Forms;
using log4net.Config;
using WvsBeta.Common;

namespace WvsBeta.Game
{
    public class Program
    {
        public static IMainForm MainForm { get; set; }

        public static string IMGFilename { get; set; }
        public static Logfile LogFile { get; private set; }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
#if FORMS
        [STAThread]
#endif
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                MessageBox.Show("Invalid argument length.");
                return;
            }
            log4net.GlobalContext.Properties["ImgName"] = args[0];
            XmlConfigurator.ConfigureAndWatch(new FileInfo(Path.Combine(Environment.CurrentDirectory, "..", "DataSvr", "logging-config-game.xml")));

            IMGFilename = args[0];
            UnhandledExceptionHandler.Set(args, IMGFilename, LogFile);

            MasterThread.Load(IMGFilename);
            LogFile = new Logfile(IMGFilename);
            
            var mainForm = new GameMainForm();
            MainForm = mainForm;
            mainForm.InitializeServer();
#if FORMS
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(mainForm);
#else
            mainForm.ReadInput();
#endif
        }
    }
}
