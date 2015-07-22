using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace TwixelEmotes
{
    public class SubscriberEmote
    {
        public string Code { get; private set; }
        public long Id { get; private set; }

        public SubscriberEmote(string code, long id)
        {
            Code = code;
            Id = id;
        }
    }
}
