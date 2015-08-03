using System.Collections.Generic;

namespace TwixelEmotes
{
    public class DuplicateSet
    {
        public string ChannelName { get; private set; }
        public List<long> Sets { get; private set; }

        public DuplicateSet(string channelName)
        {
            ChannelName = channelName;
            Sets = new List<long>();
        }
    }
}
