using System.Globalization;

namespace WvsBeta.Common
{
    public class Utils
    {
        public static long ConvertNameToID(string pName)
        {
            if (pName[pName.Length - 1] == 'g')
            {
                pName = pName.Remove(pName.Length - 4);
            }
            return long.Parse(pName, NumberStyles.Integer);
        }
        
    }
}
