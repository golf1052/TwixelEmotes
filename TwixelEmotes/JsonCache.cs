using System;

namespace TwixelEmotes
{
    internal static class JsonCache
    {
        internal static string globalString;
        internal static DateTime? globalServerDate;
        internal static DateTime? globalLocalDate;
        internal static string subscriberString;
        internal static DateTime? subscriberServerDate;
        internal static DateTime? subscriberLocalDate;
        internal static string setsString;
        internal static DateTime? setsServerDate;
        internal static DateTime? setsLocalDate;
        internal static string imagesString;
        internal static DateTime? imagesServerDate;
        internal static DateTime? imagesLocalDate;
        internal static string basic0String;
        internal static string basic33String;
        internal static string basic42String;
        internal static DateTime? basicLocalDate;

        static JsonCache()
        {
            globalString = null;
            globalServerDate = null;
            globalLocalDate = null;
            subscriberString = null;
            subscriberServerDate = null;
            subscriberLocalDate = null;
            setsString = null;
            setsServerDate = null;
            setsLocalDate = null;
            imagesString = null;
            imagesServerDate = null;
            imagesLocalDate = null;
            basic0String = null;
            basic33String = null;
            basic42String = null;
            basicLocalDate = null;
        }
    }
}
