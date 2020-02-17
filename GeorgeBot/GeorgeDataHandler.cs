using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace George
{
    internal class GeorgeDataHandler
    {
        private bool paused = false;

        private const ulong adminId = 178280670001364993; //ID of whoever the admin is (me, in this case)

        private DiscordHandler discord = null;

        internal static Dictionary<ulong, string> languageByGuild = null;
        private static Dictionary<ulong, List<string>> censoredWordsByGuild = null;
        private static Dictionary<ulong, List<ulong>> censoredUsersByGuild = null;

        internal GeorgeDataHandler()
        {
            discord = Program.discordHandler;

            languageByGuild = new Dictionary<ulong, string>();
            censoredWordsByGuild = new Dictionary<ulong, List<string>>();
            discord.client.JoinedGuild += Client_JoinedGuild;
            discord.client.LeftGuild += Client_LeftGuild;

            censoredUsersByGuild = new Dictionary<ulong, List<ulong>>();
        }

        internal bool GetGuilds()
        {
            foreach (var guild in discord.client.Guilds)
            {
                languageByGuild.Add(guild.Id, "en");
                censoredWordsByGuild.Add(guild.Id, new List<string>());
                censoredUsersByGuild.Add(guild.Id, new List<ulong>());
            }
            return true;
        }

        private async Task Client_LeftGuild(SocketGuild guild) => DeInitGuild(guild.Id);

        private void DeInitGuild(ulong guildId)
        {
            languageByGuild.Remove(guildId);
            censoredWordsByGuild.Remove(guildId);
            censoredUsersByGuild.Remove(guildId);
        }

        private async Task Client_JoinedGuild(SocketGuild guild) => InitGuild(guild.Id);

        private void InitGuild(ulong guildId)
        {
            languageByGuild.Add(guildId, "en");
            censoredWordsByGuild.Add(guildId, new List<string>());
            censoredUsersByGuild.Add(guildId, new List<ulong>());
        }

        private void CensorMessage(SocketMessage msg)
        {
            if (msg.Author.Id == discord.client.CurrentUser.Id)
                return;
            if (DiscordHandler.UserIsAdmin(msg.Author))
                return;

            var guild = (msg.Channel as SocketGuildChannel).Guild;

            if (censoredUsersByGuild.TryGetValue(guild.Id, out List<ulong> bannedUsers)) {
                if (bannedUsers.Contains(msg.Author.Id))
                    msg.DeleteAsync();
            }

            censoredWordsByGuild.TryGetValue(guild.Id, out List<string> bannedWords);

            foreach (string word in bannedWords)
            {
                if (msg.Content.ToLower().Contains(word))
                {
                    msg.DeleteAsync();
                    discord.SendMessageAsync(msg.Channel, $"Message removed due to censorship policy of `{guild.Name}`");
                    break;
                }
            }
        }

        internal async Task OnMessageReceivedEvent(SocketMessage msg)
        {
            string language;
            languageByGuild.TryGetValue(DiscordHandler.GetGuildFromChannel(msg.Channel).Id, out language);

            bool georgeMentioned = false;
            foreach(var user in msg.MentionedUsers)
            {
                if (user.Id == discord.client.CurrentUser.Id)
                    georgeMentioned = true;
            }

            if (paused)
            {
                if (georgeMentioned && msg.Content.ToLower().Contains($"cmd:debug unpause {Program.GUID}"))
                {
                    paused = false;
                    discord.SendMessageAsync(msg.Channel, $"Instance {Program.GUID} unpaused");
                }
                return;
            }

            if (georgeMentioned)
            {
                var text = msg.Content.ToLower();
                if (!text.Contains("cmd:"))
                    CensorMessage(msg);

                while (text.Contains("cmd:"))
                {
                    text = text.Substring(text.IndexOf("cmd:") + 4);
                    string rawCommand = text.Substring(0, text.IndexOf(';') >= 0 ? text.IndexOf(';') : text.Length);

                    string[] command = rawCommand.Split(' ');

                    switch (command[0])
                    {
                        default:
                            discord.SendMessageCode(msg.Channel, "InvalidCommand", language);
                            CensorMessage(msg);
                            break;
                        case "debug":
                            if (msg.Author.Id != adminId)
                            {
                                discord.SendMessageAsync(msg.Channel, "You do not have permission to use debug commands.");
                                break;
                            }
                            switch (command[1])
                            {
                                default:
                                    discord.SendMessageAsync(msg.Channel, "Invalid command, or syntax wrong.");
                                    break;
                                case "pause":
                                    if (Program.GUID.Equals(new Guid(command[2])))
                                    {
                                        paused = true;
                                        discord.SendMessageAsync(msg.Channel, $"Instance {Program.GUID} paused");
                                    }
                                    break;
                                case "grantcontrol":
                                    discord.SendMessageAsync(msg.Channel, "Granted local administrative authority.");
                                    DiscordHandler.hasLocalAdmin = true;
                                    break;
                                case "revokecontrol":
                                    discord.SendMessageAsync(msg.Channel, "Revoked local administrative authority.");
                                    DiscordHandler.hasLocalAdmin = false;
                                    break;
                                case "hi":
                                    discord.SendMessageAsync(msg.Channel, $"`{Program.GUID}` - Version: {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version} - Running from: {Environment.MachineName}");
                                    break;
                                case "guilds":
                                    string guildsString = "Guilds: ";
                                    foreach (ulong id in censoredWordsByGuild.Keys)
                                    {
                                        guildsString += id + ",";
                                    }
                                    discord.SendMessageAsync(msg.Channel, guildsString);
                                    break;
                                case "kill":
                                    discord.SendMessageAsync(msg.Channel, "Attempting soft termination...");
                                    Program.Kill();
                                    break;
                                case "kill_force":
                                    discord.SendMessageAsync(msg.Channel, "Hard terminating. Bye");
                                    Environment.Exit(1);
                                    break;
                            }
                            break;
                        case "censor":
                            if (!DiscordHandler.UserIsAdmin(msg.Author))
                            {
                                discord.SendMessageCode(msg.Channel, "NoPermission", language);
                                break;
                            }
                            string toCensor = "";
                            for (int i = 1; i < command.Length; i++)
                            {
                                if (i != 1)
                                    toCensor += ' ';
                                toCensor += command[i];
                            }

                            if (censoredWordsByGuild.TryGetValue(DiscordHandler.GetGuildFromChannel(msg.Channel).Id, out List<string> bannedWords))
                            {
                                bannedWords.Add(toCensor);
                                discord.SendMessageCode(msg.Channel, "TermAdded", language);
                            }
                            else
                                discord.SendMessageCode(msg.Channel, "ErrorGeneric", language);
                            break;
                        case "uncensor":
                            if (!DiscordHandler.UserIsAdmin(msg.Author))
                            {
                                discord.SendMessageCode(msg.Channel, "NoPermission", language);
                                break;
                            }
                            string toUncensor = "";
                            for (int i = 1; i < command.Length; i++)
                            {
                                if (i != 1)
                                    toUncensor += ' ';
                                toUncensor += command[i];
                            }

                            if (censoredWordsByGuild.TryGetValue(DiscordHandler.GetGuildFromChannel(msg.Channel).Id, out bannedWords))
                            {
                                bannedWords.Remove(toUncensor);
                                discord.SendMessageCode(msg.Channel, "TermRemoved", language);
                            }
                            else
                                discord.SendMessageCode(msg.Channel, "ErrorGeneric", language);
                            break;
                        case "censoruser":
                            if (!DiscordHandler.UserIsAdmin(msg.Author))
                            {
                                discord.SendMessageCode(msg.Channel, "NoPermission", language);
                                break;
                            }

                            if (censoredUsersByGuild.TryGetValue(DiscordHandler.GetGuildFromChannel(msg.Channel).Id, out var bannedUsers))
                            {
                                try
                                {
                                    bannedUsers.Add(ulong.Parse(command[1]));
                                } catch (FormatException)
                                {
                                    discord.SendMessageCode(msg.Channel, "InvalidUserID", language);
                                } catch (IndexOutOfRangeException)
                                {
                                    discord.SendMessageCode(msg.Channel, "InvalidCommand", language);
                                } catch (Exception)
                                {
                                    discord.SendMessageCode(msg.Channel, "ErrorGeneric", language);
                                }
                            }
                            else
                                discord.SendMessageCode(msg.Channel, "ErrorGeneric", language);
                            break;
                        case "uncensoruser":
                            if (!DiscordHandler.UserIsAdmin(msg.Author))
                            {
                                discord.SendMessageCode(msg.Channel, "NoPermission", language);
                                break;
                            }

                            if (censoredUsersByGuild.TryGetValue(DiscordHandler.GetGuildFromChannel(msg.Channel).Id, out bannedUsers))
                            {
                                try
                                {
                                    bannedUsers.Remove(ulong.Parse(command[1]));
                                }
                                catch (FormatException)
                                {
                                    discord.SendMessageCode(msg.Channel, "InvalidUserID", language);
                                }
                                catch (IndexOutOfRangeException)
                                {
                                    discord.SendMessageCode(msg.Channel, "InvalidCommand", language);
                                }
                                catch (Exception)
                                {
                                    discord.SendMessageCode(msg.Channel, "ErrorGeneric", language);
                                }
                            }
                            else
                                discord.SendMessageCode(msg.Channel, "ErrorGeneric", language);
                            break;
                        case "what":
                            if (censoredWordsByGuild.TryGetValue(DiscordHandler.GetGuildFromChannel(msg.Channel).Id, out bannedWords))
                            {
                                string toSay = "Censored: ";
                                foreach (string word in bannedWords)
                                {
                                    toSay += word + ", ";
                                }
                                discord.SendMessageAsync(msg.Channel, toSay);
                            }
                            else
                            {
                                discord.SendMessageCode(msg.Channel, "ErrorGeneric", language);
                            }
                            break;
                        case "help":
                            discord.SendMessageAsync(msg.Channel, "Available commands:\n\n" +
                                "Available to all users:\n" +
                                "`what` - Returns any banned words in the server\n" +
                                "Available to administrators only:\n" +
                                "`(un)censor <term>` - Censors or uncensors a term, may include spaces\n" +
                                "`(un)censoruser <userID>` - Censors or uncensors a user, get a user ID by right clicking their name (with debug mode on Discord enabled)\n" +
                                "`language <languageCode>` - Changes the language to the language code, even if the language isn't supported. Just for fun");
                            break;
                        case "language":
                            if (!DiscordHandler.UserIsAdmin(msg.Author))
                            {
                                discord.SendMessageCode(msg.Channel, "NoPermission", language);
                                break;
                            }
                            languageByGuild[DiscordHandler.GetGuildFromChannel(msg.Channel).Id] = command[1]?.ToLower();
                            discord.SendMessageAsync(msg.Channel, ":white_check_mark:");
                            break;
                    }
                }
            }
            else
                CensorMessage(msg);

            return;
        }
    }
}
