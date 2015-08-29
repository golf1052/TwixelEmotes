using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwixelEmotes
{
    public class EmoteDataCache
    {
        public string GlobalString { get; internal set; }
        public DateTime? GlobalTime { get; internal set; }
        public string SubscriberString { get; internal set; }
        public DateTime? SubscriberTime { get; internal set; }
        public string SetsString { get; internal set; }
        public DateTime? SetsTime { get; internal set; }
        public string ImagesString { get; internal set; }
        public DateTime? ImagesTime { get; internal set; }
        public string Basic0String { get; internal set; }
        public string Basic33String { get; internal set; }
        public string Basic42String { get; internal set; }
        public DateTime? BasicTime { get; internal set; }

        public EmoteDataCache(string globalString, string globalTime,
            string subscriberString, string subscriberTime,
            string setsString, string setsTime,
            string imagesString, string imagesTime,
            string basic0String, string basic33String, string basic42String, string basicTime)
        {
            GlobalString = globalString;
            try
            {
                GlobalTime = DateTime.Parse(globalTime).ToUniversalTime();
            }
            catch
            {
            }
            SubscriberString = subscriberString;
            try
            {
                SubscriberTime = DateTime.Parse(subscriberTime).ToUniversalTime();
            }
            catch
            {
            }
            SetsString = setsString;
            try
            {
                SetsTime = DateTime.Parse(setsTime).ToUniversalTime();
            }
            catch
            {
            }
            ImagesString = imagesString;
            try
            {
                ImagesTime = DateTime.Parse(imagesTime).ToUniversalTime();
            }
            catch
            {
            }
            Basic0String = basic0String;
            Basic33String = basic33String;
            Basic42String = basic42String;
            try
            {
                BasicTime = DateTime.Parse(basicTime).ToUniversalTime();
            }
            catch
            {
            }
        }
    }
}
