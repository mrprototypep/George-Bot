using System;
using System.Threading;
using System.Threading.Tasks;

namespace George
{
    public class Program
    {
        internal static DiscordHandler discordHandler = null;

        private static GeorgeDataHandler dataHandler = null;

        private static ManualResetEvent _quitEvent = new ManualResetEvent(false);

        public static void Main(string[] args)
        {
            Console.CancelKeyPress += (sender, args) =>
            {
                _quitEvent.Set();
                args.Cancel = true;
            };

            //Create DiscordHandler object
            Console.WriteLine("Creating DiscordHandler...");
            discordHandler = new DiscordHandler();

            //Connect to Discord API
            Console.WriteLine("Connecting to Discord...");
            try
            {
                discordHandler.Connect();
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                Console.WriteLine($"Failed to connect to Discord - {ex.Message}");
                return;
            }

            //Create GeorgeDataHandler object
            Console.WriteLine("Initializing George...");
            dataHandler = new GeorgeDataHandler();
            discordHandler.client.MessageReceived += dataHandler.OnMessageReceivedEvent;

            //Populate guilds
            Console.WriteLine("Collecting server information...");
            dataHandler.GetGuilds();

            _quitEvent.WaitOne();

            discordHandler.client.StopAsync();
        }

        internal static void Kill() => _quitEvent.Set();
    }
}
