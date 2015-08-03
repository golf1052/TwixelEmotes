using System;

namespace TwixelEmotes
{
    public class Emote
    {
        public long Id { get; internal set; }
        public string Code { get; internal set; }
        public string Description { get; internal set; }
        public long Set { get; internal set; }
        public string Channel { get; internal set; }

        public Uri Small
        {
            get
            {
                return new Uri(EmoteManager.Protocol + "static-cdn.jtvnw.net/emoticons/v1/" + Id + "/1.0");
            }
        }

        public Uri Medium
        {
            get
            {
                return new Uri(EmoteManager.Protocol + "static-cdn.jtvnw.net/emoticons/v1/" + Id + "/2.0");
            }
        }

        public Uri Large
        {
            get
            {
                return new Uri(EmoteManager.Protocol + "static-cdn.jtvnw.net/emoticons/v1/" + Id + "/3.0");
            }
        }
    }
}
