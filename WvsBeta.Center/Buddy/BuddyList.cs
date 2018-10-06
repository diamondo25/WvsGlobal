using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using MySql.Data.MySqlClient;
using WvsBeta.Common;
using WvsBeta.Common.Sessions;

namespace WvsBeta.Center
{
    public class BuddyList
    {
        public static ILog log = LogManager.GetLogger("BuddylistLog");

        private byte Capacity;
        private readonly Dictionary<int, BuddyData> Buddies;
        public readonly BuddyData Owner;
        private readonly Queue<BuddyData> BuddyRequests;

        private BuddyList(byte cap, Character own)
        {
            Capacity = cap;
            Owner = new BuddyData(own.ID, own.Name);
            Buddies = new Dictionary<int, BuddyData>(Capacity);
            BuddyRequests = new Queue<BuddyData>();
        }


        public BuddyList(Packet pr)
        {
            Capacity = pr.ReadByte();
            Owner = new BuddyData(pr);
            Buddies = new Dictionary<int, BuddyData>(Capacity);
            BuddyRequests = new Queue<BuddyData>();

            int count = pr.ReadByte();
            for (var i = 0; i < count; i++)
            {
                var buddy = new BuddyData(pr);
                Buddies.Add(buddy.charId, buddy);
            }

            count = pr.ReadByte();
            for (var i = 0; i < count; i++)
            {
                var buddy = new BuddyData(pr);
                BuddyRequests.Enqueue(buddy);
            }
        }

        public void EncodeForTransfer(Packet pw)
        {
            pw.WriteByte(Capacity);
            Owner.EncodeForTransfer(pw);

            pw.WriteByte((byte)Buddies.Count);
            Buddies.ForEach(x => x.Value.EncodeForTransfer(pw));

            pw.WriteByte((byte)BuddyRequests.Count);
            BuddyRequests.ForEach(x => x.EncodeForTransfer(pw));
        }

        public Boolean IsFull()
        {
            return Buddies.Values.Count >= Capacity;
        }

        public Boolean HasBuddy(int id)
        {
            return Buddies.ContainsKey(id);
        }

        public Boolean HasBuddy(BuddyData bud)
        {
            return HasBuddy(bud.charId);
        }

        public void Add(BuddyData buddy, Boolean packet = true)
        {
            if (!HasBuddy(buddy) && !IsFull())
            {
                Buddies[buddy.charId] = buddy;

                if (packet)
                {
                    SendBuddyList();
                }
            }
        }

        public void Request(BuddyData requestor)
        {
            if (requestor.GetBuddyList().IsFull())
            {
                log.Warn($"[{Owner.charName}] Buddylist invite failed: its full");
                requestor.GetBuddyList().SendRequestError(11);
                return;
            }

            if (Owner.GetChar().IsGM && requestor.GetChar().IsGM == false)
            {
                log.Warn($"[{Owner.charName}] Buddylist invite failed: nonadmin to admin invite");
                requestor.GetBuddyList().SendRequestError(14);
                return;
            }

            if (HasBuddy(requestor))
            {
                log.Warn($"[{Owner.charName}] Buddylist invite failed: already as buddy");
                requestor.GetBuddyList().Add(Owner);
                SendBuddyList(); //TODO test sending Update. Sometimes we send full buddylist when it seems like only an update is necessary
                return;
            }
            if (IsFull())
            {
                log.Warn($"[{Owner.charName}] Buddylist invite failed: own is full");
                requestor.GetBuddyList().SendRequestError(12);
                return;
            }

            log.Warn($"[{Owner.charName}] invited {requestor.charName}");

            requestor.GetBuddyList().Add(Owner);
            BuddyRequests.Enqueue(requestor);
            if (BuddyRequests.Count == 1)
            {
                PollRequests();
            }
        }

        public void PollRequests()
        {
            if (BuddyRequests.Count != 0)
            {
                BuddyData requestor = BuddyRequests.Peek();
                SendInviteFrom(requestor);
            }
        }

