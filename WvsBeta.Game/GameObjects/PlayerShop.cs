using System.Collections.Generic;
using WvsBeta.Common;
using WvsBeta.Common.Tracking;

namespace WvsBeta.Game
{
    public class PlayerShopItem
    {
        public int Price { get; set; }
        public short Bundles { get; set; }
        public short BundleAmount { get; set; }
        public byte ShopSlot { get; set; }
        public BaseItem sItem { get; set; }

        public PlayerShopItem(BaseItem Item)
        {
            this.sItem = Item;
            this.Price = 0;
            this.Bundles = 0;
            this.ShopSlot = 0;
        }
    }

    public class PlayerShop : MiniRoomBase
    {
        public Dictionary<byte, PlayerShopItem> Items { get; set; } = new Dictionary<byte, PlayerShopItem>();
        public List<PlayerShopItem> BoughtItems { get; set; } = new List<PlayerShopItem>();
        public int Mesos { get; set; }
        public Character Owner { get; private set; }

        public PlayerShop(Character pOwner) : base(4, RoomType.PersonalShop)
        {
            Owner = pOwner;
            EnteredUsers++;
            Owner.RoomSlotId = GetEmptySlot();
            Users[Owner.RoomSlotId] = Owner;
        }
        
        public void AddUser(Character pTo)
        {
            EnteredUsers++;
            pTo.RoomSlotId = GetEmptySlot();
            Users[pTo.RoomSlotId] = pTo;
        }

        public void RevertItems(Character pOwner)
        {
            if (pOwner == pOwner.Room.Users[0])
            {
                foreach (var pst in Items)
                {
                    if (pst.Value.sItem.Amount != 0) //If an item is set to 0, no point of adding it.
                    {
                        pOwner.Inventory.AddItem2(pst.Value.sItem);
                        ItemTransfer.PersonalShopGetBackItem(pOwner.ID, pst.Value.sItem.ItemID, pst.Value.sItem.Amount, _transaction, pst.Value.sItem);
                        pst.Value.sItem = null;
                    }
                }
            }
        }

        public override void RemovePlayer(Character pCharacter, byte pReason)
        {
            if (pCharacter.Room.Type == MiniRoomBase.RoomType.PersonalShop)
            {
                RevertItems(pCharacter);
            }
            base.RemovePlayer(pCharacter, pReason);
        }

        public static void HandleShopUpdateItem(Character pCharacter, byte inv, short invslot, short bundle, short bundleamount, int price)
        {
            MiniRoomBase mrb = pCharacter.Room;

            if (pCharacter.AssertForHack(mrb.Users[0] != pCharacter,
                "PlayerShop hack: Tried to update shop item while not owner.")) return;

            BaseItem tehItem = pCharacter.Inventory.GetItem(inv, invslot);
            if (tehItem == null)
            {
                //Doesn't have item in inventory
                ReportManager.FileNewReport("Tried adding an item into player shop without having it.", pCharacter.ID, 0);
                InventoryPacket.NoChange(pCharacter);
                return;
            }

            var newItem = tehItem.Duplicate();
            newItem.InventorySlot = invslot;

            if (newItem.Amount < bundle || bundle <= 0 || bundle > 100)
            {
                //Packet edits 
                ReportManager.FileNewReport("Tried adding an item into player shop with an incorrect amount/incorrect itemid.", pCharacter.ID, 0);
                InventoryPacket.NoChange(pCharacter);
                return;
            }

            PlayerShopItem pst = new PlayerShopItem(newItem)
            {
                Price = price
            };
            pst.sItem.Amount = (short)(bundle * bundleamount);
            pst.Bundles = bundle;
            pst.BundleAmount = bundleamount;
            mrb.AddItemToShop(pCharacter, pst);
            if (Constants.isStackable(pst.sItem.ItemID))
            {
                pCharacter.Inventory.TakeItemAmountFromSlot(inv, invslot, (short)(bundle * bundleamount), false);
            }
            else
            {
                pCharacter.Inventory.TakeItem(pst.sItem.ItemID, bundle);
            }

            ItemTransfer.PersonalShopPutUpItem(pCharacter.ID, pst.sItem.ItemID, pst.sItem.Amount, mrb.TransactionID, pst.sItem);

            InventoryPacket.NoChange(pCharacter); //-.-

        }

