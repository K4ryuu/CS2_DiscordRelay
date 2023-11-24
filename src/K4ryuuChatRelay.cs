using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Utils;

namespace K4ryuuDiscordRelay
{
    [MinimumApiVersion(5)]
    public class DiscordRelayPlugin : BasePlugin
    {
        public override string ModuleName => "Discord Relay";
        public override string ModuleVersion => "1.0.0";
        public override string ModuleAuthor => "K4ryuu";

        public override void Load(bool hotReload)
        {
            new CFG().CheckConfig(ModuleDirectory);

            RegisterEventHandler<EventPlayerChat>((@event, info) =>
            {
                if (!CFG.config.MessageRelay)
                    return HookResult.Continue;

                CCSPlayerController player = new CCSPlayerController(NativeAPI.GetEntityFromIndex(@event.Userid));
                if (player == null || !player.IsValid || player.IsBot || @event.Text == null)
                    return HookResult.Continue;

                string team = "ALL";
                if (@event.Teamonly)
                {
                    switch ((CsTeam)player.TeamNum)
                    {
                        case CsTeam.Terrorist:
                            team = "TERRORIST";
                            break;
                        case CsTeam.CounterTerrorist:
                            team = "COUNTER-TERRORIST";
                            break;
                        case CsTeam.Spectator:
                            team = "SPECTATOR";
                            break;
                        default:
                            team = "NONE";
                            break;
                    }
                }

                _ = SendWebhookMessageAsEmbed(player.PlayerName, @event.Text, player.SteamID);

                return HookResult.Continue;
            });
        }

        public async Task SendWebhookMessageAsEmbed(string playerName, string message, ulong steamID)
        {
            using (var httpClient = new HttpClient())
            {
                var embed = new
                {
                    title = playerName,
                    description = $"{message}\n[Steam Profile](https://steamcommunity.com/profiles/{steamID})",
                    color = 16711680,
                    footer = new
                    {
                        text = $"{CFG.config.ServerName}"
                    }
                };

                var payload = new
                {
                    embeds = new[] { embed }
                };

                var jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(CFG.config.MessageWebhook, content);

                if (!response.IsSuccessStatusCode)
                {
                    Log($"Failed to send message to Discord webhook. Status code: {response.StatusCode}");
                }
            }
        }

        public void Log(string message)
        {
            string logFile = Path.Join(ModuleDirectory, "logs.txt");
            using (StreamWriter writer = File.AppendText(logFile))
            {
                writer.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + message);
            }

            Console.WriteLine(message);
        }
    }
}