        public void RemoveBuddyOrRequest(Character Victim, int victimId)
        {
            if (BuddyRequests.Count != 0 && BuddyRequests.Peek().charId == victimId)
            {
                BuddyData requestor = BuddyRequests.Dequeue();
                //requestor.GetBuddyList().SendBuddyList();
                PollRequests();
                //SendBuddyList();
            }
            else
            {
                Buddies.Remove(victimId);
                SendBuddyList();
                if (Victim != null)
                {
                    Victim.FriendsList.SendBuddyList(); //TODO test sending Update. Sometimes we send full buddylist when it seems like only an update is necessary
                }
            }
        }

        public void AcceptRequest()
        {
            if (BuddyRequests.Count != 0)
            {
                BuddyData requestor = BuddyRequests.Dequeue();
                Add(requestor);
                if (requestor.GetBuddyList() != null)
                    requestor.GetBuddyList().SendUpdate(Owner, false);
                PollRequests();
            }
        }

        public void IncreaseCapacity()
        {
            Capacity += 5;
            log.Warn($"[{Owner.charName}] Increasing buddylist capacity from to {Capacity}");
            SendCapacityChange();
            CenterServer.Instance.CharacterDatabase.RunQuery("UPDATE characters SET buddylist_size = " + Capacity + " WHERE ID = " + Owner.charId);
        }

        public void OnOnlineCC(bool toSave = true, bool disconnected = false)
        {
            Buddies.Values.ToList()
                .FindAll(b => Owner.IsVisibleTo(b))
                .ForEach(buddy => buddy.GetBuddyList().SendUpdate(Owner, disconnected));

            if (toSave == true)
            {
                SaveBuddiesToDb();
            }
        }

        #region Packets Stuff

        private void SendUpdate(BuddyData buddy, bool dc)
        {
            Packet pw = new Packet(ServerMessages.FRIEND_RESULT);
            pw.WriteByte(20);
            pw.WriteInt(buddy.charId);
            pw.WriteByte(0); // 0 = not in cash shop, 1 = in cash shop
            pw.WriteInt(dc == true ? -1 : buddy.GetChannel());
            Owner.SendPacket(pw);
        }

        public void BuddyChat(string message, int[] recipients)
        {
            Buddies.Values
                .Where(e => Owner.IsVisibleTo(e))
                .Where(x => recipients.Contains(x.charId))
                .ForEach(b => b.GetBuddyList().SendBuddyChat(Owner.charName, message, 0));
        }

        public void SendBuddyList()
        {
            Packet pw = new Packet(ServerMessages.FRIEND_RESULT);
            pw.WriteByte(0x07);
            pw.WriteByte((byte)(Buddies.Values.Count + Math.Min(1, BuddyRequests.Count)));

            Buddies.Values.ForEach(bud =>
            {
                pw.WriteInt(bud.charId);
                pw.WriteString(bud.charName, 13);
                pw.WriteByte(0);
                pw.WriteInt(Owner.IsVisibleTo(bud) == true ? bud.GetChannel() : -1);
            });

            if (BuddyRequests.Count != 0)
            {
                BuddyData data = BuddyRequests.Peek();
                pw.WriteInt(data.charId);
                pw.WriteString(data.charName, 13);
                pw.WriteByte(1);
                pw.WriteInt(data.GetChannel());
            }

            Enumerable.Range(0, (Buddies.Values.Count + BuddyRequests.Count())).ForEach(e => pw.WriteInt(0));
            Owner.SendPacket(pw);
        }

        public void SendRequestError(int msg)
        {
            Packet pw = new Packet(ServerMessages.FRIEND_RESULT);
            pw.WriteByte((byte)msg);
            Owner.SendPacket(pw);
        }

        private void SendInviteFrom(BuddyData from)
        {
            Packet pw = new Packet(ServerMessages.FRIEND_RESULT);
            pw.WriteByte(9);
            pw.WriteInt(from.charId);
            pw.WriteString(from.charName);
            pw.WriteInt(from.charId);
            pw.WriteString(from.charName, 13);
            pw.WriteByte(1);
            pw.WriteInt(from.GetChannel());
            pw.WriteByte(0);
            Owner.SendPacket(pw);
        }

        private void SendCapacityChange()
        {
            Packet pw = new Packet(ServerMessages.FRIEND_RESULT);
            pw.WriteByte(21);
            Owner.SendPacket(pw);
        }

