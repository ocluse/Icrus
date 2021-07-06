using Icrus.Discord.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Thismaker.Aba.Client.Core;
using Thismaker.Core.Utilities;

namespace Icrus.Discord
{
    public class DiscordIcrus : CoreClient<DiscordIcrus>
    {
        const string authToken = "ODU3MjU4MjY1MzEwMDAzMjMw.YNM9-Q.JeAJW0wqgtkqLSahHcAULm4pXn4";

        private ClientWebSocket _ws;

        #region Base Implements

        public override async Task StartAsync()
        {
            Console.WriteLine("Icrus has started");
            await Run();
        }

        public override Task StopAsync()
        {
            throw new NotImplementedException();
        }

        protected override async Task ReadyHttpClientForRequestAsync(bool secured)
        {
            if (secured)
            {
                HttpClient.DefaultRequestHeaders.Authorization
                    = new AuthenticationHeaderValue("Bot", authToken);
            }
            else
            {
                await base.ReadyHttpClientForRequestAsync(secured);
            }
        }

        protected override T Deserialize<T>(string args)
        {
            return JsonSerializer.Deserialize<T>(args, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }); 
        }

        protected override Task RenewAccessTokenAsync()
        {
            throw new NotImplementedException();
        }

        protected override string Serialize<T>(T args)
        {
            return JsonSerializer.Serialize(args, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
        }

        #endregion

        #region Run

        private string _channelId;

        public async Task Run()
        {
            while (true)
            {
                string input = Console.ReadLine();

                if (input.StartsWith("fetch-user "))
                {
                    string id = input["fetch-user ".Length..];
                    await FetchUser(id);
                }
                else if(input.StartsWith("farm-users "))
                {
                    string path = input["farm-users ".Length..];

                    Console.Write("Enter the ID to ignore: ");
                    string ignoredId = Console.ReadLine();

                    await FarmUsersData(path, ignoredId);
                }
                else if (input.StartsWith("send "))
                {
                    if (string.IsNullOrEmpty(_channelId))
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("You must set a channel id first with command set-channel");
                        Console.ResetColor();
                        continue;
                    }
                    string messageContent = input["send ".Length..];

                    await CreateMessage(messageContent);
                }
                else if (input.StartsWith("set-channel "))
                {
                    _channelId = input["set-channel ".Length..];
                }
                else if (input == "connect")
                {
                    await ConnectAsync();
                }
                
            }
        }

