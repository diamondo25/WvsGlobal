using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace WvsBeta
{
    public partial class MasterThread
    {
        public class RepeatingAction
        {
            private static ILog _log = LogManager.GetLogger("RepeatingAction");
            
            public string Name { get; private set; }
            public long Repeat { get; private set; }
            public Action<long> Action { get; private set; }
            public long LastRun { get; private set; }
            public long NextRun { get; private set; }
            public bool ScheduledForRemoval { get; set; } = false;
            private readonly Func<bool> EndCondition;
            private static Stopwatch _functionStopwatch = new Stopwatch();

            public static int RunningTasks { get; private set; } = 0;

            /// <summary>
            /// Create a new Repeating Action
            /// </summary>
            /// <param name="pName">Unique identifier for this action</param>
            /// <param name="pAction">Logic that is ran</param>
            /// <param name="start">Amount of milliseconds delay for first run</param>
            /// <param name="repeat">Amount of milliseconds between each run. Use 0 to have a one-time action.</param>
            /// <param name="pEndCondition">When this returns true, the repeating action will be deregistered.</param>
            public RepeatingAction(string pName, Action<long> pAction, long start, long repeat, Func<bool> pEndCondition = null)
            {
                Name = pName;
                Action = pAction;
                EndCondition = pEndCondition;
                ChangeRuntimes(start, repeat);
            }

            /// <summary>
            /// Create a new Repeating Action
            /// </summary>
            /// <param name="pName">Unique identifier for this action</param>
            /// <param name="pAction">Logic that is ran</param>
            /// <param name="start">Amount of milliseconds delay for first run</param>
            /// <param name="repeat">Amount of milliseconds between each run. Use 0 to have a one-time action.</param>
            /// <param name="pEndCondition">When this returns true, the repeating action will be deregistered.</param>
            public static RepeatingAction Start(string pName, Action<long> pAction, long start, long repeat,
                Func<bool> pEndCondition = null)
            {
                var x = new RepeatingAction(pName, pAction, start, repeat, pEndCondition);
                x.Start();
                return x;
            }


            /// <summary>
            /// Create a new Repeating Action
            /// </summary>
            /// <param name="pName">Unique identifier for this action</param>
            /// <param name="pAction">Logic that is ran</param>
            /// <param name="start">Amount of milliseconds delay for first run</param>
            /// <param name="repeat">Amount of milliseconds between each run. Use 0 to have a one-time action.</param>
            /// <param name="pEndCondition">When this returns true, the repeating action will be deregistered.</param>
            public static RepeatingAction Start(string pName, Action pAction, long start, long repeat,
                Func<bool> pEndCondition = null)
            {
                return Start(pName, x => pAction(), start, repeat, pEndCondition);
            }

            /// <summary>
            /// Create a new Repeating Action
            /// </summary>
            /// <param name="pName">Unique identifier for this action</param>
            /// <param name="pAction">Logic that is ran</param>
            /// <param name="start">Amount of milliseconds delay for first run</param>
            /// <param name="repeat">Amount of milliseconds between each run. Use 0 to have a one-time action.</param>
            /// <param name="pEndCondition">When this returns true, the repeating action will be deregistered.</param>
            public static RepeatingAction Start<T>(string pName, Func<T> pAction, long start, long repeat,
                Func<bool> pEndCondition = null)
            {
                return Start(pName, x => pAction(), start, repeat, pEndCondition);
            }


            public void ChangeRuntimes(long start, long repeat)
            {
                Repeat = repeat;
                CalculateNextRun(start);
                
            }

            public bool Stop()
            {
                if (_task == null) return false;

                ScheduledForRemoval = true;
                return true;
            }

            public bool Start()
            {
                if (_task != null) return false;
                StartTask();
                return true;
            }

            private void CalculateNextRun(long offset, long? dt = null)
            {
                NextRun = (dt ?? MasterThread.CurrentTime) + offset;
            }
            
            public bool CanRun()
            {
                if (ScheduledForRemoval ||
                    (EndCondition != null && EndCondition()))
                {
                    ScheduledForRemoval = true;
                    return false;
                }

                if (CurrentTime < NextRun) return false;
                return true;
            }

            private bool _isRunning = false;
            private Task _task;
            private void StartTask()
            {
                _task = Task.Factory.StartNew(() =>
                {
                    RunningTasks++;
                    _log.Info("Started task for " + this);
                    while (true)
                    {
                        if (!_isRunning && CanRun())
                        {
                            _isRunning = true;
                            TryRun();
                        }
                        if (ScheduledForRemoval) break;
                        Thread.Sleep(50);
                    }

                    _log.Info("Stopping task for " + this);

                    RunningTasks--;
                    ScheduledForRemoval = false;
                    _task = null;
                    Trace.WriteLine("Terminated task " + this + ", " + CurrentDate);
                }, TaskCreationOptions.LongRunning);
            }
            
            private void TryRun()
            {
                var startTime = CurrentTime;

                MasterThread.Instance.AddCallback((date) =>
                {
                    var delay = (CurrentTime - startTime) / 1000;
                    if (delay > 2)
                    {
                        _log.Warn($"RepeatingAction is getting behind! {Name} was scheduled for {startTime} but ran {CurrentTime} (+{delay} seconds)!!!");
                    }

                    _functionStopwatch.Restart();
                    Action(date);
                    _functionStopwatch.Stop();

                    if (_functionStopwatch.Elapsed.TotalMilliseconds > 500)
                    {
                        _log.Warn($"RepeatingAction is slow! {Name} ran for {_functionStopwatch.Elapsed}!!!");
                    }


                    LastRun = CurrentTime;

                    if (Repeat == 0) ScheduledForRemoval = true;
                    else CalculateNextRun(Repeat, CurrentTime);

                    _isRunning = false;
                }, "RA: " + Name);
            }

            public override string ToString()
            {
                return string.Format("RepeatingAction. Name: {2}, Seconds for each run: {0}, Next run: {1}", Repeat, NextRun, Name);
            }
        }
        
    }
}