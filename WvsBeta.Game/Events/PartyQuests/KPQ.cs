using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using log4net;
using WvsBeta.Common;
using WvsBeta.Game.Events.PartyQuests;
using static WvsBeta.MasterThread;

namespace WvsBeta.Game.Events
{
    public class KPQ
    {
        private static ILog _log = LogManager.GetLogger("KPQ");


        private static readonly Map KerningCity = DataProvider.Maps[103000000];
        private static readonly Map ExitMap = DataProvider.Maps[103000890];
        public static readonly IEnumerable<Map> Maps = (new List<int> { 103000800, 103000801, 103000802, 103000803, 103000804, 103000805 }).Select(m => DataProvider.Maps[m]);
        private static readonly int PQTime = 30 * 60 * 1000;
        private static readonly int KingSlime = 9300003;
        private static readonly Map KingSlimeMap = DataProvider.Maps[103000804];
        private static readonly List<MobGenItem> Stage5MobGen;

        private static RepeatingAction KPQWatcher() => new RepeatingAction("KPQWatcher - " + Server.Instance.ID, time =>
        {
            _instance?.WithCheck(() => KPQStageResult.CHECK);
        }, 1000, 1000);

        private static readonly Action<Character, Map> PQTimer = (chr, map) =>
        {
            if (_instance != null)
                MapPacket.ShowMapTimerForCharacter(chr, (int)(_instance.GetTimeRemaining() / 1000));
        };

        private static readonly List<Tuple<string, int>> StageOneQuestions = new List<Tuple<string, int>>()
        {
            Tuple.Create("Here's the question. Collect the same number of coupons as the minimum amount of STR needed to make the first job advancement as a warrior.", 35),
            Tuple.Create("Here's the question. Collect the same number of coupons as the number of levels needed to make the first job advancement as the warrior.", 10),
            Tuple.Create("Here's the question. Collect the same number of coupons as the minimum amount of INT needed to make the first job advancement as the magician.", 20),
            Tuple.Create("Here's the question. Collect the same number of coupons as the number of levels needed to make the first job advancement as the magician.", 8),
            Tuple.Create("Here's the question. Collect the same number of coupons as the number of levels needed to make the first job advancement as the thief.", 10),
            Tuple.Create("Here's the question. Collect the same number of coupons as the number of levels needed to make the first job advancement as the bowman.", 10),
            Tuple.Create("Here's the question. Collect the same number of coupons as the minimum amount of DEX needed to make the first job advancement as the thief.", 25),
            Tuple.Create("Here's the question. Collect the same number of coupons as the minimum amount of DEX needed to make the first job advancement as the bowman.", 25)
        };

        private static KPQ _instance = null;

        public static KPQ GetInstance() => _instance;

        static KPQ()
        {
            Maps.ForEach(m => m.OnEnter = PQTimer);
            Stage5MobGen = KingSlimeMap.MobGen.ToList();
            KingSlimeMap.MobGen.Clear();
        }

        public enum KPQStartResult
        {
            NOT_IN_PARTY,
            MUST_BE_LEADER,
            PARTY_TOO_SMALL,
            PARTY_INSIDE,
            NOT_ALL_MEMBERS_PRESENT,
            SUCCESS,
        }
        
        private static IEnumerable<int> GetAvailablePartyMembers(PartyData pt)
        {
            return pt.Members.Where(x => x != 0);
        }

        private static IEnumerable<Character> LoadCharactersFromParty(PartyData pt)
        {
            return GetAvailablePartyMembers(pt)
                .Where(m => Server.Instance.CharacterList.ContainsKey(m))
                .Select(m => Server.Instance.CharacterList[m]);
        }

        private static IEnumerable<Character> FilterOutNonKPQPeople(IEnumerable<Character> characters)
        {
            return characters.Where(x => Maps.Contains(x.Field));
        }

