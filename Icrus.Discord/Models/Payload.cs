using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Icrus.Discord.Models
{

    public class DiscordPayload
    {
        [JsonPropertyName("op")]
        public DiscordOpcode Opcode { get; set; }

        [JsonPropertyName("d")]
        public IPayloadData Data { get; set; }

        [JsonPropertyName("s")]
        public int? Sequence { get; set; }

        [JsonPropertyName("t")]
        public string EventName { get; set; }

        public T GetData<T>() where T : IPayloadData
        {
            return (T)Data;
        }

        internal static DiscordPayload Deserialize(string json)
        {
            JsonDocument document = JsonDocument.Parse(json);
            bool isValid = document.RootElement.TryGetProperty("op", out _);

            if (!isValid) throw new InvalidOperationException("Not a valid Discord Payload");

            DiscordPayload result = new()
            {
                Opcode = (DiscordOpcode)document.RootElement.GetProperty("op").GetInt32()
            };

            if (document.RootElement.TryGetProperty("s", out JsonElement data))
            {
                try
                {
                    if (data.TryGetInt32(out int s))
                    {
                        result.Sequence = s;
                    }
                }
                catch (InvalidOperationException)
                {

                }
                
            }

            if (document.RootElement.TryGetProperty("t", out data))
            {
                try
                {
                    result.EventName = data.GetString();
                }
                catch(InvalidOperationException)
                {

                }
            }

            if(document.RootElement.TryGetProperty("d", out data))
            {
                string dataJson = data.GetRawText();

                if (!string.IsNullOrEmpty(dataJson))
                {
                    switch (result.Opcode)
                    {
                        case DiscordOpcode.Hello:
                            result.Data = JsonSerializer.Deserialize<GatewayHello>(dataJson);
                            break;
                    }
                }
               
            }

            return result;

        }
    }
    
    public enum DiscordOpcode
    {
        Dispatch,
        Heartbeat,
        Identify,
        PresenceUpdate,
        VoiceStateUpdate,
        Blank,
        Resume,
        Reconnect,
        RequestGuildMemebers,
        InvalidSession,
        Hello,
        HeartbearACK,
    }

    public interface IPayloadData
    {

    }

    public class GatewayHello : IPayloadData
    {
        [JsonPropertyName("heartbeat_interval")]
        public int HeartbeatInterval { get; set; }
    }

    public class DiscordMessage
    {
        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("tts")]
        public bool Tts { get; set; }

        [JsonPropertyName("embeds")]
        public List<DiscordEmbedd> Embeds { get; set; }
    }

    public class DiscordEmbedd
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }
    }

    public class OpCodeHello
    {

    }

    public class OpCodeData
    {
        [JsonPropertyName("heartbeat_interval")]
        public int HeartBeatInterval { get; set; }
    }

    public class IdentifyData : IPayloadData
    {
        [JsonPropertyName("token")]
        public string Token { get; set; }

        [JsonPropertyName("intents")]
        public DiscordIntent Intents { get; set; }

        [JsonPropertyName("properties")]
        public ConnectionProperties Properties { get; set; }

        [JsonPropertyName("compress")]
        public bool? Compress { get; set; }

        [JsonPropertyName("large_threshold")]
        public int? LargeThreshold { get; set; }

        [JsonPropertyName("shard")]
        public int[] Shard { get; set; }

        [JsonPropertyName("")]
        public UpdatePresence UpdatePresence { get; set; }
    }

    public class UpdatePresence
    {
        [JsonPropertyName("since")]
        public int? Since { get; set; }

        [JsonPropertyName("activities")]
        public List<DiscordActivity> Activities { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("afk")]
        public bool IsAfk { get; set; }
    }

    public class ConnectionProperties
    {
        [JsonPropertyName("os")]
        public string OperatingSystem { get; set; }

        [JsonPropertyName("browser")]
        public string Browser { get; set; }

        [JsonPropertyName("device")]
        public string Device { get; set; }

    }

    public class DiscordActivity
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public ActivityType Type { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("created_at")]
        public int CreatedAt { get; set; }

        [JsonPropertyName("application_id")]
        public Snowflake AppId { get; set; }

        [JsonPropertyName("details")]
        public string Details { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; }

        [JsonPropertyName("emoji")]
        public ActivityEmoji Emoji { get; set; }

        [JsonPropertyName("party")]
        public ActivityParty Party { get; set; }

        [JsonPropertyName("assets")]
        public ActivityAssets Assets { get; set; }

        [JsonPropertyName("secrets")]
        public ActivitySecrets Secrets { get; set; }

        [JsonPropertyName("instance")]
        public bool? Instance { get; set; }

        [JsonPropertyName("flags")]
        public int? Flags { get; set; }

        [JsonPropertyName("button")]
        public List<DiscordButton> Buttons { get; set; }
    }

    public class Snowflake
    {

    }

    public class ActivityEmoji
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("id")]
        public Snowflake Id { get; set; }

        [JsonPropertyName("animated")]
        public bool? Animated { get; set; }
    }

    public class ActivityParty
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("size")]
        public int[] Size { get; set; }
    }

    public class ActivityAssets
    {
        [JsonPropertyName("large_image")]
        public string LargeImage { get; set; }

        [JsonPropertyName("large_text")]
        public string LargeText { get; set; }

        [JsonPropertyName("small_image")]
        public string SmallImage { get; set; }

        [JsonPropertyName("small_text")]
        public string SmallText { get; set; }
    }

    public class ActivitySecrets
    {
        [JsonPropertyName("join")]
        public string Join { get; set; }

        [JsonPropertyName("spectate")]
        public string Spectate { get; set; }

        [JsonPropertyName("match")]
        public string Match { get; set; }
    }

    public class DiscordButton
    {
        [JsonPropertyName("label")]
        public string Label { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }
    }

    public enum ActivityType
    {
        Game,
        Streaming,
        Listening,
        Watching,
        Custom,
        Competing
    }

    public enum DiscordIntent
    {
        Guild = 1<<0,
        GuildMembers = 1<<1,
        GuildBans = 1<<2,
        GuildEmojis =1<<3,
        GuildIntergrations = 1<<4,
        GuildWebhooks = 1<<5,
        GuildInvites = 1<<6,
        GuildVoiceStates = 1<<7,
        GuildPresences = 1<<8,
        GuildMessages = 1<<9,
        GuildMessageReactions = 1<<10,
        GuildMessageTyping = 1<<11,
        DirectMessages = 1<<12,
        DirectMessageReactions = 1<<13
    }
}
