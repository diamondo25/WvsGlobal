namespace WvsBeta.Game
{
    public class Kite : IFieldObj
    {
        public Character Owner { get; set; }
        public Map Field { get; set; }
        public int ID { get; set; }
        public int ItemID { get; set; }
        public string Message { get; set; }
        public short X { get; set; }
        public short Y { get; set; }
        

        public Kite(Character owner, int id, int itemID, string message, Map field)
        {
            Owner = owner;
            ID = id;
            ItemID = itemID;
            Message = message;
            Field = field;
            X = owner.Position.X;
            Y = (short)(owner.Position.Y - 100);

            Field.Kites.Add(this);
            MapPacket.Kite(Owner, this);
        }

        public bool IsShownTo(IFieldObj Object) => true;

        public void RemoveKiteEffect()
        {
            MapPacket.RemoveKite(Field, this, 0);
            Field.Kites.Remove(this);
        }
    }
}
