using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using log4net.Config;
using WvsBeta.Common;


namespace WvsBeta.Shop
{
    class Program
    {
        public static frmMain MainForm { get; set; }

        public static string IMGFilename { get; set; }
        public static Common.Logfile LogFile { get; private set; }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                MessageBox.Show("Invalid argument length.");
                return;
            }

            log4net.GlobalContext.Properties["ImgName"] = args[0];
            XmlConfigurator.ConfigureAndWatch(new FileInfo(Path.Combine(Environment.CurrentDirectory, "..", "DataSvr", "logging-config-shop.xml")));

            IMGFilename = args[0];
            LogFile = new Common.Logfile(IMGFilename);


            UnhandledExceptionHandler.Set(args, IMGFilename, LogFile);
            MasterThread.Load(IMGFilename);
            Pinger.Init();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(MainForm = new frmMain());
        }
    }
}
