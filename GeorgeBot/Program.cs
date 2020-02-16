﻿using System;
using System.Threading;

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
            if (!dataHandler.GetGuilds())
                throw new Exception("Something went wrong, probably with connecting to Discord.");

            _quitEvent.WaitOne();

            discordHandler.client.StopAsync();
        }

        internal static void Kill() => _quitEvent.Set();
    }
}
