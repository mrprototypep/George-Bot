using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace George
{
    internal class DiscordHandler
    {
        private string _TOKEN = null; //Token for Discord API
        internal static bool hasLocalAdmin = false; //Override for debugging
        private List<MessageCode> messages = null; //List of MessageCode objects, imported from messages.json

        internal DiscordSocketClient client = null;

        internal DiscordHandler()
        {
            _TOKEN = File.ReadAllText("token.txt"); //Token is stored in token.txt file so I could ignore it
            client = new DiscordSocketClient();

            messages = JsonConvert.DeserializeObject<List<MessageCode>>(File.ReadAllText("messages.json"));
        }

        //Returns the guild (server) that a channel is part of
        internal static SocketGuild GetGuildFromChannel(ISocketMessageChannel messageChannel)
        {
            return (messageChannel as SocketGuildChannel).Guild;
        }

        //Returns if user is an administrator, or if hasLocalAdmin is enabled (allows my discord to override admin rules for debugging)
        internal static bool UserIsAdmin(SocketUser socketUser)
        {
            return (socketUser as SocketGuildUser).GuildPermissions.Administrator || hasLocalAdmin;
        }

        //Sends a message to a passed channel.
        internal void SendMessageCode(ISocketMessageChannel channel, string messageCode, string language)
        {
            string message = null;
            foreach (var m in messages)
            {
                if (m.MessageID.Equals(messageCode))
                {
                    message = m.GetMessage(language);
                    break;
                }
            }
            if (message == null)
                message = "Contact administrator - invalid MessageCode passed";

            SendMessageAsync(channel, message);
        }

        //Sends message over channel
        internal async void SendMessageAsync(ISocketMessageChannel channel, string message)
        {
            await channel.SendMessageAsync(message);
        }

        //Attempts to connect to Discord
        internal async Task ConnectAsync()
        {            
             await client.LoginAsync(TokenType.Bot, _TOKEN);
            await client.StartAsync();
        }
    }
}