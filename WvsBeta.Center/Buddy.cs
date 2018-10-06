using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WvsBeta.Center
{
    public class Buddy
    {
        public List<int> Buddies { get; private set; }
        public string Name { get; set; }
        public string Group { get; set; }
        public int CharacterID { get; set; }
        public byte Channel { get; set; }
        public bool Assigned { get; set; }


        public Buddy(int cid)
        {
           this.CharacterID = cid;
        }
        
        public static Buddy AddBuddy(int bid)
        {
            Buddy buddy = new Buddy(bid);
            buddy.Buddies = new List<int>();
            buddy.Buddies.Add(bid);
            return buddy;
        }
        

        public bool IsOnline
        {
            get
            {
                return this.Channel > 0;
            }
        }
    }
}
