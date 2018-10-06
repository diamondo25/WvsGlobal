namespace WvsBeta.Game
{
    public interface IFieldObj
    {
        Map Field { get; }

        bool IsShownTo(IFieldObj Object);
    }
}
