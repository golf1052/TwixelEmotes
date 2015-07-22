namespace TwixelEmotes
{
    public class GlobalEmote
    {
        public long Id { get; internal set; }
        public string Code { get; internal set; }
        public string Description { get; internal set; }

        public GlobalEmote(long id, string code, string description)
        {
            Id = id;
            Code = code;
            Description = description;
        }
    }
}