        public override void OnPacket(Character pCharacter, byte pOpcode, Common.Sessions.Packet pPacket)
        {
        }

        public override void AddItemToShop(Character pCharacter, PlayerShopItem Item)
        {
            if (Items.Count == 0)
            {
                Items.Add(0, Item);
                Item.ShopSlot = 0;
            }
            else
            {
                Items.Add((byte)Items.Count, Item);
                Item.ShopSlot = (byte)Items.Count;
            }
            PlayerShopPackets.PersonalShopRefresh(pCharacter, this);
            base.AddItemToShop(pCharacter, Item);
        }

        public void HandleMoveItemBack(Character pCharacter, byte slot)
        {
            if (Items.TryGetValue(slot, out PlayerShopItem pst))
            {
                pCharacter.Inventory.AddItem2(pst.sItem);
                ItemTransfer.PersonalShopGetBackItem(pCharacter.ID, pst.sItem.ItemID, pst.sItem.Amount, _transaction, pst.sItem);
                var x = pst.sItem.Amount;
                if (x == 1)
                {
                    PlayerShopPackets.MoveItemToInventory(pCharacter, 0, slot);
                    // Remove? but that would reorder it!!!
                }
                else
                {
                    byte left = (byte)(x - 1); //amount left
                    PlayerShopPackets.MoveItemToInventory(pCharacter, left, slot);
                }
            }
            InventoryPacket.NoChange(pCharacter);
        }

        public void BuyItem(Character pCharacter, byte slot, short quantity)
        {
            //This may seem confusing, but the client calculates the amount left itself.
            //The formula is bundles * bundleamount, so if you have 2 bundles with 25 in each, it will obviously show 50. If you have 100 items in 1 bundle, it will show you 100
            PlayerShopItem pst = Items[slot];
            PlayerShop ps = MiniRoomBase.PlayerShops[pCharacter.Room.ID];

            if (pst == null) return;

            if (pCharacter.AssertForHack(
                  quantity < 0,
                 $"PlayerShop hack: trying to buy negative amount. Requested {quantity}"
            )) return;

            if (pst.sItem.Amount <= 0)
            {
                // TODO: Get packet?
                return;
            }


            var cost = quantity * pst.Price;
            var realAmount = (short)(quantity * pst.BundleAmount);

            if (cost > pCharacter.Inventory.Mesos)
            {
                // TODO: Get packet?
                return;
            }

            if (pCharacter.AssertForHack(
                pst.Bundles < quantity,
                $"PlayerShop hack: buying more than there's in store. Bundle: {pst.Bundles}, requested {quantity}"
            )) return;

            if (pCharacter.AssertForHack(
                pst.sItem.Amount < realAmount,
                $"PlayerShop hack: buying more than there's in store. Item amount: {pst.sItem.Amount}, requested {realAmount}"
            )) return;


            pCharacter.Inventory.AddNewItem(pst.sItem.ItemID, realAmount);
            pCharacter.AddMesos(-cost);
            Owner.AddMesos(cost);

            MesosTransfer.PlayerBuysFromPersonalShop(pCharacter.ID, Owner.ID, cost, _transaction);
            ItemTransfer.PersonalShopBoughtItem(Owner.ID, pCharacter.ID, pst.sItem.ItemID, realAmount, TransactionID, pst.sItem);



            pst.Bundles -= quantity;
            pst.sItem.Amount -= realAmount;

            PlayerShopPackets.PersonalShopRefresh(pCharacter, ps);
            PlayerShopPackets.SoldItemResult(Owner, pCharacter, slot, quantity);
        }
    }
}
