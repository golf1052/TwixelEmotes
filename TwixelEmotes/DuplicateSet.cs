using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
