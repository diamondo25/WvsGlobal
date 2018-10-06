using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WvsBeta.Game {
	public class OmokStone {
		public byte mX {get;set;}
		public byte mY {get;set;}
		public bool mOpened {get;set;}
	}

	public class Omok : MiniRoomBase {
		public bool[] mLeaveBooked { get; set; }
		public bool[] mRetreat { get; set; }
		public int mGameResult { get; set; }
		public byte mWinnerIndex { get; set; }
		public int[][] mCheckedStones { get; set; }
		public int[] mPlayerColor { get; set; }
		public byte mCurrentTurnIndex { get; set; }
		public int mLastStoneChecker { get; set; }
		public bool mUserReady { get; set; }

		public List<OmokStone> mStones { get; set; }

		public Omok() : base(2) {
			mStones = new List<OmokStone>();
		}
	}
}
