/**
 * Authors : Swaglord and wackyracer
 **/

using System;
using System.Collections.Generic;

namespace WvsBeta.Game.Events
{
    class KerningCity
    {
        public static int mExit = 103000890;
        public static int mBonus = 103000805;
        public static int Countdown = 1800;
        public static int CurrentTick { get; set; }

        public static void NewPQ(Character chr)
        {
            MasterThread.Instance.AddRepeatingAction(new MasterThread.RepeatingAction("PQWatcher", (date) =>  {PQWatcher(chr); }, 0, 1 * 1000));
            chr.UsingTimer = true;
        }
        
        public static void PQWatcher(Character chr)
        {
            DateTime date = new DateTime();
            Countdown = Countdown - 1;
            CurrentTick = Countdown;

            // BELOW NEEDS A REVAMP FROM OUTDATED DMS CODE - wackyracer

            /*if (chr.PartyID != null)
            {
                foreach (Character pMember in PQParty.Members)
                {
                    if (pMember.UsingTimer == false)
                    {
                        MapPacket.MapTimer(pMember, CurrentTick);
                        pMember.UsingTimer = true;
                    }
                }

                if (PQParty.Members.Count <= 2 && chr.MapID != mExit) // Can't continue with only 2 members
                {
                    foreach (Character member in PQParty.Members)
                    {
                        if (member.MapID != mExit)
                        {
                            member.UsingTimer = false;
                        }

                        member.ChangeMap(mExit);
                    }
                }

                if (!Server.Instance.CharacterList.ContainsKey(PQParty.Leader.ID)) // Leader can't be offline
                {
                    foreach (Character member in PQParty.Members)
                    {
                        if (member.MapID != mExit)
                        {
                            member.UsingTimer = false;
                        }

                        member.ChangeMap(mExit);
                    }
                }
            }

            if (chr.mParty == null && chr.MapID != mExit) // Checks all stages for characters without parties.
            {
                foreach (Character noParty in Server.Instance.CharacterList.Values)
                {
                    if (noParty.MapID >= 103000800 && noParty.MapID <= 103000805)
                    {
                        if (noParty.mParty == null)
                        {
                            noParty.ChangeMap(mExit);
                            StopPQ(date);
                            noParty.UsingTimer = false;
                        }
                    }
                }
            }

            if (CurrentTick == 0) // Times up!
            {
                StopPQ(date);

                foreach (Character eCharacter in Server.Instance.CharacterList.Values)
                {
                    if (eCharacter.MapID >= 103000800 && eCharacter.MapID <= 103000805)
                    {
                        eCharacter.ChangeMap(mExit);
                    }

                    eCharacter.UsingTimer = false;
                }
            }*/
        }

        public static void ResetPortals()
        {
            foreach (KeyValuePair<int, Map> kvp in DataProvider.Maps)
            {
                if (kvp.Key >= 103000800 && kvp.Key <= 103000805)
                {
                    kvp.Value.PQPortalOpen = false;
                }
            }
        }

        public static void OpenPortal(int MapID)
        {
            Map map = DataProvider.Maps[MapID];
            map.PQPortalOpen = true;
            MapPacket.PortalEffect(map, 2, "gate");
        }

        public static void ClosePortal(int MapID)
        {
            Map map = DataProvider.Maps[MapID];
            map.PQPortalOpen = false;
        }

        public static void StopPQ(DateTime Date)
        {
            MasterThread.Instance.RemoveRepeatingAction("PQWatcher", (date, name, removed) => { MasterThread.Instance.PerformanceLog.WriteLine("RemoveRepeatingAction Callback: Date: {0}; Name: {1}; Removed: {2}", date, name, removed); });
        }
    }
}