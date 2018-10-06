using System;
using System.Collections.Generic;

namespace WvsBeta.Game
{
    public class OmokStone
    {
        public int mX { get; set; }
        public int mY { get; set; }
        public bool mOpened { get; set; }
        public byte mType { get; set; }

        public OmokStone(int X, int Y, byte Type)
        {
            mX = X;
            mY = Y;
            mType = Type;
        }
    }

    public class Omok : MiniRoomBase
    {
        public bool[] mLeaveBooked { get; set; }
        public bool[] mRetreat { get; set; }
        public int mGameResult { get; set; }
        public int[][] mCheckedStones { get; set; }
        public int[] mPlayerColor { get; set; }
        public byte mCurrentTurnIndex { get; set; }
        public int mLastStoneChecker { get; set; }
        public bool mUserReady { get; set; }
        public byte OmokType { get; set; }
        public byte[,] Stones { get; set; }
        public byte[,] LastPlaced { get; set; }
        public int TotalStones { get; set; }
        public bool[] PlacedStone { get; set; }

        public Dictionary<byte, OmokStone> LastPlacedStone { get; set; }
        public Character Owner { get; private set; }

        public Omok(Character pOwner)
            : base(2, RoomType.Omok)
        {
            mCurrentTurnIndex = 0;
            Stones = new byte[15, 15];
            LastPlaced = new byte[15, 15];
            PlacedStone = new bool[2] { false, false };
            Owner = pOwner;
            TotalStones = 0;
            LastPlacedStone = new Dictionary<byte, OmokStone>();
        }

        public void AddOwner(Character pOwner)
        {
            EnteredUsers++;
            pOwner.RoomSlotId = GetEmptySlot();
            Users[pOwner.RoomSlotId] = pOwner;
        }

        public void AddUser(Character pTo)
        {
            EnteredUsers++;
            pTo.RoomSlotId = GetEmptySlot();
            Users[pTo.RoomSlotId] = pTo;
        }

        public void CloseOmok(Character pOwner)
        {
            MiniGamePacket.RemoveAnnounceBox(pOwner);
            for (int i = 0; i < 2; i++)
            {
                if (Users[i] != null)
                {
                    if (Users[i].RoomSlotId == 1)
                    {
                        MiniGamePacket.RoomClosedMessage(Users[i]);
                    }

                    Users[i].Room = null;
                    Users[i].RoomSlotId = 0;
                    EnteredUsers--;
                }
            }
        }

        public void UpdateGame(Character pWinnner, bool Draw = false, bool Forfeit = false)
        {
            if (Draw)
            {
                Users[0].GameStats.OmokTies++;
                Users[1].GameStats.OmokTies++;
            }
            else
            {
                for (int i = 0; i < 2; i++)
                {
                    if (Users[i] == pWinnner)
                    {
                        pWinnner.GameStats.OmokWins++;
                        if (pWinnner.RoomSlotId == 0) mWinnerIndex = 1;
                        if (pWinnner.RoomSlotId == 1) mWinnerIndex = 0;
                    }
                    else
                    {
                        Users[i].GameStats.OmokLosses++;
                    }
                }
            }
            if (Draw) MiniGamePacket.UpdateGame(pWinnner, pWinnner.Room, 1);
            if (Forfeit) MiniGamePacket.UpdateGame(pWinnner, pWinnner.Room, 2);
            else MiniGamePacket.UpdateGame(pWinnner, pWinnner.Room, 0);

            //Reset all values
            Stones = new byte[15, 15];
        }

        public void UpdateAnnounceBox(Character pOwner)
        {

        }

        public byte GetOtherPiece(byte Piece)
        {
            if (Piece == 1) return 2;
            if (Piece == 2) return 1;
            return 0xFF;
        }

        public void AddStone(int X, int Y, byte Piece, Character chr)
        {
            this.Stones[X, Y] = Piece;

            if (!LastPlacedStone.ContainsKey(Piece))
            {
                LastPlacedStone.Add(Piece, new OmokStone(X, Y, Piece));
            }
            else
            {
                LastPlacedStone[Piece].mX = X;
                LastPlacedStone[Piece].mX = Y;
            }

            for (int i = 0; i < 2; i++)
            {
                if (Users[i] == chr)
                {
                    PlacedStone[i] = true;
                }
                else
                {
                    PlacedStone[i] = false;
                }
            }
            TotalStones++;

        }

        public bool CheckStone(byte Piece)
        {
            if (CheckStoneDiagonal(Piece, true) || CheckStoneDiagonal(Piece, false)
            || CheckStoneHorizontal(Piece) || CheckStoneVertical(Piece)) return true;
            else return false;
        }

        private bool CheckStones(int x, int y, byte piece)
        {
            int count(int xPos, int yPos, Func<int, int> xInc, Func<int, int> yInc, int result = 0)
            {
                if (x >= Stones.GetLength(0) || y >= Stones.GetLength(1) || x < 0 || y < 0 || Stones[x, y] != piece)
                    return result;
                else
                    return count(xInc(x), yInc(y), xInc, yInc, result + 1);
            }

            int diag1 = count(x, y, c => c + 1, c => c + 1) + count(x, y, c => c - 1, c => c - 1);
            int diag2 = count(x, y, c => c + 1, c => c - 1) + count(x, y, c => c - 1, c => c + 1);
            int horz = count(x, y, c => c + 1, c => c) + count(x, y, c => c - 1, c => c);
            int vert = count(x, y, c => c, c => c + 1) + count(x, y, c => c, c => c - 1);

            return diag1 > 5 || diag2 > 5 || horz > 5 || vert > 5; 
            //must be > 5 instead of >= because adding two count() calls overcounts by 1 (the first piece is counted twice)
        }

        //Credits to Loki for these formulas, had to tweak them a bit though.
        public bool CheckStoneDiagonal(byte Piece, bool Up)
        {
            if (Up) //from Top left to bottom right or vice versa
            {
                for (int i = 4; i < 15; i++)
                {
                    for (int j = 0; j < 11; j++)
                    {
                        if (this.Stones[j, i] == Piece &&
                        this.Stones[j + 1, i - 1] == Piece &&
                        this.Stones[j + 2, i - 2] == Piece &&
                        this.Stones[j + 3, i - 3] == Piece &&
                        this.Stones[j + 4, i - 4] == Piece)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            else
            {
                for (int i = 0; i < 11; i++)
                {
                    for (int j = 0; j < 11; j++)
                    {
                        if (this.Stones[j, i] == Piece &&
                        this.Stones[j + 1, i + 1] == Piece &&
                        this.Stones[j + 2, i + 2] == Piece &&
                        this.Stones[j + 3, i + 3] == Piece &&
                        this.Stones[j + 4, i + 4] == Piece)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public bool CheckStoneHorizontal(byte Piece)
        {
            for (int i = 0; i < 15; i++)
            {
                for (int j = 0; j < 11; j++)
                {
                    if (this.Stones[j, i] == Piece &&
                    this.Stones[j + 1, i] == Piece &&
                    this.Stones[j + 2, i] == Piece &&
                    this.Stones[j + 3, i] == Piece &&
                    this.Stones[j + 4, i] == Piece)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool CheckStoneVertical(byte Piece)
        {
            for (int i = 0; i < 11; i++)
            {
                for (int j = 0; j < 15; j++)
                {
                    if (this.Stones[j, i] == Piece &&
                    this.Stones[j, i + 1] == Piece &&
                    this.Stones[j, i + 2] == Piece &&
                    this.Stones[j, i + 3] == Piece &&
                    this.Stones[j, i + 4] == Piece)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}