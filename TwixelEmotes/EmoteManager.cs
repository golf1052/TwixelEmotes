using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TwixelEmotes
{
    public static class EmoteManager
    {
        internal static string Protocol
        {
            get
            {
                if (UseHttps)
                {
                    return "https://";
                }
                else
                {
                    return "http://";
                }
            }
        }

        private static string BaseUrl
        {
            get
            {
                return Protocol + "twitchemotes.com/api_cache/v2/";
            }
        }

        private static Dictionary<long, Emote> emotes;
        private static Dictionary<long, GlobalEmote> globalEmotes;
        private static Dictionary<string, GlobalEmote> globalEmotesByCode;
        private static Dictionary<long, Emote> basicEmotes;

        public static Dictionary<long, Emote> EmotesById
        {
            get
            {
                return emotes;
            }
        }

        public static Dictionary<string, Emote> EmotesByCode { get; private set; }

        public static Dictionary<string, Channel> ChannelsByName { get; private set; }

        public static Dictionary<long, Channel> ChannelsBySet { get; private set; }

        public static Dictionary<long, string> ChannelNameBySet { get; private set; }

        public static Dictionary<string, long> SetByChannelName { get; private set; }

        public static Dictionary<string, DuplicateSet> DuplicateSets { get; private set; }

        private static Channel globalChannel;

        public static TimeSpan AutoRefreshTime { get; set; }

        public static bool UseHttps { get; set; }

        public static bool Initialized { get; private set; }
 
        static EmoteManager()
        {
            Initialized = false;
            emotes = new Dictionary<long, Emote>();
            globalEmotes = new Dictionary<long, GlobalEmote>();
            basicEmotes = new Dictionary<long, Emote>();
            globalEmotesByCode = new Dictionary<string, GlobalEmote>();
            EmotesByCode = new Dictionary<string, Emote>();
            ChannelsByName = new Dictionary<string, Channel>();
            ChannelsBySet = new Dictionary<long, Channel>();
            ChannelNameBySet = new Dictionary<long, string>();
            SetByChannelName = new Dictionary<string, long>();
            DuplicateSets = new Dictionary<string, DuplicateSet>();
            globalChannel = null;
            AutoRefreshTime = TimeSpan.FromMinutes(30);
            UseHttps = true;
        }

        public static async Task Initialize(EmoteDataCache dataCache = null)
        {
            globalChannel = new Channel();
            globalChannel.Title = "--global--";
            globalChannel.Link = null;
            globalChannel.Description = null;
            globalChannel.Id = "--global--";
            globalChannel.Badge = null;
            globalChannel.Set = 0;

            Channel turboChannel33 = new Channel();
            turboChannel33.Title = "--twitch-turbo--";
            turboChannel33.Link = null;
            turboChannel33.Description = null;
            turboChannel33.Id = "--twitch-turbo--";
            turboChannel33.Badge = null;
            turboChannel33.Set = 33;

            Channel turboChannel42 = new Channel();
            turboChannel42.Title = "--twitch-turbo--";
            turboChannel42.Link = null;
            turboChannel42.Description = null;
            turboChannel42.Id = "--twitch-turbo--";
            turboChannel42.Badge = null;
            turboChannel42.Set = 42;

            if (dataCache == null)
            {
                await RetrieveGlobalEmotes();
                await RetrieveImages();
                await RetrieveSets();
                await RetrieveSubscriberEmotes();
                await RetrieveBasicEmotes();
            }
            else
            {
                await LoadGlobalEmotesFromString(dataCache.GlobalString, dataCache.GlobalTime);
                await LoadImagesFromString(dataCache.ImagesString, dataCache.ImagesTime);
                await LoadSetsFromString(dataCache.SetsString, dataCache.SetsTime);
                await LoadSubscriberEmotesFromString(dataCache.SubscriberString, dataCache.SubscriberTime);
                await LoadBasicEmotesFromString(dataCache.Basic0String, dataCache.Basic33String, dataCache.Basic42String, dataCache.BasicTime);
            }

            foreach (KeyValuePair<long, GlobalEmote> globalEmote in globalEmotes)
            {
                globalChannel.Emotes.Add(emotes[globalEmote.Key]);
            }
            foreach (KeyValuePair<long, Emote> basicEmote in basicEmotes)
            {
                if (basicEmote.Value.Set == 0)
                {
                    globalChannel.Emotes.Add(basicEmote.Value);
                }
                else if (basicEmote.Value.Set == 33)
                {
                    turboChannel33.Emotes.Add(basicEmote.Value);
                }
                else if (basicEmote.Value.Set == 42)
                {
                    turboChannel42.Emotes.Add(basicEmote.Value);
                }
                emotes.Add(basicEmote.Key, basicEmote.Value);
            }
            ChannelsByName.Add("--global--", globalChannel);
            ChannelsBySet.Add(0, globalChannel);
            Channel superTurbo = new Channel();
            superTurbo.Title = turboChannel33.Title;
            superTurbo.Link = turboChannel33.Link;
            superTurbo.Description = turboChannel33.Description;
            superTurbo.Id = turboChannel33.Id;
            superTurbo.Badge = turboChannel33.Badge;
            superTurbo.Set = turboChannel33.Set;
            superTurbo.Emotes.AddRange(turboChannel33.Emotes);
            superTurbo.Emotes.AddRange(turboChannel42.Emotes);
            ChannelsByName.Add("--twitch-turbo--", superTurbo);
            ChannelsBySet.Add(33, turboChannel33);
            ChannelsBySet.Add(42, turboChannel42);
            Initialized = true;
        }

        public static EmoteDataCache GetDataCache()
        {
            return new EmoteDataCache(JsonCache.globalString, JsonCache.globalLocalDate.Value.ToString("o"),
                JsonCache.subscriberString, JsonCache.subscriberLocalDate.Value.ToString("o"),
                JsonCache.setsString, JsonCache.setsLocalDate.Value.ToString("o"),
                JsonCache.imagesString, JsonCache.imagesLocalDate.Value.ToString("o"),
                JsonCache.basic0String, JsonCache.basic33String, JsonCache.basic42String, JsonCache.basicLocalDate.Value.ToString("o"));
        }

        public static async Task LoadGlobalEmotesFromString(string globalEmotesString, DateTime? globalTime)
        {
            JsonCache.globalString = globalEmotesString;
            JsonCache.globalLocalDate = globalTime;
            globalEmotes.Clear();
            globalEmotesByCode.Clear();
            await RetrieveGlobalEmotes();
        }

        public static async Task<EmoteResponse<Dictionary<string, GlobalEmote>>> RetrieveGlobalEmotes()
        {
            if (JsonCache.globalString == null ||
                (JsonCache.globalServerDate.HasValue &&
                JsonCache.globalLocalDate.HasValue &&
                AutoRefreshTime != TimeSpan.Zero &&
                DateTime.UtcNow > JsonCache.globalServerDate.Value + AutoRefreshTime &&
                DateTime.UtcNow > JsonCache.globalLocalDate.Value + AutoRefreshTime))
            {
                globalEmotes.Clear();
                globalEmotesByCode.Clear();
                Uri uri = new Uri(BaseUrl + "global.json");
                string responseString;
                try
                {
                    responseString = await GetWebData(uri);
                }
                catch (Exception ex)
                {
                    throw;
                }
                JsonCache.globalString = responseString;
                JsonCache.globalLocalDate = DateTime.UtcNow;
                return LoadGlobalEmotes(responseString);
            }
            else
            {
                if (!JsonCache.globalServerDate.HasValue)
                {
                    return LoadGlobalEmotes(JsonCache.globalString);
                }
                return new EmoteResponse<Dictionary<string, GlobalEmote>>(JsonCache.globalServerDate.Value, globalEmotesByCode);
            }
        }

        private static EmoteResponse<Dictionary<string, GlobalEmote>> LoadGlobalEmotes(string responseString)
        {
            JObject responseObject = JObject.Parse(responseString);
            Dictionary<string, JObject> emotesDict = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(((JObject)responseObject["emotes"]).ToString());
            foreach (KeyValuePair<string, JObject> emotesPair in emotesDict)
            {
                string description = (string)emotesPair.Value["description"];
                long id = long.Parse((string)emotesPair.Value["image_id"]);
                GlobalEmote globalEmote = new GlobalEmote(id, emotesPair.Key, description);
                globalEmotes.Add(id, globalEmote);
                globalEmotesByCode.Add(emotesPair.Key, globalEmote);
            }
            JsonCache.globalServerDate = DateTime.Parse((string)responseObject["meta"]["generated_at"]);
            return new EmoteResponse<Dictionary<string, GlobalEmote>>(JsonCache.globalServerDate.Value, globalEmotesByCode);
        }

        public static async Task LoadSubscriberEmotesFromString(string subscriberEmotesString, DateTime? subscriberTime)
        {
            JsonCache.subscriberString = subscriberEmotesString;
            JsonCache.subscriberLocalDate = subscriberTime;
            ChannelsByName.Clear();
            ChannelsBySet.Clear();
            await RetrieveSubscriberEmotes();
        }

        public static async Task<EmoteResponse<Dictionary<string, Channel>>> RetrieveSubscriberEmotes()
        {
            if (JsonCache.subscriberString == null ||
                (JsonCache.subscriberServerDate.HasValue &&
                JsonCache.subscriberLocalDate.HasValue &&
                AutoRefreshTime != TimeSpan.Zero && 
                DateTime.UtcNow > JsonCache.subscriberServerDate.Value + AutoRefreshTime &&
                DateTime.UtcNow > JsonCache.subscriberLocalDate.Value + AutoRefreshTime))
            {
                ChannelsByName.Clear();
                ChannelsBySet.Clear();
                Uri uri = new Uri(BaseUrl + "subscriber.json");
                string responseString;
                try
                {
                    responseString = await GetWebData(uri);
                }
                catch (Exception ex)
                {
                    throw;
                }
                JsonCache.subscriberString = responseString;
                JsonCache.subscriberLocalDate = DateTime.UtcNow;
                return LoadSubscriberEmotes(responseString);
            }
            else
            {
                if (!JsonCache.subscriberServerDate.HasValue)
                {
                    return LoadSubscriberEmotes(JsonCache.subscriberString);
                }
                return new EmoteResponse<Dictionary<string, Channel>>(JsonCache.subscriberServerDate.Value, ChannelsByName);
            }
        }

        private static EmoteResponse<Dictionary<string, Channel>> LoadSubscriberEmotes(string responseString)
        {
            JObject responseObject = JObject.Parse(responseString);
            Dictionary<string, JObject> channels = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(((JObject)responseObject["channels"]).ToString());
            foreach (KeyValuePair<string, JObject> channelPair in channels)
            {
                Channel channel = new Channel();
                channel.Title = (string)channelPair.Value["title"];
                string link = (string)channelPair.Value["link"];
                if (!string.IsNullOrEmpty(link))
                {
                    channel.Link = new Uri(link);
                }
                channel.Description = (string)channelPair.Value["desc"];
                channel.Id = (string)channelPair.Value["id"];
                channel.Badge = (string)channelPair.Value["badge"];
                channel.Set = long.Parse((string)channelPair.Value["set"]);
                foreach (JObject o in (JArray)channelPair.Value["emotes"])
                {
                    string code = (string)o["code"];
                    long id = long.Parse((string)o["image_id"]);
                    channel.SubscriberEmotes.Add(new SubscriberEmote(code, id));
                    if (emotes.ContainsKey(id))
                    {
                        channel.Emotes.Add(emotes[id]);
                    }
                }
                ChannelsByName.Add(channelPair.Key, channel);
                ChannelsBySet.Add(channel.Set, channel);
            }
            JsonCache.subscriberServerDate = DateTime.Parse((string)responseObject["meta"]["generated_at"]);
            return new EmoteResponse<Dictionary<string, Channel>>(JsonCache.subscriberServerDate.Value, ChannelsByName);
        }

        public static async Task LoadSetsFromString(string setsString, DateTime? setsTime)
        {
            JsonCache.setsString = setsString;
            JsonCache.setsLocalDate = setsTime;
            ChannelNameBySet.Clear();
            DuplicateSets.Clear();
            SetByChannelName.Clear();
            await RetrieveSets();
        }

        public static async Task<EmoteResponse<Dictionary<long, string>>> RetrieveSets()
        {
            if (JsonCache.setsString == null ||
                (JsonCache.setsServerDate.HasValue &&
                JsonCache.setsLocalDate.HasValue &&
                AutoRefreshTime != TimeSpan.Zero &&
                DateTime.UtcNow > JsonCache.setsServerDate.Value + AutoRefreshTime &&
                DateTime.UtcNow > JsonCache.setsLocalDate.Value + AutoRefreshTime))
            {
                ChannelNameBySet.Clear();
                DuplicateSets.Clear();
                SetByChannelName.Clear();
                Uri uri = new Uri(BaseUrl + "sets.json");
                string responseString;
                try
                {
                    responseString = await GetWebData(uri);
                }
                catch (Exception ex)
                {
                    throw;
                }
                JsonCache.setsString = responseString;
                JsonCache.setsLocalDate = DateTime.UtcNow;
                return LoadSets(responseString);
            }
            else
            {
                if (!JsonCache.setsServerDate.HasValue)
                {
                    return LoadSets(JsonCache.setsString);
                }
                return new EmoteResponse<Dictionary<long, string>>(JsonCache.setsServerDate.Value, ChannelNameBySet);
            }
        }

        private static EmoteResponse<Dictionary<long, string>> LoadSets(string responseString)
        {
            JObject responseObject = JObject.Parse(responseString);
            Dictionary<string, string> sets = JsonConvert.DeserializeObject<Dictionary<string, string>>(((JObject)responseObject["sets"]).ToString());
            foreach (KeyValuePair<string, string> set in sets)
            {
                long setId = long.Parse(set.Key);
                ChannelNameBySet.Add(setId, set.Value);
                if (SetByChannelName.ContainsKey(set.Value) || DuplicateSets.ContainsKey(set.Value))
                {
                    if (!DuplicateSets.ContainsKey(set.Value))
                    {
                        DuplicateSets.Add(set.Value, new DuplicateSet(set.Value));
                    }
                    if (SetByChannelName.ContainsKey(set.Value))
                    {
                        DuplicateSets[set.Value].Sets.Add(SetByChannelName[set.Value]);
                        SetByChannelName.Remove(set.Value);
                    }
                    DuplicateSets[set.Value].Sets.Add(setId);
                }
                else
                {
                    SetByChannelName.Add(set.Value, setId);
                }

            }
            JsonCache.setsServerDate = DateTime.Parse((string)responseObject["meta"]["generated_at"]);
            return new EmoteResponse<Dictionary<long, string>>(JsonCache.setsServerDate.Value, ChannelNameBySet);
        }

        public static async Task LoadImagesFromString(string imagesString, DateTime? imagesTime)
        {
            JsonCache.imagesString = imagesString;
            JsonCache.imagesLocalDate = imagesTime;
            emotes.Clear();
            EmotesByCode.Clear();
            await RetrieveImages();
        }

        public static async Task<EmoteResponse<Dictionary<long, Emote>>> RetrieveImages()
        {
            if (JsonCache.imagesString == null || 
                (JsonCache.imagesServerDate.HasValue &&
                JsonCache.imagesLocalDate.HasValue &&
                AutoRefreshTime != TimeSpan.Zero &&
                DateTime.UtcNow > JsonCache.imagesServerDate.Value + AutoRefreshTime &&
                DateTime.UtcNow > JsonCache.imagesLocalDate.Value + AutoRefreshTime))
            {
                emotes.Clear();
                EmotesByCode.Clear();
                Uri uri = new Uri(BaseUrl + "images.json");
                string responseString;
                try
                {
                    responseString = await GetWebData(uri);
                }
                catch (Exception ex)
                {
                    throw;
                }
                JsonCache.imagesString = responseString;
                JsonCache.imagesLocalDate = DateTime.UtcNow;
                return LoadImages(responseString);
            }
            else
            {
                if (!JsonCache.imagesServerDate.HasValue)
                {
                    return LoadImages(JsonCache.imagesString);
                }
                return new EmoteResponse<Dictionary<long, Emote>>(JsonCache.imagesServerDate.Value, emotes);
            }
        }

        private static EmoteResponse<Dictionary<long, Emote>> LoadImages(string responseString)
        {
            JObject responseObject = JObject.Parse(responseString);
            Dictionary<string, JObject> images = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(((JObject)responseObject["images"]).ToString());
            foreach (KeyValuePair<string, JObject> image in images)
            {
                Emote emote = new Emote();
                emote.Id = long.Parse(image.Key);
                emote.Code = (string)image.Value["code"];
                emote.Description = (string)image.Value["description"];
                string set = (string)image.Value["set"];
                if (!string.IsNullOrEmpty(set))
                {
                    emote.Set = long.Parse(set);
                }
                else
                {
                    emote.Set = 0;
                }
                string channel = (string)image.Value["channel"];
                if (!string.IsNullOrEmpty(channel))
                {
                    emote.Channel = channel;
                }
                else
                {
                    emote.Channel = "--global--";
                }
                emotes.Add(emote.Id, emote);
                if (!EmotesByCode.ContainsKey(emote.Code))
                {
                    EmotesByCode.Add(emote.Code, emote);
                }
            }
            JsonCache.imagesServerDate = DateTime.Parse((string)responseObject["meta"]["generated_at"]);
            return new EmoteResponse<Dictionary<long, Emote>>(JsonCache.imagesServerDate.Value, emotes);
        }

        public static async Task LoadBasicEmotesFromString(string basicSet0String, string basicSet33String, string basicSet42String, DateTime? basicTime)
        {
            JsonCache.basic0String = basicSet0String;
            JsonCache.basic33String = basicSet33String;
            JsonCache.basic42String = basicSet42String;
            JsonCache.basicLocalDate = basicTime;
            basicEmotes.Clear();
            await RetrieveBasicEmotes();
        }

        public static async Task<Dictionary<long, Emote>> RetrieveBasicEmotes()
        {
            if (JsonCache.basic0String == null ||
                (JsonCache.basicLocalDate.HasValue &&
                AutoRefreshTime != TimeSpan.Zero &&
                DateTime.UtcNow > JsonCache.basicLocalDate.Value + AutoRefreshTime))
            {
                basicEmotes.Clear();
                try
                {
                    JsonCache.basic0String = await RetrieveEmoteSet(0);
                }
                catch (Exception ex)
                {
                    throw;
                }
                try
                {
                    JsonCache.basic33String = await RetrieveEmoteSet(33);
                }
                catch (Exception ex)
                {
                    throw;
                }
                try
                {
                    JsonCache.basic42String = await RetrieveEmoteSet(42);
                }
                catch (Exception ex)
                {
                    throw;
                }
                JsonCache.basicLocalDate = DateTime.UtcNow;
                return LoadBasicEmotes(JsonCache.basic0String, JsonCache.basic33String, JsonCache.basic42String);
            }
            else
            {
                return basicEmotes;
            }
        }

        private static Dictionary<long, Emote> LoadBasicEmotes(string set0, string set33, string set42)
        {
            JObject set0Response = JObject.Parse(set0);
            JObject set33Response = JObject.Parse(set33);
            JObject set42Response = JObject.Parse(set42);
            foreach (JObject o in set0Response["emoticon_sets"]["0"])
            {
                long id = long.Parse((string)o["id"]);
                if (id < 15)
                {
                    Emote emote = new Emote();
                    emote.Id = id;
                    emote.Code = UnHtml((string)o["code"]);
                    emote.Description = null;
                    emote.Set = 0;
                    emote.Channel = "--global--";
                    basicEmotes.Add(id, emote);
                }
            }
            foreach (JObject o in set33Response["emoticon_sets"]["33"])
            {
                long id = long.Parse((string)o["id"]);
                Emote emote = new Emote();
                emote.Id = id;
                emote.Code = UnHtml((string)o["code"]);
                emote.Description = null;
                emote.Set = 33;
                emote.Channel = "--twitch-turbo--";
                basicEmotes.Add(id, emote);
            }
            foreach (JObject o in set42Response["emoticon_sets"]["42"])
            {
                long id = long.Parse((string)o["id"]);
                Emote emote = new Emote();
                emote.Id = id;
                emote.Code = UnHtml((string)o["code"]);
                emote.Description = null;
                emote.Set = 42;
                emote.Channel = "--twitch-turbo--";
                basicEmotes.Add(id, emote);
            }
            return basicEmotes;
        }

        private static string UnHtml(string html)
        {
            return html.Replace(@"\&lt\;", "<").Replace(@"\&gt\;", ">");
        }

        private static async Task<string> RetrieveEmoteSet(long set)
        {
            Uri uri = new Uri(Protocol + "api.twitch.tv/kraken/chat/emoticon_images?emotesets=" + set);
            string responseString;
            try
            {
                responseString = await GetWebData(uri);
            }
            catch (Exception ex)
            {
                throw;
            }
            return responseString;
        }

        /// <summary>
        /// Gets a json string from the given url
        /// </summary>
        /// <param name="uri">The url to fetch data from</param>
        /// <returns>The json string from the API</returns>
        /// <remarks>If the status code isn't 200 it will return a string
        /// that is the status code.</remarks>
        static async Task<string> GetWebData(Uri uri)
        {
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(uri);

            string responseString;
            if (response.StatusCode == HttpStatusCode.OK)
            {
                // 200 - OK
                responseString = await response.Content.ReadAsStringAsync();
                return responseString;
            }
            else
            {
                responseString = await response.Content.ReadAsStringAsync();
                throw new Exception(responseString);
            }
        }
    }
}
