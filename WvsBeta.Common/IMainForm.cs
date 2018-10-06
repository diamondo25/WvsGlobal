using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WvsBeta.Common
{
    public interface IMainForm
    {
        void LogAppend(string pFormat, params object[] pParams);
        void LogDebug(string pFormat, params object[] pParams);
        void LogToFile(string what);
        void ChangeLoad(bool up);
        void Shutdown();
    }
}
