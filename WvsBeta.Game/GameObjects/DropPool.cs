using System.Collections.Generic;
using WvsBeta.Common;
using WvsBeta.Common.Sessions;

namespace WvsBeta.Game
{
    public class DropPool
    {
        private const bool _MergeDrops = false;
        public Map Field { get; set; }
        private LoopingID _DropIdCounter { get; set; }
        public const long DropExpireTime = 3 * 60 * 1000;
        public bool DropEverlasting { get; set; }
        public Dictionary<int, Drop> Drops { get; private set; } = new Dictionary<int, Drop>();

        public DropPool(Map field)
        {
            Field = field;
            _DropIdCounter = new LoopingID();
        }

        public bool Create(Reward Reward, int OwnerID, int OwnPartyID, DropType OwnType, int SourceID, Pos CurPos, int x2, short Delay, bool Admin, short Pos, bool ByPet, bool ByUser)
        {
            var Foothold = Field.GetFootholdUnderneath(x2, CurPos.Y - 100, out int y2);

            if (Foothold == null || !Field.IsPointInMBR(x2, y2, true))
                Foothold = Field.GetFootholdClosest(x2, CurPos.Y, ref x2, ref y2, CurPos.X);

            Drop Drop = null;
            if (_MergeDrops && !ByUser && GetDrop(OwnerID, Reward, x2, y2, out Drop))
            {
                Drop.CreateTime = MasterThread.CurrentTime;
                Drop.FFA = true;
                bool Changed = false;

                if (Drop.Reward.Mesos)
                {
                    int Value = Reward.Drop + Drop.Reward.Drop;
                    if (Drop.Reward.Drop < 50 && Value >= 50)
                        Changed = true;
                    else if (Drop.Reward.Drop < 100 && Value >= 100)
                        Changed = true;
                    else if (Drop.Reward.Drop < 1000 && Value >= 1000)
                        Changed = true;
                    Drop.Reward.Drop = Value;
                }
                else
                    Drop.Reward.GetData().Amount += Reward.Amount;

                DropPacket.SendMakeEnterFieldPacket(Drop, RewardEnterType.DisappearDuringDrop, Delay);

                if (Changed)
                {
                    DropPacket.SendMakeLeaveFieldPacket(Drop, RewardLeaveType.Remove);
                    DropPacket.SendMakeEnterFieldPacket(Drop, RewardEnterType.ShowExisting, 0);
                }
                return true;
            }
            else
            {
                Drop = new Drop(_DropIdCounter.NextValue(), Reward, OwnerID, OwnPartyID, OwnType, SourceID, CurPos.X, CurPos.Y, (short)x2, (short)y2, ByPet, ByUser)
                {
                    Field = Field,
                    CreateTime = MasterThread.CurrentTime,
                    Pos = Pos,
                    Everlasting = DropEverlasting,
                    ConsumeOnPickup = (!Reward.Mesos && false/*DataProvider.ConsumeOnPickup.Contains(Reward.ItemID)*/)
                };

                if (!Admin && ByUser && !Drop.Reward.Mesos && ((DataProvider.QuestItems.Contains(Reward.ItemID) || DataProvider.UntradeableDrops.Contains(Reward.ItemID))))
                    DropPacket.SendMakeEnterFieldPacket(Drop, RewardEnterType.DisappearDuringDrop, Delay);
                else
                {
                    Drops.Add(Drop.DropID, Drop);
                    DropPacket.SendMakeEnterFieldPacket(Drop, RewardEnterType.DropAnimation, Delay);
                }
                return false;
            }
        }

        #region Update
        public void Update(long tCur)
        {
            if (DropEverlasting) return;

            foreach (var Drop in new List<Drop>(Drops.Values))
            {
                if (!Drop.Everlasting && (tCur - Drop.CreateTime) > DropExpireTime)
                    RemoveDrop(Drop);
            }
        }
        #endregion

        public Drop GetDrop(int DropID)
        {
            Drops.TryGetValue(DropID, out Drop Result);

            return Result;
        }

        public bool GetDrop(int OwnerID, Reward Reward, int x, int y, out Drop Result)
        {
            Result = null;
            /*var tCur = DateTime.Now;

            if ((!Reward.Mesos && !Constants.isRechargeable(Reward.Drop)) || Reward.Mesos)
            {
                Point Pos = new Point(x, y);
                foreach (Drop Drop in Drops.Values)
                {
                    if (Drop.SourceID != 0 && Drop.MergeArea.Contains(Pos))
                    {
                        if (((Drop.Reward.Mesos && Reward.Mesos == Drop.Reward.Mesos) || (Reward.Drop == Drop.Reward.Drop && Drop.Reward.Amount + Reward.Amount < Reward.MaxStack)) 
                          && !Drop.ToExplode && ((tCur - Drop.CreateTime).TotalMinutes > 1 || Drop.OwnerID == OwnerID))
                        {
                            Result = Drop;
                            return true;
                        }
                    }
                }
            }*/
            return false;
        }

        public void Clear(RewardLeaveType rlt = RewardLeaveType.Normal)
        {
            foreach (Drop Drop in new List<Drop>(Drops.Values))
            {
                RemoveDrop(Drop, rlt);
            }
        }

        public void OnEnter(Character User)
        {
            foreach (Drop Drop in Drops.Values)
            {
                DropPacket.SendMakeEnterFieldPacket(Drop, RewardEnterType.ShowExisting, 0, User);
            }
        }

        public void RemoveDrop(Drop Drop, RewardLeaveType Type = RewardLeaveType.Normal, int Option = 0)
        {
            if (Drops.Remove(Drop.DropID))
                DropPacket.SendMakeLeaveFieldPacket(Drop, Type, Option);
        }

        public void EncodeForMigration(Packet pw)
        {
            pw.WriteInt(_DropIdCounter.Current);
            pw.WriteInt(Drops.Count);
            Drops.ForEach(x => x.Value.EncodeForMigration(pw));
        }

        public void DecodeForMigration(Packet pr)
        {
            _DropIdCounter.Reset(pr.ReadInt());
            int amount = pr.ReadInt();
            Drops = new Dictionary<int, Drop>(amount);

            Program.MainForm.LogAppend(Field.ID + " has " + amount + " drops...");
            for (var i = 0; i < amount; i++)
            {
                var drop = Drop.DecodeForMigration(pr);
                drop.Field = Field;
                Drops.Add(drop.DropID, drop);
            }
        }
    }

    public enum DropType : byte
    {
        Normal = 0,
        Party = 1,
        FreeForAll = 2,
        Explosive = 3
    }

    public enum RewardEnterType
    {
        ShowDrop = 0,
        DropAnimation = 1,
        ShowExisting = 2,
        DisappearDuringDrop = 3
    }

    public enum RewardLeaveType
    {
        Normal = 0,
        Party = 1,
        FreeForAll = 2,
        Remove = 3,
        Explode = 4,
        PetPickup = 5
    }
}
