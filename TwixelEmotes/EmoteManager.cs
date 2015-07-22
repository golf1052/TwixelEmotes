using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Diagnostics;

namespace TwixelEmotes
{
    public static class EmoteManager
    {
        private const string BaseUrl = "http://twitchemotes.com/api_cache/v2/";

        private static Dictionary<long, Emote> emotes;
        private static Dictionary<long, GlobalEmote> globalEmotes;
        private static Dictionary<string, GlobalEmote> globalEmotesByCode;

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

        public static bool Initialized { get; private set; }
 
        static EmoteManager()
        {
            Initialized = false;
            emotes = new Dictionary<long, Emote>();
            globalEmotes = new Dictionary<long, GlobalEmote>();
            globalEmotesByCode = new Dictionary<string, GlobalEmote>();
            EmotesByCode = new Dictionary<string, Emote>();
            ChannelsByName = new Dictionary<string, Channel>();
            ChannelsBySet = new Dictionary<long, Channel>();
            ChannelNameBySet = new Dictionary<long, string>();
            SetByChannelName = new Dictionary<string, long>();
            DuplicateSets = new Dictionary<string, DuplicateSet>();
            globalChannel = null;
            AutoRefreshTime = TimeSpan.FromMinutes(30);
        }

        public static async Task Initialize()
        {
            globalChannel = new Channel();
            globalChannel.Title = "--global--";
            globalChannel.Link = null;
            globalChannel.Description = null;
            globalChannel.Id = "--global--";
            globalChannel.Badge = null;
            globalChannel.Set = 0;

            await RetrieveGlobalEmotes();
            await RetrieveImages();
            await RetrieveSets();
            await RetrieveSubscriberEmotes();
            foreach (KeyValuePair<long, GlobalEmote> globalEmote in globalEmotes)
            {
                globalChannel.Emotes.Add(emotes[globalEmote.Key]);
            }
            ChannelsByName.Add("--global--", globalChannel);
            ChannelsBySet.Add(0, globalChannel);
            Initialized = true;
        }

        public static async Task LoadGlobalEmotesFromStream(Stream globalEmotesStream)
        {
            using (StreamReader reader = new StreamReader(globalEmotesStream))
            {
                JsonCache.globalString = await reader.ReadToEndAsync();
                globalEmotes.Clear();
                globalEmotesByCode.Clear();
                LoadGlobalEmotes(JsonCache.globalString);
            }
        }

        public static async Task<EmoteResponse<Dictionary<string, GlobalEmote>>> RetrieveGlobalEmotes()
        {
            if (JsonCache.globalString == null || (JsonCache.globalDate.HasValue && DateTime.UtcNow > JsonCache.globalDate.Value + AutoRefreshTime))
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
                return LoadGlobalEmotes(responseString);
            }
            else
            {
                return new EmoteResponse<Dictionary<string, GlobalEmote>>(JsonCache.globalDate.Value, globalEmotesByCode);
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
            JsonCache.globalDate = DateTime.Parse((string)responseObject["meta"]["generated_at"]);
            return new EmoteResponse<Dictionary<string, GlobalEmote>>(JsonCache.globalDate.Value, globalEmotesByCode);
        }

        public static async Task LoadSubscriberEmotesFromStream(Stream subscriberEmotesStream)
        {
            using (StreamReader reader = new StreamReader(subscriberEmotesStream))
            {
                JsonCache.subscriberString = await reader.ReadToEndAsync();
                ChannelsByName.Clear();
                ChannelsBySet.Clear();
                LoadSubscriberEmotes(JsonCache.subscriberString);
            }
        }

        public static async Task<EmoteResponse<Dictionary<string, Channel>>> RetrieveSubscriberEmotes()
        {
            if (JsonCache.subscriberString == null || (JsonCache.subscriberDate.HasValue && DateTime.UtcNow > JsonCache.subscriberDate.Value + AutoRefreshTime))
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
                return LoadSubscriberEmotes(responseString);
            }
            else
            {
                return new EmoteResponse<Dictionary<string, Channel>>(JsonCache.subscriberDate.Value, ChannelsByName);
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
            JsonCache.subscriberDate = DateTime.Parse((string)responseObject["meta"]["generated_at"]);
            return new EmoteResponse<Dictionary<string, Channel>>(JsonCache.subscriberDate.Value, ChannelsByName);
        }

        public static async Task LoadSetsFromStream(Stream setsStream)
        {
            using (StreamReader reader = new StreamReader(setsStream))
            {
                JsonCache.setsString = await reader.ReadToEndAsync();
                ChannelNameBySet.Clear();
                DuplicateSets.Clear();
                SetByChannelName.Clear();
                LoadSets(JsonCache.setsString);
            }
        }

        public static async Task<EmoteResponse<Dictionary<long, string>>> RetrieveSets()
        {
            if (JsonCache.setsString == null || (JsonCache.setsDate.HasValue && DateTime.UtcNow > JsonCache.setsDate.Value + AutoRefreshTime))
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
                return LoadSets(responseString);
            }
            else
            {
                return new EmoteResponse<Dictionary<long, string>>(JsonCache.setsDate.Value, ChannelNameBySet);
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
            JsonCache.setsDate = DateTime.Parse((string)responseObject["meta"]["generated_at"]);
            return new EmoteResponse<Dictionary<long, string>>(JsonCache.setsDate.Value, ChannelNameBySet);
        }

        public static async Task LoadImagesFromStream(Stream imagesStream)
        {
            using (StreamReader reader = new StreamReader(imagesStream))
            {
                JsonCache.imagesString = await reader.ReadToEndAsync();
                emotes.Clear();
                EmotesByCode.Clear();
                LoadImages(JsonCache.imagesString);
            }
        }

        public static async Task<EmoteResponse<Dictionary<long, Emote>>> RetrieveImages()
        {
            if (JsonCache.imagesString == null || (JsonCache.imagesDate.HasValue && DateTime.UtcNow > JsonCache.imagesDate.Value + AutoRefreshTime))
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
                return LoadImages(responseString);
            }
            else
            {
                return new EmoteResponse<Dictionary<long, Emote>>(JsonCache.imagesDate.Value, emotes);
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
            JsonCache.imagesDate = DateTime.Parse((string)responseObject["meta"]["generated_at"]);
            return new EmoteResponse<Dictionary<long, Emote>>(JsonCache.imagesDate.Value, emotes);
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