        public static KPQStartResult TryStart(Character chr, PartyData pt)
        {
            var leader = pt.Leader;
            var memberList = GetAvailablePartyMembers(pt).ToList();


            if (chr.PartyID == 0)
            {
                return KPQStartResult.NOT_IN_PARTY;
            }
            else if (chr.ID != leader)
            {
                return KPQStartResult.MUST_BE_LEADER;
            }
            else if (memberList.Count != 4)
            {
                return KPQStartResult.PARTY_TOO_SMALL;
            }
            else if (_instance != null)
            {
                return KPQStartResult.PARTY_INSIDE;
            }
            else
            {
                //check if all in same channel
                if (memberList.Any(x => Server.Instance.CharacterList.ContainsKey(x) == false))
                {
                    return KPQStartResult.NOT_ALL_MEMBERS_PRESENT;
                }
                var characters = LoadCharactersFromParty(pt).ToList();

                if (!characters.TrueForAll(e => e.MapID == KerningCity.ID)) //check if all are in Kerning City
                {
                    return KPQStartResult.NOT_ALL_MEMBERS_PRESENT;
                }

                _instance = new KPQ(chr, characters);
                characters.ForEach(m => m.ChangeMap(103000800, "st00")); //this is not in constructor to make timer appear properly
                return KPQStartResult.SUCCESS;
            }
        }

        /****** INSTANCE *******/
        private long startTime;
        public int PartyId { get; private set; }
        private Character _leader;
        private IEnumerable<Character> _party;
        private List<bool> ropes;
        private List<bool> kittens;
        private List<bool> barrels;
        private Dictionary<int, Tuple<string, int>> questions;
        private bool _bonusTimerSet;
        private RepeatingAction _watcher;

        public enum KPQStageResult
        {
            NOT_LEADER,
            NEED_MORE_PASSES,
            NEED_MORE_COUPONS,
            ENOUGH_PASSES,
            ENOUGH_COUPONS,
            COMBO_SUCCESS,
            COMBO_FAIL,
            CANNOT_CONTINUE,
            NOT_ENOUGH_FOR_COMBO,
            CHECK //Used for additional checks and such, nothing to do with NPC
        }

        private KPQ(Character pLeader, IEnumerable<Character> pParty)
        {
            startTime = MasterThread.CurrentTime;
            PartyId = pLeader.PartyID;
            _leader = pLeader;
            _party = pParty;

            ropes = new List<bool>(4) { true, true, true, false };
            kittens = new List<bool>(5) { true, true, true, false, false };
            barrels = new List<bool>(6) { true, true, true, false, false, false };
            questions = new Dictionary<int, Tuple<string, int>>();
            _bonusTimerSet = false;

            ropes.Shuffle();
            kittens.Shuffle();
            barrels.Shuffle();
            _party.ForEach(m => questions.Add(m.ID, StageOneQuestions.RandomElement()));

            Maps.ForEach(m => m.PQPortalOpen = false);
            Maps.ForEach(m => m.DropPool.Clear());

            KingSlimeMap.Mobs.Clear();
            Stage5MobGen.ForEach(mgi => KingSlimeMap.SpawnMob(mgi.ID, mgi, new Pos(mgi.X, mgi.Y), mgi.Foothold));

            _watcher = KPQWatcher();
            _watcher.Start();

            _log.Info($"Started KPQ with party ({string.Join(", ", pParty.Select(x => x.Name))})");
        }
        
        public void UpdateParty(PartyData pd)
        {
            // Not for us
            if (PartyId != pd.PartyID) return;
            
            _party = FilterOutNonKPQPeople(LoadCharactersFromParty(pd));
            WithCheck(() => KPQStageResult.CHECK);
        }

        public long GetTimeRemaining() => Math.Max(PQTime - (MasterThread.CurrentTime - startTime), 0);

        public string GetStage1Question(Character chr)
        {
            if (questions.TryGetValue(chr.ID, out Tuple<string, int> questionPair))
                return questionPair.Item1;
            return "Unknown error occurred for character " + chr.Name + ". Please show this to a GM";
        }

        public int GetStage1Coupons(Character chr)
        {
            if (questions.TryGetValue(chr.ID, out Tuple<string, int> questionPair))
                return questionPair.Item2;
            return 0;
        }

        /**
         * STAGE 1 COUPON CHECK
         * 
         * RETURNS:
         *      NEED_MORE_COUPONS
         *      ENOUGH_COUPONS
         *      CANNOT_CONTINUE
        **/
        public KPQStageResult CheckStage1Coupons(Character chr) => WithCheck(() =>
        {
            return
                chr.Inventory.GetItemAmount(4001007) != questions[chr.ID].Item2 ?
                KPQStageResult.NEED_MORE_COUPONS : KPQStageResult.ENOUGH_COUPONS;
        });

