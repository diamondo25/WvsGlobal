using System.Collections.Generic;
using WvsBeta.Common;
using WvsBeta.Common.Sessions;

namespace WvsBeta.Game
{
    public class MiniRoomBase
    {
        protected string _transaction = Cryptos.GetNewSessionHash();
        public string TransactionID => _transaction;

        public static Dictionary<int, MiniRoomBase> MiniRooms = new Dictionary<int, MiniRoomBase>();
        public static Dictionary<int, PlayerShop> PlayerShops = new Dictionary<int, PlayerShop>();
        public static Dictionary<int, Omok> Omoks = new Dictionary<int, Omok>();

        public enum RoomType : byte
        {
            Omok = 1,
            MemoryGame = 2,
            Trade = 3,
            PersonalShop = 4,
            EntrustedShop = 5,
        }

        public int ID { get; protected set; }
        public int BalloonID { get; protected set; }
        public string Title { get; protected set; }
        public string Password { get; protected set; }
        public byte MaxUsers { get; protected set; }
        public byte EnteredUsers { get; protected set; }
        public Character[] Users { get; protected set; }
        public bool Opened { get; protected set; }
        public bool Private { get; protected set; }
        public bool CloseRequest { get; protected set; }
        public bool GameStarted { get; set; }
        public bool Tournament { get; protected set; }
        public int RoundID { get; protected set; }
        public Pos mHost { get; protected set; }
        public RoomType Type { get; private set; }
        public int ObjectID { get; private set; }
        public byte PieceType { get; private set; }
        public byte mWinnerIndex { get; set; }

        public MiniRoomBase(byte pMaxUsers, RoomType pType)
        {
            ID = Server.Instance.MiniRoomIDs.NextValue();
            MiniRooms.Add(ID, this);
            Title = "";
            Password = "";
            MaxUsers = pMaxUsers;
            Users = new Character[MaxUsers];
            Opened = false;
            CloseRequest = false;
            Tournament = false;
            GameStarted = false;
            Type = pType;
        }

        public virtual void Close(byte pReason)
        {
            MiniRooms.Remove(ID);
            for (var i = 0; i < MaxUsers; i++)
                Users[i] = null;
        }

        public byte GetEmptySlot()
        {
            for (byte i = 0; i < MaxUsers; i++)
            {
                if (Users[i] == null)
                {
                    return i;
                }
            }

            return 0xFF;
        }

        public byte GetCharacterSlotID(Character pCharacter)
        {
            return pCharacter.RoomSlotId;
        }

        public void BroadcastPacket(Packet pPacket, Character pSkipMeh = null)
        {
            foreach (Character chr in Users) if (chr != null && chr != pSkipMeh) chr.SendPacket(pPacket);
        }

        public bool IsFull()
        {
            return EnteredUsers == MaxUsers;
        }

        public virtual void RemovePlayer(Character pCharacter, byte pReason)
        {
            //Users[pCharacter.RoomSlotId] = null;

            MiniRoomPacket.ShowLeave(this, pCharacter, pReason);
            Users[pCharacter.RoomSlotId] = null;
            pCharacter.Room = null;
            pCharacter.RoomSlotId = 0;
            EnteredUsers--;


            if (EnteredUsers == 0)
            {
                this.Close(0);
            }
        }

        public void RemovePlayerFromShop(Character pCharacter)
        {
            MiniRoomBase mrb = pCharacter.Room;

            if (pCharacter == Users[0])
            {
                // Kick all players
                for (int i = 0; i < EnteredUsers; i++)
                {
                    if (pCharacter != Users[i])
                    {
                        PlayerShopPackets.CloseShop(Users[i], 2);
                        EnteredUsers--;
                        Users[i].Room = null;
                        Users[i].RoomSlotId = 0;
                    }

                }

                PlayerShop ps = PlayerShops[mrb.ID];
                ps.RevertItems(pCharacter);
                MiniGamePacket.RemoveAnnounceBox(pCharacter);
                PlayerShops.Remove(mrb.ID);
                pCharacter.Field.PlayerShops.Remove(mrb.ID);
                pCharacter.Room = null;
                pCharacter.RoomSlotId = 0;
            }

            else
            {
                PlayerShopPackets.RemovePlayer(pCharacter, mrb);
                EnteredUsers--;
                Users[pCharacter.RoomSlotId] = null;
                pCharacter.Room = null;
                pCharacter.RoomSlotId = 0;
            }
        }

        public virtual void AddPlayer(Character pCharacter)
        {
            _transaction += " " + pCharacter.Name + " (" + pCharacter.ID + ")";
            EnteredUsers++;
            pCharacter.RoomSlotId = GetEmptySlot();
            Users[pCharacter.RoomSlotId] = pCharacter;
        }

        public bool CheckPassword(string pPass)
        {
            return Password.Equals(pPass);
        }

        public virtual void EncodeLeave(Character pCharacter, Packet pPacket) { }

        public virtual void EncodeEnter(Character pCharacter, Packet pPacket) { }

        public virtual void EncodeEnterResult(Character pCharacter, Packet pPacket) { }

        public virtual void OnPacket(Character pCharacter, byte pOpcode, Packet pPacket) { }

        public virtual void AddItemToShop(Character pCharacter, PlayerShopItem Item) { }

        public static MiniRoomBase CreateRoom(Character pOwner, byte pType, Packet pPacket, bool pTournament, int pRound)
        {
            switch ((RoomType)pType)
            {
                case RoomType.Trade:
                    {
                        Trade trade = new Trade(pOwner);
                        trade.AddPlayer(pOwner);
                        return trade;
                    }

                case RoomType.Omok:
                    {
                        Omok omok = new Omok(pOwner)
                        {
                            Title = pPacket.ReadString(),
                            Private = pPacket.ReadBool()
                        };
                        if (omok.Private == true)
                        {
                            omok.Password = pPacket.ReadString();
                        }

                        pPacket.Skip(7); //Important ? :S
                        omok.PieceType = pPacket.ReadByte();
                        omok.AddOwner(pOwner);
                        omok.mWinnerIndex = 1;
                        Omoks.Add(omok.ID, omok);
                        return omok;
                    }

                case RoomType.PersonalShop:
                    {
                        PlayerShop ps = new PlayerShop(pOwner)
                        {
                            Title = pPacket.ReadString(),
                            Private = pPacket.ReadBool()
                        };
                        short x = pPacket.ReadShort(); // might be type of shop (different shops had different outer designs/looks)? unused var. not sure what it's purpose it serves.
                        ps.ObjectID = pPacket.ReadInt();
                        PlayerShops.Add(ps.ID, ps);
                        return ps;
                    }

                default:
                    {
                        return null;
                    }
            }
        }
    }
}