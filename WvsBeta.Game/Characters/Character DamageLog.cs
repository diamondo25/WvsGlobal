using System;
using System.Collections.Generic;
using log4net;

namespace WvsBeta.Game
{
    public partial class Character
    {
        private static ILog _damageLog = LogManager.GetLogger("DamageLog");

        public class DamageLogState
        {
            public byte level { get; set; }
            public int totalStr { get; set; }
            public int totalDex { get; set; }
            public int totalInt { get; set; }
            public int totalLuk { get; set; }
            public int totalPAD { get; set; }
            public int totalMAD { get; set; }
            //public int totalPDD { get; set; }
            //public int totalMDD { get; set; }
            public int totalACC { get; set; }
            public int totalEVA { get; set; }
            public int totalSpeed { get; set; }
            public int totalCraft { get; set; }

            public DamageLogState(CharacterPrimaryStats primaryStats)
            {
                level = primaryStats.Level;
                totalStr = primaryStats.TotalStr;
                totalDex = primaryStats.TotalDex;
                totalInt = primaryStats.TotalInt;
                totalLuk = primaryStats.TotalLuk;
                totalPAD = primaryStats.TotalPAD;
                totalMAD = primaryStats.TotalMAD;
                //totalPDD = primaryStats.TotalPDD;
                //totalMDD = primaryStats.TotalMDD;
                totalACC = primaryStats.TotalACC;
                totalEVA = primaryStats.TotalEVA;
                totalCraft = primaryStats.TotalCraft;
                totalSpeed = primaryStats.TotalSpeed;
            }

            public bool IsSame(DamageLogState other)
            {
                return (
                    other.level == level &&
                    other.totalStr == totalStr &&
                    other.totalDex == totalDex &&
                    other.totalInt == totalInt &&
                    other.totalLuk == totalLuk &&
                    other.totalPAD == totalPAD &&
                    other.totalMAD == totalMAD &&
                    //other.totalPDD == totalPDD &&
                    //other.totalMDD == totalMDD &&
                    other.totalACC == totalACC &&
                    other.totalEVA == totalEVA &&
                    other.totalCraft == totalCraft &&
                    other.totalSpeed == totalSpeed
                );
            }
        }

        private DamageLogState _damageLogState = null;
        private readonly Dictionary<(int skillId, byte skillLevel, int mobId), (int minDamage, int maxDamage, int records)> _damageLogs = new Dictionary<(int skillId, byte skillLevel, int mobId), (int minDamage, int maxDamage, int records)>();

        public void UpdateDamageLog(int skillId, byte skillLevel, int mobId, int minDamage, int maxDamage)
        {
            var key = (skillId, skillLevel, mobId);
            var records = 1;
            var (curMin, curMax) = (minDamage, maxDamage);

            if (_damageLogs.ContainsKey(key))
            {
                (curMin, curMax, records) = _damageLogs[key];
                curMin = Math.Min(curMin, minDamage);
                curMax = Math.Max(curMax, maxDamage);
                records++;
            }

            _damageLogs[key] = (curMin, curMax, records);
        }

        private void InitDamageLog()
        {
            _damageLogState = new DamageLogState(PrimaryStats);
        }

        public void FlushDamageLog(bool force = false)
        {
            var actualState = new DamageLogState(PrimaryStats);
            if (_damageLogState.IsSame(actualState) && !force) return;

            RunFlushDamageLog();

            // Update the state
            _damageLogState = actualState;
        }

        private void RunFlushDamageLog()
        {

            foreach (var kvp in _damageLogs)
            {
                _damageLog.Info(new MaxDamageLogRecord
                {
                    level = _damageLogState.level,
                    skillId = kvp.Key.skillId,
                    skillLevel = kvp.Key.skillLevel,
                    mobId = kvp.Key.mobId,
                    totalStr = _damageLogState.totalStr,
                    totalDex = _damageLogState.totalDex,
                    totalInt = _damageLogState.totalInt,
                    totalLuk = _damageLogState.totalLuk,
                    totalPAD = _damageLogState.totalPAD,
                    totalMAD = _damageLogState.totalMAD,
                    totalCraft = _damageLogState.totalCraft,
                    totalSpeed = _damageLogState.totalSpeed,
                    totalAcc = _damageLogState.totalACC,
                    totalEva = _damageLogState.totalEVA,
                    maxDamage = kvp.Value.maxDamage,
                    minDamage = kvp.Value.minDamage,
                });
            }

            _damageLogs.Clear();
        }
    }
}