        /**
         * STAGE 1
         * 
         * RETURNS:
         *      NOT_LEADER
         *      NEED_MORE_PASSES
         *      ENOUGH_PASSES
         *      CANNOT_CONTINUE
         **/
        public KPQStageResult CheckStage1(Character chr) => WithCheck(() =>
        {
            int passes = _party.Count() - 1;

            if (_leader.ID != chr.ID)
                return KPQStageResult.NOT_LEADER;
            else if (chr.Inventory.GetItemAmount(4001008) < passes)
                return KPQStageResult.NEED_MORE_PASSES;
            else
                return KPQStageResult.ENOUGH_PASSES;
        });

        /**
        * STAGE 2
        * 
        * RETURNS:
        *      NOT_LEADER
        *      COMBO_SUCCESS
        *      COMBO_FAIL
        *      CANNOT_CONTINUE
        *      NOT_ENOUGH_FOR_COMBO
        **/
        public KPQStageResult CheckStage2(Character chr) => WithCheck(() =>
        {
            KPQStageResult check2()
            {
                bool one = _party.Exists(m => m.Foothold == -4);
                bool two = _party.Exists(m => m.Foothold == -5);
                bool three = _party.Exists(m => m.Foothold == -6);
                bool four = _party.Exists(m => m.Foothold == -7);

                var playersOnRopes = new List<bool>() { one, two, three, four };
                if (playersOnRopes.SequenceEqual(ropes))
                    return KPQStageResult.COMBO_SUCCESS;
                else if (playersOnRopes.Count(p => p) == 3)
                    return KPQStageResult.COMBO_FAIL;
                else
                    return KPQStageResult.NOT_ENOUGH_FOR_COMBO;
            }

            if (_leader.ID != chr.ID)
                return KPQStageResult.NOT_LEADER;
            else
                return check2();
        });


        /**
        * STAGE 3
        * 
        * RETURNS:
        *      NOT_LEADER
        *      COMBO_SUCCESS
        *      COMBO_FAIL
        *      CANNOT_CONTINUE
        *      NOT_ENOUGH_FOR_COMBO
        **/
        public KPQStageResult CheckStage3(Character chr) => WithCheck(() =>
        {
            KPQStageResult check3()
            {
                bool one = _party.Exists(m => m.Foothold >= 25 && m.Foothold <= 32);
                bool two = _party.Exists(m => m.Foothold >= 34 && m.Foothold <= 38);
                bool three = _party.Exists(m => m.Foothold >= 8 && m.Foothold <= 12);
                bool four = _party.Exists(m => m.Foothold >= 2 && m.Foothold <= 6);
                bool five = _party.Exists(m => m.Foothold >= 14 && m.Foothold <= 20);

                var playersOnKittens = new List<bool>() { one, two, three, four, five };
                if (playersOnKittens.SequenceEqual(kittens))
                    return KPQStageResult.COMBO_SUCCESS;
                else if (playersOnKittens.Count(p => p) == 3)
                    return KPQStageResult.COMBO_FAIL;
                else
                    return KPQStageResult.NOT_ENOUGH_FOR_COMBO;
            }

            if (_leader.ID != chr.ID)
                return KPQStageResult.NOT_LEADER;
            else
                return check3();
        });

        /**
        * STAGE 4
        * 
        * RETURNS:
        *      NOT_LEADER
        *      COMBO_SUCCESS
        *      COMBO_FAIL
        *      CANNOT_CONTINUE
        *      NOT_ENOUGH_FOR_COMBO
        **/
        public KPQStageResult CheckStage4(Character chr) => WithCheck(() =>
        {
            KPQStageResult check4()
            {
                bool one = _party.Exists(m => m.Foothold == 117);
                bool two = _party.Exists(m => m.Foothold == 115);
                bool three = _party.Exists(m => m.Foothold == 113);
                bool four = _party.Exists(m => m.Foothold == 116);
                bool five = _party.Exists(m => m.Foothold == 114);
                bool six = _party.Exists(m => m.Foothold == 112);

                var playersOnBarrels = new List<bool>() { one, two, three, four, five, six };
                if (playersOnBarrels.SequenceEqual(barrels))
                    return KPQStageResult.COMBO_SUCCESS;
                else if (playersOnBarrels.Count(p => p) == 3)
                    return KPQStageResult.COMBO_FAIL;
                else
                    return KPQStageResult.NOT_ENOUGH_FOR_COMBO;
            }

            if (_leader.ID != chr.ID)
                return KPQStageResult.NOT_LEADER;
            else
                return check4();
        });

