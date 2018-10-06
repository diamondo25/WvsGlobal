using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WvsBeta.Game {
	public interface INpcScript {
		void Init(IHost host);

		void Run(Character character, byte State, byte Answer, string StringAnswer, int IntegerAnswer);
	}

	public interface IHost {
		int mID { get; }
		void SendNext(string Message);
		void SendBackNext(string Message);
		void SendBackOK(string Message);
		void SendOK(string Message);
		void AskMenu(string Message);
		void AskYesNo(string Message);
		void AskText(string Message, string Default, short MinLenght, short MaxLength);
		void AskInteger(string Message, int Default, int MinValue, int MaxValue);
		void AskStyle(string Message, List<int> Values);
		void Stop();
	}
}
