using System.Collections.Generic;
using WvsBeta.Common;

namespace WvsBeta.Game
{
    public class Reactor
    {
        public readonly Map Field;

        public readonly short ID;
        private byte _state;
        public readonly short X;
        public readonly short Y;
        public readonly byte Z;
        public readonly byte ZM;

        public Character Owner { get; private set; }
        public List<(int itemId, short amount)> ItemDrops { get; set; } = new List<(int itemId, short amount)>();
        public int MesoDrop { get; set; }

        private static int MaxState = 4;

        public Reactor(Map pField, short pID, short pX, short pY) : this(pField, pID, 0, pX, pY)
        {
        }

        public Reactor(Map pField, short pID, byte pState, short pX, short pY) : this(pField, pID, 0, pX, pY, 3, 100)
        {
        }

        public Reactor(Map pField, short pID, byte pState, short pX, short pY, byte pZ, byte pZM)
        {
            Field = pField;
            ID = pID;
            _state = pState;
            X = pX;
            Y = pY;
            Z = pZ;
            ZM = pZM;
            Owner = null;
            MesoDrop = 0;
        }

        public byte State
        {
            get
            {
                return _state;
            }
            set
            {
                _state = value;
                if (State <= MaxState) //check state before change to avoid invalid pointer errors in client
                    ReactorPacket.ReactorChangedState(this);
                if (State >= MaxState)
                {
                    Field.RemoveReactor(ID);
                    DoDrop();
                }
            }
        }

        public void Show()
        {
            ReactorPacket.ShowReactor(this);
        }

        public void ShowTo(Character chr)
        {
            ReactorPacket.ShowReactor(this, true, chr);
        }

        public void UnShow()
        {
            ReactorPacket.DestroyReactor(this);
        }

        public void HitBy(Character chr)
        {
            Owner = chr;
            State++;
        }

        private void DoDrop()
        {
            int x2 = X - 10 * (ItemDrops.Count + MesoDrop > 0 ? 1 : 0) + 10;
            short delay = 0;
            foreach (var dropInfo in ItemDrops)
            {
                BaseItem it = BaseItem.CreateFromItemID(dropInfo.itemId, dropInfo.amount);
                it.GiveStats(ItemVariation.None);

                Field.DropPool.Create(Reward.Create(it), Owner.ID, Owner.PartyID, DropType.Normal, ID, new Pos(X, Y), x2, delay, false, 0, false, false);
                x2 += 20;
                delay += 120;
            }

            if (MesoDrop > 0)
            {
                Field.DropPool.Create(Reward.Create(MesoDrop), Owner.ID, Owner.PartyID, DropType.Normal, ID, new Pos(X, Y), x2, delay, false, 0, false, false);
            }
        }
    }
}
