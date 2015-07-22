using System;
using System.Collections.Generic;

namespace TwixelEmotes
{
    public class Channel
    {
        public string Title { get; internal set; }
        public Uri Link { get; internal set; }
        public string Description { get; internal set; }
        public string Id { get; internal set; }
        public string Badge { get; internal set; }
        public long Set { get; internal set; }
        public List<SubscriberEmote> SubscriberEmotes { get; internal set; }
        public List<Emote> Emotes { get; internal set; }

        public Channel()
        {
            SubscriberEmotes = new List<SubscriberEmote>();
            Emotes = new List<Emote>();
        }
    }
}
