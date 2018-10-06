using System;

namespace WvsBeta.Common
{
    public static class UnhandledExceptionHandler
    {
        public static void Set(string[] args, string serverName, Logfile logFile)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var exceptionText = e.ExceptionObject.ToString();
                var currentTime = DateTime.Now.ToFileTime();

                if (e.IsTerminating)
                {
                    exceptionText += Environment.NewLine + "--- Terminating ---";
                }

                // Unhandled exception!
                if (logFile != null)
                {
                    try
                    {
                        // Try to write to logfile
                        logFile.WriteLine("UNHANDLED EXCEPTION!");
                        logFile.WriteLine(exceptionText);
                    }
                    catch { }
                }

                try
                {
                    // Write to exception log
                    System.IO.File.WriteAllText(System.IO.Path.Combine(Environment.CurrentDirectory, "UnhandledException-" + serverName + "-" + currentTime + ".log"), exceptionText);
                }
                catch { }

                try
                {
                    // Write to eventlog
                    var eventLogName = "wvsbeta." + serverName;
                    System.Diagnostics.EventLog.WriteEntry(eventLogName, exceptionText, System.Diagnostics.EventLogEntryType.Error);
                }
                catch { }
                
            };

        }
    }
}
