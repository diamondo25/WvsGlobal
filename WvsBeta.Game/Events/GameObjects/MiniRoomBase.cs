using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WvsBeta.Common;

namespace WvsBeta.Game {
	public class MiniRoomBase {
		public int mMiniRoomID { get; set; }
		public int mBalloonID { get; set; }
		public string mTitle { get; set; }
		public string mPassword { get; set; }
		public int mMaxUsers { get; set; }
		public int mCurrentUsers { get; set; }
		public List<Character> mUsers { get; set; }
		public bool mOpened { get; set; }
		public bool mPrivate { get; set; }
		public bool mCloseRequest { get; set; }
		public bool mGameOn { get; set; }
		public bool mTournament { get; set; }
		public int mRound { get; set; }
		public Pos mHost { get; set; }

		public MiniRoomBase(int pMaxUsers) {
			mMiniRoomID = Server.Instance.MiniRoomIDs.NextValue();
			mTitle = "";
			mPassword = "";
			mMaxUsers = pMaxUsers;
			mCurrentUsers = 0;
			mUsers = new List<Character>();
			mOpened = false;
			mCloseRequest = false;
			mTournament = false;
			mGameOn = false;
		}

		public bool CheckPassword(string pPass) { return mPassword.Equals(pPass); }
	}
}
