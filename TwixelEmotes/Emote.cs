using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwixelEmotes
{
    public class Emote
    {
        public long Id { get; internal set; }
        public string Code { get; internal set; }
        public string Description { get; internal set; }
        public long Set { get; internal set; }
        public string Channel { get; internal set; }
    }
}
