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
        private static ManualResetEvent _connectionEvent = new ManualResetEvent(false);

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

            discordHandler.client.Connected += async () => {
                Console.WriteLine("Connected to Discord");
                _connectionEvent.Set();
            };

            discordHandler.client.Disconnected += async (ex) =>
            {
                Console.WriteLine($"Disconnected from Discord - {ex.Message}");
                Console.WriteLine("Exiting...");
                return;
            };

            //Connect to Discord API
            Console.WriteLine("Connecting to Discord...");
            Task connection = discordHandler.ConnectAsync();
            connection.Wait(10000); //10 second timeout
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
            if (discordHandler.client.ConnectionState != Discord.ConnectionState.Connected)
                Console.WriteLine("Waiting for connection before proceeding...");
            _connectionEvent.WaitOne(20000); //20 second timeout

            Console.WriteLine("Collecting server information...");
            if (!dataHandler.GetGuilds())
                throw new Exception("Something went wrong, probably with connecting to Discord.");

            Console.WriteLine($"Initialization finished in {DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime()}");

            _quitEvent.WaitOne();

            Console.WriteLine("Terminating...");

            discordHandler.client.StopAsync();
        }

        internal static void Kill() => _quitEvent.Set();
    }
}
