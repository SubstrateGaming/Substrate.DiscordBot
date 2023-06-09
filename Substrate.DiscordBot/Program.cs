﻿using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Yaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Substrate.Automation
{
    public class Program
    {
        private DiscordSocketClient _client;

        // Program entry point
        public static Task Main(string[] args) => new Program().MainAsync();

        public async Task MainAsync()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            var config = new ConfigurationBuilder()
                // this will be used more later on
                .SetBasePath(AppContext.BaseDirectory)
                // I chose using YML files for my config data as I am familiar with them
                .AddYamlFile("config.yml")
                .Build();

            using IHost host = Host.CreateDefaultBuilder()
                .ConfigureServices((_, services) =>
                    services

                        // Add the configuration to the registered services
                        .AddSingleton(config)

                        // Add the DiscordSocketClient, along with specifying the GatewayIntents and user caching
                        .AddSingleton(x => new DiscordSocketClient(new DiscordSocketConfig
                        {
                            GatewayIntents = Discord.GatewayIntents.AllUnprivileged,
                            AlwaysDownloadUsers = true,
                            LogLevel = Discord.LogSeverity.Debug
                        }))

                        // Adding console logging
                        .AddTransient<ConsoleLogger>()

                        // Used for slash commands and their registration with Discord
                        .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))

                        // Required to subscribe to the various client events used in conjunction with Interactions
                        .AddSingleton<InteractionHandler>()

                        // Adding database context
                        //.AddDbContext<DiscordBotDbContext>(
                        //  options => options.UseSqlite($"Data Source={dbPath}"))

                        ).Build();

            await RunAsync(host);
        }

        public async Task RunAsync(IHost host)
        {
            using IServiceScope serviceScope = host.Services.CreateScope();

            IServiceProvider provider = serviceScope.ServiceProvider;

            var commands = provider.GetRequiredService<InteractionService>();

            _client = provider.GetRequiredService<DiscordSocketClient>();

            var config = provider.GetRequiredService<IConfigurationRoot>();

            await provider.GetRequiredService<InteractionHandler>().InitializeAsync();

            // Subscribe to client log events
            _client.Log += _ => provider.GetRequiredService<ConsoleLogger>().Log(_);

            // Subscribe to slash command log events
            commands.Log += _ => provider.GetRequiredService<ConsoleLogger>().Log(_);

            _client.Ready += async () =>
            {
                // If running the bot with DEBUG flag, register all commands to guild specified in config
                if (IsDebug())
                {
                    // Id of the test guild can be provided from the Configuration object
                    await commands.RegisterCommandsToGuildAsync(UInt64.Parse(config["testGuild"]), true);
                }
                else
                {
                    // If not debug, register commands globally
                    await commands.RegisterCommandsGloballyAsync(true);
                }
            };
            await _client.LoginAsync(Discord.TokenType.Bot, config["tokens:discord"]);
            await _client.StartAsync();

            await Task.Delay(-1);
        }

        private static bool IsDebug()
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }
    }
}