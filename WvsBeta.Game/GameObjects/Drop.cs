using System.Drawing;
using WvsBeta.Common;
using WvsBeta.Common.Sessions;

namespace WvsBeta.Game
{
    public class Drop : IFieldObj
    {
        public Map Field { get; set; }
        public int DropID { get; set; }
        public bool ByPet { get; set; }
        public bool ByUser { get; set; }
        public bool Everlasting { get; set; }
        public bool ConsumeOnPickup { get; set; }
        public DropType OwnType { get; set; }
        public int OwnerID { get; set; }
        public int OwnPartyID { get; set; }
        public long CreateTime { get; set; }
        public bool FFA { get; set; }
        public bool ToExplode { get; set; }
        public Reward Reward { get; set; }
        public Pos Pt1 { get; set; }
        public Pos Pt2 { get; set; }
        public Rectangle MergeArea { get; set; }
        public int SourceID { get; set; }
        public long DateExpire { get; set; }
        public int Period { get; set; }
        public short QuestID { get; set; }
        public short ShowMax { get; set; }
        public short Pos { get; set; }

        public Drop(int DropID, Reward reward, int OwnerID, int OwnPartyID, DropType OwnType, int SourceID, short x1, short y1, short x2, short y2, bool ByPet, bool ByUser)
        {
            this.DropID = DropID;
            this.Reward = reward;
            this.OwnerID = OwnerID;
            this.OwnPartyID = OwnPartyID;
            this.OwnType = OwnType;
            this.SourceID = SourceID;
            this.Pt1 = new Pos(x1, y1);
            this.Pt2 = new Pos(x2, y2);
            this.MergeArea = Rectangle.FromLTRB(x2 - 50, y2 - 20, x2 + 50, y2 + 20);
            this.ByPet = ByPet;
            this.ByUser = ByUser;
        }

        public bool CanTakeDrop(Character chr)
        {
            if (Server.Instance.InMigration) return false;

            var isPartyAble = chr.PartyID != 0 && OwnPartyID == chr.PartyID;
            var isOwnerDrop = OwnerID == chr.ID;
            
            if (isOwnerDrop) return true;
            if (isPartyAble) return true;


            if (!Field.EverlastingDrops)
            {
                if (ByUser) return true;

                var isAvailableForPickup = ((MasterThread.CurrentTime - CreateTime) / 60000) > 1;

                if (isAvailableForPickup)
                    return true;
            }

            return false;
        }

        public bool IsShownTo(IFieldObj Object)
        {
            var Result = false;

            if (Object is Character User && User.PrimaryStats.HP > 0 && User.MapID == Field.ID)
            {
                if (ShowMax > 0)
                {
                    var Count = User.Inventory.GetItemAmount(Reward.ItemID);

                    //TODO If quests give out cash items this needs to be changed
                    if (User.Inventory.GetEquippedItemId((short)Constants.GetBodyPartFromItem(Reward.ItemID), false) == Reward.ItemID)
                        Count++;

                    if (Count > ShowMax)
                        return false;
                }

                if (QuestID > 0)
                {
                    if (User.Quests.HasQuest(QuestID))
                    {
                        if (OwnType == DropType.Normal && User.ID == OwnerID ||
                            OwnType == DropType.Party && User.PartyID == OwnPartyID ||
                            OwnType == DropType.FreeForAll ||
                            OwnType == DropType.Explosive)
                        {
                            Result = true;
                        }
                    }
                }
                else
                    Result = true;
            }

            return Result;
        }

        public void EncodeForMigration(Packet pw)
        {
            pw.WriteInt(DropID);
            this.Reward.EncodeForMigration(pw);
            pw.WriteInt(OwnerID);
            pw.WriteInt(OwnPartyID);
            pw.WriteByte((byte)OwnType);
            pw.WriteInt(SourceID);
            pw.WriteShort(Pt1.X);
            pw.WriteShort(Pt1.Y);
            pw.WriteShort(Pt2.X);
            pw.WriteShort(Pt2.Y);
            pw.WriteBool(ByPet);
            pw.WriteBool(ByUser);
            pw.WriteShort(Pos);
            pw.WriteBool(Everlasting);
            pw.WriteBool(ConsumeOnPickup);
        }

        public static Drop DecodeForMigration(Packet pr)
        {
            var DropID = pr.ReadInt();
            var reward = Game.Reward.DecodeForMigration(pr);
            var OwnerID = pr.ReadInt();
            var OwnPartyID = pr.ReadInt();
            var OwnType = pr.ReadByte();
            var SourceID = pr.ReadInt();
            var Pt1X = pr.ReadShort();
            var Pt1Y = pr.ReadShort();
            var Pt2X = pr.ReadShort();
            var Pt2Y = pr.ReadShort();
            var ByPet = pr.ReadBool();
            var ByUser = pr.ReadBool();
            var Pos = pr.ReadShort();
            var DropEverlasting = pr.ReadBool();
            var ConsumeOnPickup = pr.ReadBool();

            var drop = new Drop(DropID, reward, OwnerID, OwnPartyID, (DropType)OwnType, SourceID, Pt1X, Pt1Y, Pt2X, Pt2Y, ByPet, ByUser);

            // Drop time is reset; cannot get the datetime transfer to work
            drop.CreateTime = MasterThread.CurrentTime;

            drop.Pos = Pos;
            drop.Everlasting = DropEverlasting;
            drop.ConsumeOnPickup = ConsumeOnPickup;
            return drop;
        }
    }
}
