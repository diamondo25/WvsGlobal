namespace WvsBeta.Common
{
    public class LoopingID
    {
        public int Current { get; private set; }
        public int Minimum { get; private set; }
        public int Maximum { get; private set; }

        public LoopingID()
        {
            Minimum = Current = 1;
            Maximum = int.MaxValue;
        }

        public LoopingID(int min, int max)
        {
            Minimum = Current = min;
            Maximum = max;
        }

        public int NextValue()
        {
            int ret = Current;
            if (Current == Maximum)
            {
                Reset();
            }
            else
            {
                Current++;
            }
            return ret;
        }

        public void Reset() { Reset(Minimum); }
        public void Reset(int val)
        {
            Current = val;
        }
    }
}
