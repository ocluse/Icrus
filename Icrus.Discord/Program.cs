using System;
using System.Threading.Tasks;
using Thismaker.Aba.Client.Core;

namespace Icrus.Discord
{
    class Program
    {
        const string apiEndpoint = "api/v8";

        const string baseAddress = "https://discord.com";
        static async Task Main(string[] args)
        {
            DiscordIcrus app = new AbaClientBuilder<DiscordIcrus>()
                .WithApiEndpoint(apiEndpoint)
                .WithBaseAddress(baseAddress)
                .AsSingleton()
                .Make()
                .Build();

            await app.StartAsync();
        }
    }
}
