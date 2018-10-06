using System;

namespace WvsBeta.Database
{
    public partial class DataBasePatcher
    {
        public class DataBasePatchException : Exception
        {
            public string Query { get; set; }
            public DataBasePatchException(string message, string query, Exception innerException) : base(message, innerException)
            {
                Query = query;
            }
        }

    }
}
