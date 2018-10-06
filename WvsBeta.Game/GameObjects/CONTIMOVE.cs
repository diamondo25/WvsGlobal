using System;
using System.Linq;
using WvsBeta.Common;

namespace WvsBeta.Game
{
    public class CONTIMOVE
    {
        // Used for ms to seconds multiplier, reduce to speed up the cycle
        public const int TimeMultiplier = 1000;

        public int FieldIdStartShipMove { get; set; }
        public int FieldIdWait { get; set; }
        public int FieldIdMove { get; set; }
        public int FieldIdCabin { get; set; }
        public int FieldIdEnd { get; set; }
        public int FieldIdEndShipMove { get; set; }
        public Conti State { get; set; }
        public int WaitMin { get; set; }
        public int RequiredMin { get; set; }
        public bool Event { get; private set; }
        public bool EventDoing { get; private set; }
        public int GetMobItemID { get; set; }
        public long MobGenTime { get; private set; }
        public int EventEndMin { get; set; }
        public Pos MobSpawnPoint { get; set; }
        public string ReactorName { get; set; }
        public int StateOnStart { get; set; }
        public int StateOnEnd { get; set; }
        public long NextBoardingTime { get; set; }
        public int TermTime { get; set; }

        // Other fields inside the Contimove object

        public int DelayTime;

        public void ResetEvent()
        {
            EventDoing = false;
            Event = GetMobItemID != 0 && (Rand32.Next() % 100) <= 30;

            int randomMinutes = (int)(Rand32.Next() % (RequiredMin - 5));
            
            MobGenTime = NextBoardingTime + ((60 * TimeMultiplier) * (WaitMin + randomMinutes + 2));
            var curTime = MasterThread.CurrentTime;
            var timeTillSpawn = MobGenTime - curTime;
            var timeTillBoard = NextBoardingTime - curTime;
            
            if (Event)
            {
                var txt = $"Will spawn crogs on trip {FieldIdStartShipMove} -> {FieldIdEndShipMove} (map {FieldIdMove}) in {(timeTillSpawn / 1000):D} seconds";
                MessagePacket.SendNoticeGMs(txt, MessagePacket.MessageTypes.Notice);
            }
        }

        public Conti GetState()
        {
            long st = MasterThread.CurrentTime;
            long diff;
            switch (State)
            {
                case Conti.Dormant:
                    if (compare_system_time(st, NextBoardingTime) > 0)
                    {
                        State = Conti.Wait;
                        return Conti.Wait;
                    }
                    break;

                case Conti.Wait:
                    diff = GetSystemTimeDiffer(st, -WaitMin);
                    if (compare_system_time(diff, NextBoardingTime) > 0)
                    {
                        State = Conti.Move;
                        // not an error
                        return Conti.Start;
                    }
                    break;

                case Conti.Move:
                    if (Event)
                    {
                        // Start event
                        if (!EventDoing && compare_system_time(st, MobGenTime) > 0)
                        {
                            diff = GetSystemTimeDiffer(st, -(WaitMin + EventEndMin));
                            if (compare_system_time(diff, NextBoardingTime) < 0)
                            {
                                // Summon mobs
                                EventDoing = true;
                                return Conti.Mobgen;
                            }
                        }

                        // Stop event
                        else if (EventDoing)
                        {
                            diff = GetSystemTimeDiffer(st, -(WaitMin + EventEndMin));
                            if (compare_system_time(diff, NextBoardingTime) > 0)
                            {
                                // Remove mobs
                                EventDoing = false;
                                return Conti.Mobdestroy;
                            }
                        }
                        
                    }

                    // Check if we have to end the boat trip
                    diff = GetSystemTimeDiffer(st, -(WaitMin + RequiredMin));
                    if (compare_system_time(diff, NextBoardingTime) > 0)
                    {
                        NextBoardingTime = GetSystemTimeDiffer(NextBoardingTime, TermTime);
                        ResetEvent();
                        State = Conti.Dormant;
                        return Conti.End;
                    }
                    break;
            }

            return State;
        }

        public static long GetSystemTimeDiffer(long input, int st)
        {
            // st is minutes??
            // TODO: back to 1000

            return input + (st * 60 * TimeMultiplier);
        }

        /// <summary>
        /// Compare two SystemTime... no wait, just longs
        /// </summary>
        /// <param name="st1">Current time</param>
        /// <param name="st2">The other time</param>
        /// <returns>1 when st1 >= st2, otherwise -1</returns>
        public static int compare_system_time(long st1, long st2)
        {
            // In GMS this does a SystemTime check on Year, month, day, hour, minute, second, millisecond
            // Pretty lame, as we can just use the time...
            // Might break on rollover. But really, a rollover on a 64-bit value?
            
            return st1 >= st2 ? 1 : -1;
        }

        public void SummonMob()
        {
            if (FieldIdMove == Constants.InvalidMap) return;

            if (!DataProvider.Maps.TryGetValue(FieldIdMove, out var field)) return;

            if (field.Limitations.HasFlag(FieldLimit.SummonLimit)) return;

            if (!DataProvider.Items.TryGetValue(GetMobItemID, out var itemData)) return;

            Program.MainForm.LogAppend("Spawning mobs for contimove trip on map " + field.ID);

            var fh = field.GetFootholdUnderneath(MobSpawnPoint.X, MobSpawnPoint.Y, out var maxY);

            foreach (var itemDataSummon in itemData.Summons)
            {
                if (Rand32.Next() % 100 >= itemDataSummon.Chance) continue;
                field.SpawnMobWithoutRespawning(itemDataSummon.MobID, MobSpawnPoint, (byte)(fh.HasValue ? fh.Value.ID : 0));
            }
        }

        public void DestroyMob()
        {
            if (FieldIdMove == Constants.InvalidMap) return;

            if (!DataProvider.Maps.TryGetValue(FieldIdMove, out var field)) return;

            Program.MainForm.LogAppend("Removing mobs for contimove trip on map " + field.ID);
            foreach (var mob in field.Mobs.Values.ToArray())
            {
                mob.ForceDead();
            }
        }
    }
}
