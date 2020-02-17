using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace George
{
    public class Program
    {
        internal static Guid GUID = Guid.NewGuid();

        internal static DiscordHandler discordHandler = null;

        private static GeorgeDataHandler dataHandler = null;

        private static ManualResetEvent _quitEvent = new ManualResetEvent(false);

        public static void Main(string[] args)
        {
            Console.WriteLine($"Started with GUID {GUID}");

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
            Task connection = discordHandler.ConnectAsync();
            connection.Wait();
            if (connection.Exception != null)
            {
                Console.WriteLine($"Something went wrong - {connection.Exception.Message}");
                return;
            }

            //Create GeorgeDataHandler object
            Console.WriteLine("Initializing George...");
            dataHandler = new GeorgeDataHandler();
            discordHandler.client.MessageReceived += dataHandler.OnMessageReceivedEvent;

            //Populate guilds
            Console.WriteLine("Collecting server information...");
            if (!dataHandler.GetGuilds())
                throw new Exception("Something went wrong, probably with connecting to Discord.");

            Console.WriteLine($"Initialization finished in {DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime()} seconds.");

            _quitEvent.WaitOne();

            Console.WriteLine("Terminating...");

            discordHandler.client.StopAsync();
        }

        internal static void Kill() => _quitEvent.Set();
    }
}
