using System;
using System.IO;
using System.Windows.Forms;
using log4net.Config;
using WvsBeta.Common;

namespace WvsBeta.Login
{
    class Program
    {
        public static frmMain MainForm { get; set; }

        public static string IMGFilename { get; set; }
        public static Logfile LogFile { get; private set; }
        
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                MessageBox.Show("Invalid argument length.");
                return;
            }
            log4net.GlobalContext.Properties["ImgName"] = args[0];
            XmlConfigurator.ConfigureAndWatch(new FileInfo(Path.Combine(Environment.CurrentDirectory, "..", "DataSvr", "logging-config-login.xml")));


            IMGFilename = args[0];
            UnhandledExceptionHandler.Set(args, IMGFilename, LogFile);

            MasterThread.Load(IMGFilename);
            LogFile = new Logfile(IMGFilename);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(MainForm = new frmMain());

        }
    }
}
