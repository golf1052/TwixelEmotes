using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwixelEmotes
{
    internal static class JsonCache
    {
        internal static string globalString;
        internal static DateTime? globalDate;
        internal static string subscriberString;
        internal static DateTime? subscriberDate;
        internal static string setsString;
        internal static DateTime? setsDate;
        internal static string imagesString;
        internal static DateTime? imagesDate;

        static JsonCache()
        {
            globalString = null;
            globalDate = null;
            subscriberString = null;
            subscriberDate = null;
            setsString = null;
            setsDate = null;
            imagesString = null;
            imagesDate = null;
        }
    }
}
