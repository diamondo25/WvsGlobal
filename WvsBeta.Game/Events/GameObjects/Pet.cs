using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WvsBeta.Common;

namespace WvsBeta.Game {
	public class Pet : MovableLife {
		public Item Item { get; set; }
		public string Name { get; set; }
		public byte Level { get; set; }
		public short Closeness { get; set; }
		public byte Fullness { get; set; }
		public long Expiration { get; set; }
		public bool Spawned { get; set; }
	}
}
