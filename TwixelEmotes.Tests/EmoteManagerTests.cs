using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace TwixelEmotes.Tests
{
    public class EmoteManagerTests
    {
        public EmoteManagerTests()
        {
            if (!EmoteManager.Initialized)
            {
                Task.Run(async () => { await EmoteManager.Initialize(); }).Wait();
            }
        }

        [Fact]
        public void InitializeTest()
        {
            Assert.True(EmoteManager.Initialized);
            Assert.True(EmoteManager.ChannelsBySet.ContainsKey(0));
            Assert.True(EmoteManager.ChannelsByName.ContainsKey("--global--"));
            Emote kappa = EmoteManager.ChannelsByName["--global--"].Emotes.FirstOrDefault(e => e.Code == "Kappa");
            Assert.NotNull(kappa);
            Assert.Equal<long>(25, kappa.Id);
        }

        [Fact]
        public async void RetrieveGlobalEmotesTest()
        {
            EmoteResponse<Dictionary<string, GlobalEmote>> globalEmotes = await EmoteManager.RetrieveGlobalEmotes();
            Assert.Equal("Kappa", globalEmotes.Response["Kappa"].Code);
            Assert.Equal(25, globalEmotes.Response["Kappa"].Id);
        }

        [Fact]
        public async void RetrieveSubscriberEmotesTest()
        {
            EmoteResponse<Dictionary<string, Channel>> subscriberEmotes = await EmoteManager.RetrieveSubscriberEmotes();
            Assert.True(subscriberEmotes.Response.ContainsKey("ongamenet"));
            Assert.Equal("Ongamenet", subscriberEmotes.Response["ongamenet"].Title);
            Assert.Equal<long>(19, subscriberEmotes.Response["ongamenet"].Set);
            SubscriberEmote rotato = subscriberEmotes.Response["ongamenet"].SubscriberEmotes.FirstOrDefault(e => e.Code == "ognTSM");
            Assert.NotNull(rotato);
            Assert.Equal<long>(11872, rotato.Id);

            Assert.True(EmoteManager.ChannelsBySet.ContainsKey(19));
            Assert.Equal("ongamenet", EmoteManager.ChannelsBySet[19].Id);
        }

        [Fact]
        public async void RetrieveSetsTest()
        {
            EmoteResponse<Dictionary<long, string>> sets = await EmoteManager.RetrieveSets();
            Assert.True(sets.Response.ContainsKey(54));
            Assert.Equal("tsm_dyrus", sets.Response[54]);

            Assert.True(EmoteManager.SetByChannelName.ContainsKey("tsm_dyrus"));
            Assert.Equal<long>(54, EmoteManager.SetByChannelName["tsm_dyrus"]);
        }

        [Fact]
        public async void RetrieveImagesTest()
        {
            EmoteResponse<Dictionary<long, Emote>> emotes = await EmoteManager.RetrieveImages();
            Assert.True(emotes.Response.ContainsKey(655));
            Assert.Equal("dyrus1800MICROWAVE", emotes.Response[655].Code);

            Assert.True(EmoteManager.EmotesById.ContainsKey(25));
            Assert.Equal("Kappa", EmoteManager.EmotesById[25].Code);
            Assert.True(EmoteManager.EmotesByCode.ContainsKey("Kappa"));
            Assert.Equal<long>(25, EmoteManager.EmotesByCode["Kappa"].Id);
        }

        [Fact]
        public async void RetrieveBasicEmotesTest()
        {
            Dictionary<long, Emote> basicEmotes = await EmoteManager.RetrieveBasicEmotes();
            Regex regex = new Regex(basicEmotes[12].Code);
            Match match = regex.Match(":-P");
            Assert.True(match.Success);
            Match match2 = regex.Match(":p");
            Assert.True(match2.Success);
        }
    }
}
