using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Configuration;
using Substrate.NetApi;

namespace Substrate.Automation.Modules
{
    // Must use InteractionModuleBase<SocketInteractionContext> for the InteractionService to auto-register the commands
    public class NodeModule : InteractionModuleBase<SocketInteractionContext>
    {
        private static Logger _logger;

        public InteractionService Commands { get; set; }

        private string _url;

        private SubstrateClient _client;

        public NodeModule(ConsoleLogger logger, IConfigurationRoot config)
        {
            _logger = logger;

            _url = "wss://" + config["node:url"];
            _client = new SubstrateClient(new Uri(_url), Substrate.NetApi.Model.Extrinsics.ChargeTransactionPayment.Default());
        }

        // Basic slash command. [SlashCommand("name", "description")]
        // Similar to text command creation, and their respective attributes
        [SlashCommand("node-block", "get block hash")]
        public async Task NodeBlockAsync(
            [Summary(description: "mention the user")] bool mention = true)
        {
            // New LogMessage created to pass desired info to the console using the existing Discord.Net LogMessage parameters
            await _logger.Log(new LogMessage(LogSeverity.Info, "NodeModule : NodeBlockAsync", $"User: {Context.User.Username}, Command: node-block", null));

            await RespondAsync($"Connecting to node {_url}, please wait ...");

            await _client.ConnectAsync();

            var blockHash = await _client.Chain.GetBlockHashAsync();

            await _client.CloseAsync();

            // Respond to the user
            await FollowupAsync($"The current block hash is {blockHash.Value}", null, ephemeral: true);
        }
    }
}