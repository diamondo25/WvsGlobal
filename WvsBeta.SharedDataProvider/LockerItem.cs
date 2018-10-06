using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using WvsBeta.Common.Sessions;

namespace WvsBeta.SharedDataProvider
{
    public class LockerItem
    {
        public long CashId { get; set; }
        public int UserId { get; set; }
        public int CharacterId { get; set; }
        public int ItemId { get; set; }
        public int CommodityId { get; set; }
        public short Amount { get; set; }
        public string BuyCharacterName { get; set; }
        public long Expiration { get; set; }
        public bool GiftUnread { get; set; }

        public bool SavedToDatabase { get; set; } = false;

        public LockerItem() { }
        
        public LockerItem(MySqlDataReader data)
        {
            CashId = data.GetInt64("cashid");
            UserId = data.GetInt32("userid");
            CharacterId = data.GetInt32("characterid");
            ItemId = data.GetInt32("itemid");
            CommodityId = data.GetInt32("commodity_id");
            Amount = data.GetInt16("amount");
            BuyCharacterName = data.GetString("buycharactername");
            Expiration = data.GetInt64("expiration");
            GiftUnread = data.GetBoolean("gift_unread");
        }

        public string GetFullUpdateColumns()
        {
            return
                "cashid = " + CashId + ", " +
                "userid = " + UserId + ", " +
                "characterid = " + CharacterId + ", " +
                "itemid = " + ItemId + ", " +
                "commodity_id = " + CommodityId + ", " +
                "amount = " + Amount + ", " +
                "buycharactername = '" + MySqlHelper.EscapeString(BuyCharacterName) + "', " +
                "expiration = " + Expiration + ", " +
                "gift_unread = " + GiftUnread + "";
        }

        public void Encode(Packet packet)
        {
            packet.WriteLong(CashId);
            packet.WriteInt(UserId);
            packet.WriteInt(CharacterId);
            packet.WriteInt(ItemId);
            packet.WriteInt(CommodityId);
            packet.WriteShort(Amount);

            packet.WriteString(BuyCharacterName, 13);
            packet.WriteLong(Expiration);
            packet.WriteByte((byte)(GiftUnread ? 1 : 0));
            packet.WriteByte(0);
            packet.WriteByte(0);
            packet.WriteByte(0);
        }


    }
}
