using System;

namespace Chris.OS.Additions.Utils
{
    public class Tools
    {
        public static int getUnixTime()
        {
            return (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
    }
}
