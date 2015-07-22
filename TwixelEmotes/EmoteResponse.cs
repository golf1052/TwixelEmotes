using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwixelEmotes
{
    public class EmoteResponse<T>
    {
        public DateTime GeneratedAt { get; private set; }
        public T Response { get; private set; }

        public EmoteResponse(DateTime generatedAt, T response)
        {
            GeneratedAt = generatedAt;
            Response = response;
        }
    }
}
