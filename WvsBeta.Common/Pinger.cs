using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using log4net;
using WvsBeta.Common.Sessions;

namespace WvsBeta.Common
{
    public class Pinger
    {
        public static int CurrentLoggingConnections => _connections.Count;

        private static ILog _log = LogManager.GetLogger("Pinger");
        private static readonly List<AbstractConnection> _connections = new List<AbstractConnection>();
        private const int PingCheckTimeSeconds = 15;
        private const int PingCheckTime = PingCheckTimeSeconds * 1000;
        private static long _lastPingTime = 0;

        private static readonly object lockobj = 1;

        public static void Add(AbstractConnection conn)
        {
            _log.Debug("Adding connection " + conn.IP + ":" + conn.Port);
            lock (lockobj)
            {
                _connections.Add(conn);
            }
        }

        public static void Remove(AbstractConnection conn)
        {
            _log.Debug("Removing connection " + conn.IP + ":" + conn.Port);
            lock (lockobj)
            {
                _connections.Remove(conn);
            }
        }

        public static void Init(Action<string> pingcallback = null, Action<string> dcCallback = null)
        {
            MasterThread.Instance.AddRepeatingAction(new MasterThread.RepeatingAction(
                "Pinger",
                time =>
                {
                    if (_lastPingTime != 0 &&
                        (time - _lastPingTime) < PingCheckTime)
                    {
                        _log.Debug($"Ignoring ping (too much!): {(time - _lastPingTime)}");
                        return;
                    }
                    _lastPingTime = time;
                    AbstractConnection[] d;

                    lock (lockobj)
                    {
                        d = _connections.ToArray();
                    }

                    foreach (var session in d)
                    {
                        if (session.gotPong || !session.sentSecondPing)
                        {
                            session.gotPong = false;
                            if (session.sentPing)
                            {
                                session.sentSecondPing = true;
                            }

                            session.sentPing = true;
                            if (session.pings > 0)
                            {
                                session.pings = 0;
                                //dcCallback?.Invoke("Got pong - resetting number of ping retries because connection re-established");
                            }
                            session.SendPing();
                        }
                        else if ((time - session.pingSentDateTime) > PingCheckTime)
                        {
                            session.pings++;

                            //dcCallback?.Invoke("Pinger Disconnected! Retry " + session.pings + ". " + session.IP + ":" + session.Port + " " + MasterThread.CurrentDate);

                            if (session.pings > 8)
                            {
                                dcCallback?.Invoke("Pinger Disconnected! Too many retries, killing connection. " + session.IP + ":" + session.Port + " " + MasterThread.CurrentDate);


                                if (session.Disconnect())
                                {
                                    // Killed
                                    dcCallback?.Invoke("Session is now disconnected. " + session.IP + ":" +
                                                       session.Port + " " + MasterThread.CurrentDate);
                                }
                                else
                                {
                                    dcCallback?.Invoke("Connection was already dead?! Getting rid of it. " +
                                                       session.IP + ":" + session.Port + " " +
                                                       MasterThread.CurrentDate);

                                    Remove(session);
                                }
                            }
                            else
                            {
                                // Make the session kill faster
                                session.SendPing();
                            }
                        }
                    }
                }, PingCheckTime, PingCheckTime));
        }
    }
}
