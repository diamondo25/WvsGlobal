using System.Collections.Generic;
using System.Linq;
using WvsBeta.Common;
using WvsBeta.Game.Events.PartyQuests;

namespace WvsBeta.Game
{
    public class MysticDoor
    {
        public readonly int OwnerId;
        public int OwnerPartyId { get; set; }
        public readonly short X;
        public readonly short Y;
        public readonly int FieldId;
        public long EndTime { get; set; }

        public MysticDoor(int ownerId, int ownPtId, short x, short y, int fieldId, long tEnd)
        {
            OwnerId = ownerId;
            OwnerPartyId = ownPtId;
            X = x;
            Y = y;
            FieldId = fieldId;
            EndTime = tEnd;
        }

        public bool CanEnterDoor(Character chr)
        {
            if (chr.PartyID != -1 && chr.PartyID != 0 && OwnerPartyId == chr.PartyID) return true;
            if (chr.ID == OwnerId) return true;
            return false;
        }
    }

    public class DoorManager
    {

        public readonly Dictionary<int, MysticDoor> DoorsLeadingHere;
        private readonly Dictionary<int, MysticDoor> Doors;
        private readonly Map Field;

        public DoorManager(Map field)
        {
            Doors = new Dictionary<int, MysticDoor>();
            DoorsLeadingHere = new Dictionary<int, MysticDoor>();
            Field = field;
        }

        public void TryRemoveDoor(int ownerCharId)
        {
            if (Doors.ContainsKey(ownerCharId))
            {
                var door = Doors[ownerCharId];
                Doors.Remove(ownerCharId);

                Field.SendPacket(MapPacket.RemoveDoor(door, 0));

                DataProvider.Maps[Field.ReturnMap].DoorPool.DoorsLeadingHere.Remove(ownerCharId);

                //in case owner is in town when it dies
                if (door.OwnerPartyId == 0)
                {
                    var owner = Server.Instance.GetCharacter(ownerCharId);
                    if (owner != null && owner.Field.ID == Field.ReturnMap)
                    {
                        MapPacket.RemovePortal(owner);
                        owner.DoorMapId = Constants.InvalidMap;
                    }
                }
                else
                {
                    Server.Instance.CenterConnection.PartyDoorRemoved(ownerCharId);
                }
            }
        }

        public void CreateDoor(Character chr, short x, short y, long endTime)
        {
            var door = new MysticDoor(chr.ID, chr.PartyID, x, y, Field.ID, endTime);
            Doors.Add(chr.ID, door);
            Field.SendPacket(MapPacket.ShowDoor(door, 0));

            DataProvider.Maps[Field.ReturnMap].DoorPool.DoorsLeadingHere.Add(chr.ID, door);

            //Owner is never in town when spawning door out of town, so no need to send portal spawn packet til he enters town

            if (door.OwnerPartyId != 0)
            {
                Server.Instance.CenterConnection.PartyDoorCreated(door);
            }
        }

        public void ShowAllDoorsTo(Character fucker)
        {
            foreach (var d in Doors.Values)
            {
                fucker.SendPacket(MapPacket.ShowDoor(d, 1));
            }
            
            foreach (var d in DoorsLeadingHere.Values)
            {
                if (d.OwnerId == fucker.ID)
                {
                    if (fucker.PartyID != 0) continue;
                    d.OwnerPartyId = 0; // Update portal just to be sure
                    MapPacket.SpawnPortal(fucker, Field.ReturnMap, Field.ReturnMap, d.X, d.Y);
                }
            }
        }

        public void Update(long pNow)
        {
            foreach (var door in Doors.Values.ToList())
            {
                if (door.EndTime < pNow)
                    TryRemoveDoor(door.OwnerId);
            }
        }

        public bool TryGetDoor(int charId, out MysticDoor door)
        {
            return Doors.TryGetValue(charId, out door);
        }
        
    }
}