        private async Task CreateMessage(string messageContent)
        {
            DiscordMessage message = new()
            {
                Content = messageContent,
                Tts = false
            };

            try
            {
                string requestUri = $"channels/{_channelId}/messages";
                HttpResponseMessage response = await ApiPostSimpleAsync<DiscordMessage, HttpResponseMessage>
                    (requestUri, message, deserializeResult:false);

                if (!response.IsSuccessStatusCode)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"ERROR: Request failed! Status Code: {response.StatusCode} Reason Phrase: {response.ReasonPhrase}");
                    Console.ResetColor();
                }

            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed to send request. Check your connectivity status");
                Console.ResetColor();
            }
        }

        private async Task FarmUsersData(string path, string ignoredId)
        {
            string messagesPath = IOUtility.CombinePath(path, "messages");

            if (!Directory.Exists(messagesPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("That is not a valid directory");
                return;
            }

            string[] channelFolders = Directory.GetDirectories(messagesPath);

            foreach(string channelFolder in channelFolders)
            {
                string channelJsonPath = IOUtility.CombinePath(channelFolder, "channel.json");

                if (!File.Exists(channelJsonPath))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Failed to find JSON data for: {channelJsonPath}");
                    continue;
                }

                string channelJson = File.ReadAllText(channelJsonPath);
                JsonDocument channel = JsonDocument.Parse(channelJson);

                int channelType = channel.RootElement.GetProperty("type").GetInt32();

                if(channelType != 1)
                {
                    continue;
                }

                var enumerator = channel.RootElement.GetProperty("recipients").EnumerateArray();

                foreach(var val in enumerator)
                {
                    string userId = val.GetString();

                    if(ignoredId == userId)
                    {
                        continue;
                    }
                    await FetchUser(userId);
                    await Task.Delay(1000);
                }
            }

        }

        private async Task FetchUser(string id)
        {
            try
            {

                var request = $"users/{id}";
                HttpResponseMessage response = await ApiGetAsync(request);

                string content = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    DiscordUser user = Deserialize<DiscordUser>(content);
                    Console.WriteLine($"Username: {user.Username}#{user.Discriminator}");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        Console.WriteLine("User Not Found");
                    }
                    else
                    {
                        Console.WriteLine($"ERROR: Request failed! Status Code: {response.StatusCode} Reason Phrase: {response.ReasonPhrase}");
                    }
                    
                    Console.ResetColor();
                }

                
            }
            catch (HttpRequestException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed to send request. Check your connectivity status");
                Console.ResetColor();
            }
        }

        private async Task ConnectAsync()
        {
            string url = "wss://gateway.discord.gg/?v=8&encoding=json";
            _ws = new ClientWebSocket();
            await _ws.ConnectAsync(new Uri(url), default);

            _ = WsReceiveAsync();

            await IdentifyAsync();
        }

        private async Task IdentifyAsync()
        {
            DiscordActivity defaultActivity = new()
            {
                Details = "It's such a joke",
                State = "With Your Life",
                Name = "A Name",
                Type = ActivityType.Game
            };

            IdentifyData data = new()
            {
                Token = authToken,
                Intents = DiscordIntent.GuildMessages,
                Properties = new()
                {
                    OperatingSystem = "windows",
                    Browser = "Icrus.Discord",
                    Device = "Icrus.Discord"
                },
                UpdatePresence = new()
                {
                    Activities = new List<DiscordActivity> { defaultActivity },
                    IsAfk = false,
                    Status = "online"
                }
            };

            DiscordPayload payload = new()
            {
                Opcode = DiscordOpcode.Identify,
                Data = data,
            };

            await WsSendAsync(payload);
        }

        private async Task WsReceiveAsync()
        {
            while (true)
            {
                try
                {
                    byte[] buffer = new byte[8192];
                    WebSocketReceiveResult result = await _ws.ReceiveAsync(buffer, default);
                    Array.Resize(ref buffer, result.Count);

                    string payloadJson = buffer.GetString<UTF8Encoding>();
                    try
                    {
                        DiscordPayload payload = DiscordPayload.Deserialize(payloadJson);

                        if (payload.Opcode == DiscordOpcode.Hello)
                        {
                            await OnGatewayHello(payload);
                        }
                    }
                    catch(Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Error: {ex.Message}");
                        Console.WriteLine(payloadJson);
                        Console.ResetColor();
                    }
                   
                }
                catch(Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: {ex.Message}");
                    Console.ResetColor();
                }
                
            }
            
        }

        private async Task WsSendAsync(DiscordPayload payload)
        {
            try
            {
                string json = JsonSerializer.Serialize(payload);
                await WsSendAsync(json);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            
        }

        private async Task WsSendAsync(string payload)
        {
            try
            {
                byte[] data = payload.GetBytes<UTF8Encoding>();
                await WsSendAsync(data);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private async Task WsSendAsync(byte[] payload)
        {
            await _ws.SendAsync(payload, WebSocketMessageType.Text, true, default);
        }

        public async Task WsSendAsync<T>(T payload)
        {try
            {
                string json = JsonSerializer.Serialize(payload);
                await WsSendAsync(json);
            }
            catch (Exception ex) { Console.WriteLine($"Error: {ex.Message}"); }
        }

        int? heartbeatSequence = 0;
        
        private async Task OnGatewayHello(DiscordPayload payload)
        {
            int heartbeatInterval = payload.GetData<GatewayHello>().HeartbeatInterval;

            await Task.Delay(2000);

            while (true)
            {
                await Task.Delay(heartbeatInterval);

                Heartbeat heartbeat = new()
                {
                    Opcode = DiscordOpcode.Heartbeat,
                    SequenceNumber = heartbeatSequence
                };

                await WsSendAsync(heartbeat);
            }
        }

        private async Task OnHeartbeatAck()
        {

        }

        class Heartbeat
        {
            [JsonPropertyName("op")]
            public DiscordOpcode Opcode { get; set; }

            [JsonPropertyName("d")]
            public int? SequenceNumber { get; set; }
        }

        #endregion
    }

    
}
