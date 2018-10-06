namespace WvsBeta.Game
{
    public class NpcLife : LifeWrapper, IFieldObj
    {
        public Map Field { get; }
        public uint SpawnID { get; set; }

        public NpcLife(Life life) : base(life)
        {
        }

        public bool IsShownTo(IFieldObj Object) => true;
    }
}