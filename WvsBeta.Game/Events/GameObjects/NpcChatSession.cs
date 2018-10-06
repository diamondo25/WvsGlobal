using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WvsBeta.Game {
	public class NpcUsedLines {
		public byte mWhat { get; set; }
		public string mText { get; set; }
		public NpcUsedLines(byte what, string text) {
			mWhat = what;
			mText = text;
		}
	}

	public class NpcChatSession : IHost {
		public int mID { get; set; }
		public Character mCharacter { get; set; }
		private INpcScript _compiledScript = null;

		private List<NpcUsedLines> mLines { get; set; }

		private byte mState { get; set; }
		public byte mLastSentType { get; set; }
		public byte mRealState { get; set; }

		public NpcChatSession(int id, Character chr) {
			mID = id;
			mState = 0;
			mCharacter = chr;
			mCharacter.NpcSession = this;
			mLines = new List<NpcUsedLines>();
		}

		~NpcChatSession() {
			mLines.Clear();
			mLines = null;
		}

		public INpcScript CompiledScript {
			get { return _compiledScript; }
		}

		public void SetScript(INpcScript script) {
			_compiledScript = script;
		}

		public void HandleThing(byte state = 0, byte action = 0, string text = "", int integer = 0) {
			_compiledScript.Run(mCharacter, state, action, text, integer);
		}

		public void Stop() {
			mCharacter.NpcSession = null;
			_compiledScript = null;
		}

		public void SendPreviousMessage() {
			if (mState == 0) return;
			mState--;
			NpcUsedLines line = mLines[mState];
			switch (line.mWhat) {
				case 0: NpcPacket.SendNPCChatTextSimple(mCharacter, mID, line.mText, false, true); break;
				case 1: NpcPacket.SendNPCChatTextSimple(mCharacter, mID, line.mText, true, true); break;
				case 2: NpcPacket.SendNPCChatTextSimple(mCharacter, mID, line.mText, true, false); break;
				case 3: NpcPacket.SendNPCChatTextSimple(mCharacter, mID, line.mText, false, false); break;
				default: Stop(); return;
			}
		}

		public void SendNext(string Message) {
			// First line, always clear
			mLines.Clear();
			mLines.Add(new NpcUsedLines(0, Message));
			mState = 0;
			mRealState++;
			NpcPacket.SendNPCChatTextSimple(mCharacter, mID, Message, false, true);
		}

		public void SendBackNext(string Message) {
			mLines.Add(new NpcUsedLines(1, Message));
			mState++;
			mRealState++;
			NpcPacket.SendNPCChatTextSimple(mCharacter, mID, Message, true, true);
		}
		public void SendBackOK(string Message) {
			mLines.Add(new NpcUsedLines(2, Message));
			mState++;
			mRealState++;
			NpcPacket.SendNPCChatTextSimple(mCharacter, mID, Message, true, false);
		}
		public void SendOK(string Message) {
			mLines.Add(new NpcUsedLines(3, Message));
			mState++;
			mRealState++;
			NpcPacket.SendNPCChatTextSimple(mCharacter, mID, Message, false, false);
		}

		public void AskMenu(string Message) {
			mState = 0;
			mRealState++;
			NpcPacket.SendNPCChatTextMenu(mCharacter, mID, Message);
		}

		public void AskYesNo(string Message) {
			mState = 0;
			mRealState++;
			NpcPacket.SendNPCChatTextYesNo(mCharacter, mID, Message);
		}

		public void AskText(string Message, string Default, short MinLenght, short MaxLength) {
			mState = 0;
			mRealState++;
			NpcPacket.SendNPCChatTextRequestText(mCharacter, mID, Message, Default, MinLenght, MaxLength);
		}

		public void AskInteger(string Message, int Default, int MinValue, int MaxValue) {
			mState = 0;
			mRealState++;
			NpcPacket.SendNPCChatTextRequestInteger(mCharacter, mID, Message, Default, MinValue, MaxValue);
		}

		public void AskStyle(string Message, List<int> Values) {
			mState = 0;
			mRealState++;
			NpcPacket.SendNPCChatTextRequestStyle(mCharacter, mID, Message, Values);
		}

	}
}
