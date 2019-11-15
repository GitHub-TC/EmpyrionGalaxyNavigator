using Eleon.Modding;
using EmpyrionNetAPIAccess;
using EmpyrionNetAPIDefinitions;
using EmpyrionNetAPITools;
using EmpyrionNetAPITools.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EmpyrionGalaxyNavigator
{
    public class Navigator : EmpyrionModBase
    {
        public ModGameAPI GameAPI { get; set; }

        public ConfigurationManager<Configuration> Configuration { get; set; }
        GalaxyMap GalaxyMap { get; set; }

        public Navigator()
        {
            EmpyrionConfiguration.ModName = "EmpyrionGalaxyNavigator";
        }

        public override void Initialize(ModGameAPI dediAPI)
        {
            GameAPI = dediAPI;

            try
            {
                Log($"**EmpyrionGalaxyNavigator loaded: {string.Join(" ", Environment.GetCommandLineArgs())}", LogLevel.Message);

                LoadConfiguration();
                LogLevel = Configuration.Current.LogLevel;
                ChatCommandManager.CommandPrefix = Configuration.Current.ChatCommandPrefix;

                GalaxyMap = new GalaxyMap();
                GalaxyMap.ReadSectors(File.ReadAllText(Path.Combine(EmpyrionConfiguration.SaveGamePath, "Sectors", "sectors.yaml")));

                ChatCommands.Add(new ChatCommand(@"nav help",           (I, A) => DisplayHelp    (I.playerId), "display help"));
                ChatCommands.Add(new ChatCommand(@"nav stop",           (I, A) => StopNavigation (I.playerId), "stops navigation"));
                ChatCommands.Add(new ChatCommand(@"nav (?<target>.*)",  (I, A) => StartNavigation(I.playerId, A), "start a navigation to (target)"));

                Event_Player_ChangedPlayfield += Navigator_Event_Player_ChangedPlayfield;

                TaskTools.Intervall(Configuration.Current.MessageLoopMS, () => { try { CheckPlayerNavMessages().Wait(); } catch { } });
            }
            catch (Exception Error)
            {
                Log($"**EmpyrionGalaxyNavigator Error: {Error} {string.Join(" ", Environment.GetCommandLineArgs())}", LogLevel.Error);
            }
        }

        private async Task CheckPlayerNavMessages()
        {
            var players = await Request_Player_List();

            players.list
                .Where(I => Configuration.Current.NavigationTargets.Any(R => R.Value.Id == I))
                .ToList()
                .ForEach(I => { try { Navigator_Event_Player_Info(Request_Player_Info(I.ToId()).Result); } catch { } });
        }

        private void Navigator_Event_Player_ChangedPlayfield(IdPlayfield obj)
        {
            if (Configuration.Current.NavigationTargets.All(R => R.Value.Id != obj.id)) return;

            try{ Navigator_Event_Player_Info(Request_Player_Info(obj.id.ToId()).Result); } catch {}
        }

        private void Navigator_Event_Player_Info(PlayerInfo player)
        {
            if (!Configuration.Current.NavigationTargets.TryGetValue(player.steamId, out var route)) return;
            if (player.playfield == route.CurrentLocation && (DateTime.Now - route.LastMessage).TotalMilliseconds < Configuration.Current.MessageLoopMS) return;

            if(player.playfield == route.Target)
            {
                InformPlayer(player.entityId, $"Congratulation you have reached '{route.NextLocation}' {(string.IsNullOrEmpty(route.Alias) ? "" : $" now fly to '{route.Alias}'")}");

                Configuration.Current.NavigationTargets.TryRemove(player.steamId, out _);
                Configuration.Save();
                return;
            }

            if (player.playfield != route.CurrentLocation || route.CurrentLocation == route.NextLocation)
            {
                var currentRoute = route;
                var newRoute = GalaxyMap.Navigate(player.playfield, currentRoute.Target);

                route = new PlayerTarget()
                {
                    Id              = player.entityId,
                    Name            = player.playerName,
                    CurrentLocation = player.playfield,
                    NextLocation    = newRoute.Skip(1).First().Name,
                    LastMessage     = DateTime.Now,
                    Target          = currentRoute.Target,
                    Alias           = currentRoute.Alias
                };
            }

            InformPlayer(player.entityId, $"Please travel from '{player.playfield}' to '{route.NextLocation}'");

            route.LastMessage = DateTime.Now;
            Configuration.Current.NavigationTargets.AddOrUpdate(player.steamId, route, (K, D) => route);
            Configuration.Save();
        }

        private async Task StartNavigation(int playerId, Dictionary<string, string> arguments)
        {
            var P = await Request_Player_Info(playerId.ToId());
            var target = arguments["target"]?.Trim();
            var alias = Configuration.Current.Aliases.FirstOrDefault(A => A.Alias == target);
            if (alias != null) target = alias.PlayfieldName;

            if (!GalaxyMap.Exists(target))
            {
                InformPlayer(playerId, $"Sorry, no target '{target}' found");
                return;
            }

            var route = GalaxyMap.Navigate(P.playfield, target);

            if(route.Count <= 1)
            {
                InformPlayer(playerId, $"Sorry, no route from '{P.playfield}' to '{target}{(alias == null ? "" : $" / {alias.Alias}")}' found");
                return;
            }

            var answer = await ShowDialog(playerId, P,
                $"Travel from '{P.playfield}' to '{target}{(alias == null ? "" : $" / {alias.Alias}")}'",
                $"Distance: {(int)route.Aggregate((double)0, (D, T) => D + T.Distance / 10)} AU\n{route.Aggregate("", (N, T) => N + "\n" + T.Name)}{(alias == null ? "" : $"\n{alias.Alias}")}", "Yes", "No");
            if (answer.Id != P.entityId || answer.Value != 0)
            {
                MessagePlayer(playerId, $"Navigation canceled from '{P.playfield}' to '{target}{(alias == null ? "" : $" / {alias.Alias}")}'", MessagePriorityType.Alarm);
                return;
            }

            var playerTarget = new PlayerTarget() {
                Id              = P.entityId,
                Name            = P.playerName,
                CurrentLocation = P.playfield,
                NextLocation    = route.Skip(1).First().Name,
                LastMessage     = DateTime.Now,
                Target          = target,
                Alias           = alias?.Alias 
            };

            Configuration.Current.NavigationTargets.AddOrUpdate(P.steamId, playerTarget, (K, D) => playerTarget);
            Configuration.Save();

            MessagePlayer(playerId, $"Navigation started from '{P.playfield}' to '{target}{(alias == null ? "" : $" / {alias.Alias}")}' next target is {playerTarget.NextLocation}", MessagePriorityType.Alarm);
        }

        private async Task StopNavigation(int playerId)
        {
            var P = await Request_Player_Info(playerId.ToId());
            Configuration.Current.NavigationTargets.TryRemove(P.steamId, out _);
            Configuration.Save();
        }

        private async Task DisplayHelp(int playerId)
        {
            var P = await Request_Player_Info(playerId.ToId());
            await DisplayHelp(playerId, Configuration.Current.NavigationTargets.TryGetValue(P.steamId, out var target) ? $"Route: '{P.playfield}' -> '{target.Target}{(string.IsNullOrEmpty(target.Alias) ? "" : $" / {target.Alias}")}'" : null);
        }

        private void LoadConfiguration()
        {
            ConfigurationManager<Configuration>.Log = Log;
            Configuration = new ConfigurationManager<Configuration>() { ConfigFilename = Path.Combine(EmpyrionConfiguration.SaveGameModPath, "Configuration.json") };

            Configuration.Load();
            Configuration.Save();
        }
    }
}