        public void SendBuddyChat(String fromName, string text, byte group)
        {
            //!packet 2D 01 04 00 6A 6F 65 70 01003400000000000000
            Packet pw = new Packet(ServerMessages.GROUP_MESSAGE);
            pw.WriteByte(group);
            pw.WriteString(fromName);
            pw.WriteString(text);
            Owner.SendPacket(pw);
        }

        #endregion

        #region Database Stuff

        public static BuddyList LoadBuddyList(int id, string name)
        {
            BuddyList list;
            var chr = CenterServer.Instance.FindCharacter(id);
            byte capacity;
            using (var capData = CenterServer.Instance.CharacterDatabase.RunQuery(
                "SELECT buddylist_size FROM characters WHERE name = @name",
                "@name", name
             ) as MySqlDataReader)
            {
                capData.Read();
                capacity = (byte)capData.GetInt32("buddylist_size");
            }

            list = LoadFromDb(chr, capacity);
            return list;
        }

        private static BuddyList LoadDefault(Character chr)
        {
            return new BuddyList(20, chr);
        }

        private static BuddyList LoadFromDb(Character chr, byte capacity)
        {
            var newlist = new BuddyList(capacity, chr);

            using (var data = CenterServer.Instance.CharacterDatabase.RunQuery(
                "SELECT * FROM buddylist WHERE charid = @charid",
                "@charid", chr.ID
            ) as MySqlDataReader)
            {
                while (data.Read())
                {
                    int buddycharid = data.GetInt32("buddy_charid");
                    string buddyname = data.GetString("buddy_charname");
                    newlist.Add(new BuddyData(buddycharid, buddyname), false);
                }
            }

            using (var invitedata = CenterServer.Instance.CharacterDatabase.RunQuery("SELECT * FROM buddylist_pending WHERE charid = @charid", "@charid", chr.ID) as MySqlDataReader)
            {
                while (invitedata.Read())
                {
                    int inviterid = invitedata.GetInt32("inviter_charid");
                    string invitername = invitedata.GetString("inviter_charname");
                    newlist.BuddyRequests.Enqueue(new BuddyData(inviterid, invitername));
                }
            }


            return newlist;
        }

        public void SaveBuddiesToDb()
        {
            //BUDDY LIST
            CenterServer.Instance.CharacterDatabase.RunTransaction(comm =>
            {
                comm.CommandText = "DELETE FROM buddylist WHERE charid = @charid";
                comm.Parameters.AddWithValue("@charid", Owner.charId);
                comm.ExecuteNonQuery();

                foreach (var buddyEntry in Buddies)
                {
                    comm.Parameters.Clear();
                    comm.CommandText = "INSERT INTO buddylist (charid, buddy_charid, buddy_charname) VALUES (@ownerCharId, @charId, @charname)";
                    comm.Parameters.AddWithValue("@ownerCharId", Owner.charId);
                    comm.Parameters.AddWithValue("@charId", buddyEntry.Value.charId);
                    comm.Parameters.AddWithValue("@charname", buddyEntry.Value.charName);
                    comm.ExecuteNonQuery();
                }
            }, Program.MainForm.LogAppend);

            //PENDING REQUESTS
            CenterServer.Instance.CharacterDatabase.RunTransaction(comm =>
            {
                comm.CommandText = "DELETE FROM buddylist_pending WHERE charid = @charid";
                comm.Parameters.AddWithValue("@charid", Owner.charId);
                comm.ExecuteNonQuery();

                while (BuddyRequests.Count > 0)
                {
                    BuddyData requestor = BuddyRequests.Dequeue();

                    comm.Parameters.Clear();
                    comm.CommandText = "INSERT INTO buddylist_pending (charid, inviter_charid, inviter_charname) VALUES (@charid, @inviterCharId, @inviterCharName)";
                    comm.Parameters.AddWithValue("@charid", Owner.charId);
                    comm.Parameters.AddWithValue("@inviterCharId", requestor.charId);
                    comm.Parameters.AddWithValue("@inviterCharName", requestor.charName);
                    comm.ExecuteNonQuery();
                }
            }, Program.MainForm.LogAppend);
        }

        #endregion

    }
}
