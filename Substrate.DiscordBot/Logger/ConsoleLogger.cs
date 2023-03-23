using Discord;
using Serilog.Events;

namespace Substrate.Automation
{
    public class ConsoleLogger : Logger
    {
        // Override Log method from ILogger, passing message to LogToConsoleAsync()
        public override async Task Log(LogMessage message)
        {
            // Using Task.Run() in case there are any long running actions, to prevent blocking gateway
            _ = Task.Run(() => LogToConsoleAsync(this, message));
        }

        private async Task LogToConsoleAsync<T>(T logger, LogMessage message) where T : ILogger
        {
            var severity = message.Severity switch
            {
                LogSeverity.Critical => LogEventLevel.Fatal,
                LogSeverity.Error => LogEventLevel.Error,
                LogSeverity.Warning => LogEventLevel.Warning,
                LogSeverity.Info => LogEventLevel.Information,
                LogSeverity.Verbose => LogEventLevel.Verbose,
                LogSeverity.Debug => LogEventLevel.Debug,
                _ => LogEventLevel.Information
            };

            Serilog.Log.Write(severity, message.Exception, "[{Source}] {Message} - {Guid}", message.Source, message.Message, _guid);

            await Task.CompletedTask;
        }
    }
}