        public void SetBonusTimer() => WithCheck(() =>
        {
            if (!_bonusTimerSet)
            {
                _log.Info("Setting bonus timer.");
                startTime = MasterThread.CurrentTime - PQTime + (10 * 60 * 1000); //A trick way to set the timer to 10 minutes: Just pretend n-10 minutes have passed for a n minute pq.
                _bonusTimerSet = true;
            }

            return KPQStageResult.CHECK;
        });

        private KPQStageResult WithCheck(Func<KPQStageResult> pEvent)
        {
            try
            {
                _party = _party
                    .Where(m => Server.Instance.GetCharacter(m.ID) != null)
                    .Where(m => m.PartyID == PartyId)
                    .Where(m => Maps.Exists(map => map.ID == m.MapID))
                    .ToList();


                if (_leader == null ||
                    !_party.Contains(_leader) ||
                    _party.Count() < 3 ||
                    GetTimeRemaining() <= 1)
                {
                    End();
                    return KPQStageResult.CANNOT_CONTINUE;
                }
                else
                {
                    Boot();
                    return pEvent();
                }
            }
            catch (Exception ex)
            {
                Program.MainForm.LogAppend("KPQWatcher threw an exception! " + ex);
                return KPQStageResult.CANNOT_CONTINUE;
            }
        }

        private void Boot()
        {
            var leaving = from map in Maps
                          from chr in map.Characters
                          where chr != null
                          where !chr.IsGM
                          where !_party.Contains(chr)
                          select chr;
            List<Character> boot = leaving.ToList(); //realize the whole list so we can modify map character lists with no issue

            if (boot.Count == 0) return;

            _log.Info($"Booting players: {string.Join(", ", boot.Select(x => x.Name))}");

            boot.ForEach(c => c.ChangeMap(ExitMap.ID));
            boot.ForEach(c => c.Inventory.TakeItem(4001008, (short)c.Inventory.ItemAmountAvailable(4001008)));
        }

        private void End()
        {
            _log.Info("Ending KPQ");

            _instance = null;
            MasterThread.Instance.RemoveRepeatingAction(_watcher);
            _party = Enumerable.Empty<Character>();
            Boot();
        }

        public void OpenPortal(int mapId)
        {
            OpenPortal(DataProvider.Maps[mapId]);
        }

        public void OpenPortal(Map map) => WithCheck(() =>
        {
            map.PQPortalOpen = true;
            MapPacket.PortalEffect(map, 2, "gate");
            RepeatingAction.Start("KPQ-UNSTUCKER", time => _party.ForEach(InventoryPacket.NoChange), 6000, 0);
            return KPQStageResult.CHECK;
        });

        public void GiveExp(int amount) => WithCheck(() =>
        {
            _log.Info($"GiveExp({amount})");
            _party.ForEach(m =>
            {
                m.SetupLogging();
                m.AddEXP(amount, true);
            });
            return KPQStageResult.CHECK;
        });

        public void SendStageResult(bool success) => WithCheck(() => //Sends WRONG or CLEAR animation to map
        {
            if (success)
            {
                MapPacket.MapEffect(_leader, 4, "Party1/Clear", false);
                MapPacket.MapEffect(_leader, 3, "quest/party/clear", false);
            }
            else
            {
                MapPacket.MapEffect(_leader, 4, "Party1/Failed", false);
                MapPacket.MapEffect(_leader, 3, "quest/party/wrong_kor", false);
            }
            //TODO not sure if this packet is correct, might be left over from v40b? Test.
            return KPQStageResult.CHECK;
        });

        public bool IsLeader(int charId)
        {
            WithCheck(() => KPQStageResult.CHECK);
            return charId == _leader.ID;
        }
    }
}