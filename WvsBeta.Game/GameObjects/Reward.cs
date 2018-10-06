using System;
using System.Collections.Generic;
using WvsBeta.Common;
using WvsBeta.Common.Sessions;

namespace WvsBeta.Game
{
    public class Reward
    {
        public bool Mesos;
        public int Drop;
        private BaseItem Data;

        public long DateExpire
        {
            get
            {
                long Result = 0;
                if (Data != null) Result = Data.Expiration;
                return (Result == 0) ? BaseItem.NoItemExpiration : Result;
            }
        }

        public short Amount => Data?.Amount ?? -1;
        public int ItemID => (!Mesos) ? Drop : 0;
        public BaseItem GetData() => Data;

        // Server drop rate
        public static double ms_fIncDropRate => Server.Instance.RateDropChance;
        // Server 'event time' droprate (between 1pm and 6pm)
        public static double ms_fIncDropRate_WSE => 1.0;
        // Used for MC drops, map the MCType prop of the item to some table calculated in CField_MonsterCarnival::GetMCRewardRate
        public static double MonsterCarnivalRewardRate => 1.0;

        public static List<Reward> GetRewards(Character Owner, Map Field, int ID, char Type, bool PremiumMap, double Showdown)
        {
            double HourDropRateIncrease = 1.0;
            var curDate = MasterThread.CurrentDate;
            if (curDate.Hour >= 13 && curDate.Hour < 19)
            {
                HourDropRateIncrease = ms_fIncDropRate_WSE;
            }
            
            double dRegionalIncRate = Field.m_dIncRate_Drop;
            double dwOwnerDropRate = Owner.m_dIncDropRate;
            double dwOwnerDropRate_Ticket = Owner.m_dIncDropRate_Ticket;

            var Result = new List<Reward>();

            if (!DataProvider.Drops.TryGetValue($"{Type}{ID}", out var Rewards)) return Result;

            foreach (var Drop in Rewards)
            {
                if ((Drop.Premium && !PremiumMap))
                    continue;

                var itemDropRate = 1.0;
                if (Drop.Mesos == 0)
                    itemDropRate = dwOwnerDropRate_Ticket;

                var maxDropChance = (long)(1000000000.0
                                           / (ms_fIncDropRate * HourDropRateIncrease)
                                           / dRegionalIncRate
                                           / Showdown
                                           / dwOwnerDropRate
                                           / itemDropRate
                                           / MonsterCarnivalRewardRate);

                var luckyNumber = Rand32.Next() % maxDropChance;

                if (luckyNumber >= Drop.Chance) continue;

                // Don't care about items that are 'expired'
                if (Drop.Mesos != 0 && Drop.DateExpire <= curDate) continue;

                var Reward = new Reward()
                {
                    Mesos = Drop.Mesos != 0,
                    Drop = Drop.Mesos != 0 ? Drop.Mesos : Drop.ItemID,
                    Data = Drop.Mesos != 0 ? null : BaseItem.CreateFromItemID(Drop.ItemID, GetItemAmount(Drop.ItemID, Drop.Min, Drop.Max))
                };

                if (!Reward.Mesos)
                {
                    Reward.Data.GiveStats(ItemVariation.Normal);
                    if (Drop.Period > 0)
                    {
                        Reward.Data.Expiration = Tools.GetFileTimeWithAddition(new TimeSpan(Drop.Period, 0, 0, 0));
                    }
                    else if (Drop.DateExpire != DateTime.MaxValue)
                    {
                        Reward.Data.Expiration = Drop.DateExpire.ToFileTimeUtc();
                    }
                }

                if (!Drop.Premium || PremiumMap)
                {
                    if (Reward.Mesos)
                    {
                        int minDrop = 4 * Reward.Drop / 5;
                        int maxDrop = 2 * Reward.Drop / 5 + 1;
                        int DroppedMesos = (int)(minDrop + Rand32.Next() % maxDrop);

                        if (DroppedMesos <= 1)
                            DroppedMesos = 1;

                        DroppedMesos = (int)(DroppedMesos * dwOwnerDropRate_Ticket);
                        Reward.Drop = DroppedMesos;
                    }
                }

                Result.Add(Reward);
            }

            return Result;
        }

        public static Reward Create(BaseItem Item)
        {
            return new Reward()
            {
                Mesos = false,
                Data = Item,
                Drop = Item.ItemID
            };
        }

        public static Reward Create(double Mesos)
        {
            return new Reward()
            {
                Mesos = true,
                Drop = Convert.ToInt32(Mesos)
            };
        }

        private static short GetItemAmount(int ItemID, int Min, int Max)
        {
            var ItemType = ItemID / 1000000;
            if (Max > 0 && (ItemType == 2 || ItemType == 3 || ItemType == 4))
                return (short)(Min + Rand32.Next() % (Max - Min + 1));
            return 1;
        }

        public void EncodeForMigration(Packet pw)
        {
            pw.WriteBool(Mesos);
            pw.WriteInt(Drop);
            if (!Mesos)
            {
                Data.EncodeForMigration(pw);
            }
        }

        public static Reward DecodeForMigration(Packet pr)
        {
            var reward = new Reward();
            reward.Mesos = pr.ReadBool();
            reward.Drop = pr.ReadInt();
            if (!reward.Mesos)
            {
                reward.Data = BaseItem.DecodeForMigration(pr);
            }
            return reward;
        }
    }
}
