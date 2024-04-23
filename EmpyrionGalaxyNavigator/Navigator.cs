using Eleon.Modding;
using EmpyrionNetAPIAccess;
using EmpyrionNetAPIDefinitions;
using EmpyrionNetAPITools;
using EmpyrionNetAPITools.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            SaveGameDBAccess.Log = Log;
            DictionaryExtensions.Log = Log;
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

                GalaxyMap = new GalaxyMap{ GalaxyAutoUpdateMinutes = Configuration.Current.GalaxyAutoUpdateMinutes };
                GalaxyMap.ReadDbData(Path.Combine(EmpyrionConfiguration.SaveGamePath, "global.db"));

                ChatCommands.Add(new ChatCommand(@"nav help",               (I, A) => DisplayHelp    (I.playerId), "display help"));
                ChatCommands.Add(new ChatCommand(@"nav stop",               (I, A) => StopNavigation (I.playerId), "stops navigation"));
                ChatCommands.Add(new ChatCommand(@"nav updategalaxy",       (I, A) => UpdateGalayMap (I.playerId), "force update the galaxy db"));
                ChatCommands.Add(new ChatCommand(@"nav togglemsg",          (I, A) => ToggleMessages (I.playerId), "switch the messages on/off"));
                ChatCommands.Add(new ChatCommand(@"nav setwarp (?<LY>.*)",  (I, A) => SetWarpDistance(I.playerId, A), "set the warp distance for navigation to (LY)"));
                ChatCommands.Add(new ChatCommand(@"nav (?<target>.*)",      (I, A) => StartNavigation(I.playerId, A), "start a navigation to (target)"));

                Event_Player_ChangedPlayfield += Navigator_Event_Player_ChangedPlayfield;

                TaskTools.Intervall(Configuration.Current.MessageLoopMS, () => { try { CheckPlayerNavMessages().Wait(); } catch { } });
            }
            catch (Exception Error)
            {
                Log($"**EmpyrionGalaxyNavigator Error: {Error} {string.Join(" ", Environment.GetCommandLineArgs())}", LogLevel.Error);
            }
        }

        private async Task ToggleMessages(int playerId)
        {
            var P = await Request_Player_Info(playerId.ToId());
            var playerInfo = Configuration.Current.Player.SingleOrDefault(p => p.PlayerId == playerId);
            if (playerInfo == null) Configuration.Current.Player.Add(playerInfo = new PlayerSettings { HideMessages = true });
            else                    playerInfo.HideMessages = !playerInfo.HideMessages;

            Configuration.Save();

            InformPlayer(playerId, $"Messages for route navigation: {(playerInfo.HideMessages ? "off" : "on")}");
        }

        private async Task UpdateGalayMap(int playerId)
        {
            var P = await Request_Player_Info(playerId.ToId());
            
            GalaxyMap.ForceUpdateFromDb();

            await ShowDialog(playerId, P,
                "Update Galay Map",
                $"{GalaxyMap.SolarSystemNavMap.Nodes.Count} known systems and {GalaxyMap.PlayfieldInSolarSystem.Count} planets reading took {GalaxyMap.GalaxyReadTime.ElapsedMilliseconds / 1000:0.0}s");
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

            var currentPlayfield = player.playfield;
            int sunPlayfield = currentPlayfield.IndexOf(" [Sun ");
            if(sunPlayfield > 0) currentPlayfield = currentPlayfield.Substring(0, sunPlayfield);

            if (currentPlayfield == route.CurrentLocation && (DateTime.Now - route.LastMessage).TotalMilliseconds < Configuration.Current.MessageLoopMS) return;

            var playerInfo = Configuration.Current.Player.SingleOrDefault(p => p.PlayerId == player.entityId);
            if (playerInfo == null) Configuration.Current.Player.Add(playerInfo = new PlayerSettings());

            if (currentPlayfield == route.Target)
            {
                GalaxyMap.DbAccess.ClearPathMarkers(player.entityId);

                if(!playerInfo.HideMessages) InformPlayer(player.entityId, $"Congratulation you have reached '{route.NextLocation}' {(string.IsNullOrEmpty(route.Alias) ? "" : $" now fly to '{route.Alias}'")}");

                Configuration.Current.NavigationTargets.TryRemove(player.steamId, out _);
                Configuration.Save();
                return;
            }

            if (currentPlayfield != route.CurrentLocation || route.CurrentLocation == route.NextLocation)
            {
                var currentRoute      = route;
                var newRoute          = GalaxyMap.Navigate(currentPlayfield, currentRoute.Target, MaxTravelDistance(player.entityId) * Const.SectorsPerLY);

                route = new PlayerTarget()
                {
                    Id              = player.entityId,
                    Name            = player.playerName,
                    CurrentLocation = currentPlayfield,
                    NextLocation    = newRoute.First().Name,
                    LastMessage     = DateTime.Now,
                    Route           = newRoute,
                    Target          = currentRoute.Target,
                    Alias           = currentRoute.Alias
                };

                GalaxyMap.DbAccess.InsertBookmarks(newRoute.Take(1), player.factionId, player.entityId, GameAPI.Game_GetTickTime());
            }

            if (!playerInfo.HideMessages) InformPlayer(player.entityId, $"Please travel from '{currentPlayfield}' to '{route.NextLocation}'");

            route.LastMessage = DateTime.Now;
            Configuration.Current.NavigationTargets.AddOrUpdate(player.steamId, route, (K, D) => route);
            Configuration.Save();
        }

        private double MaxTravelDistance(int playerId) 
            => Configuration?.Current?.Player?.SingleOrDefault(p => p.PlayerId == playerId)?.Distance ?? 30;

        private async Task SetWarpDistance(int playerId, Dictionary<string, string> arguments)
        {
            var P = await Request_Player_Info(playerId.ToId());
            var playerInfo = Configuration.Current.Player.SingleOrDefault(p => p.PlayerId == playerId);
            if (playerInfo == null) Configuration.Current.Player.Add(playerInfo = new PlayerSettings());

            var dist = int.TryParse(arguments["LY"]?.Trim(), out var ly) ? ly : 30;
            playerInfo.PlayerId = playerId;
            playerInfo.Name     = P.playerName;
            playerInfo.Distance = dist;

            Configuration.Save();

            InformPlayer(playerId, $"Set warp distance to {dist} LY for route navigation.");
        }

        private async Task StartNavigation(int playerId, Dictionary<string, string> arguments)
        {
            GalaxyMap.UpdateFromDb();

            var P = await Request_Player_Info(playerId.ToId());
            var target = arguments["target"]?.Trim();
            var alias = Configuration.Current.Aliases.FirstOrDefault(A => A.Alias == target);
            if (alias != null) target = alias.PlayfieldName;

            if (!GalaxyMap.Exists(target))
            {
                // Force Update if not found for second try
                GalaxyMap.ForceUpdateFromDb();
            }

            if (!GalaxyMap.Exists(target))
            {
                InformPlayer(playerId, $"Sorry, no target '{target}' found");
                return;
            }

            target = GalaxyMap.RealName(target);

            var maxTravelDistance = MaxTravelDistance(playerId);
            var navigateCalcTime = Stopwatch.StartNew();
            var route = GalaxyMap.Navigate(P.playfield, target, maxTravelDistance * Const.SectorsPerLY);
            navigateCalcTime.Stop();

            if (route.Count <= 1)
            {
                // Force Update if not found for second try
                GalaxyMap.ForceUpdateFromDb();

                navigateCalcTime = Stopwatch.StartNew();
                route = GalaxyMap.Navigate(P.playfield, target, maxTravelDistance * Const.SectorsPerLY);
                navigateCalcTime.Stop();
            }

            if (route.Count <= 1)
            {
                InformPlayer(playerId, $"Sorry, no route from '{P.playfield}' to '{target}{(alias == null ? "" : $" / {alias.Alias}")}' found");
                return;
            }

            var answer = await ShowDialog(playerId, P,
                $"Travel from '[c][00ff00]{P.playfield}[-][/c]' to '[c][00ff00]{target}{(alias == null ? "" : $" / {alias.Alias}")}[-][/c]'",
                $"{GalaxyMap.SolarSystemNavMap.Nodes.Count} known systems and {GalaxyMap.PlayfieldInSolarSystem.Count} planets reading took {GalaxyMap.GalaxyReadTime.ElapsedMilliseconds / 1000:0.0}s navigate took {navigateCalcTime.ElapsedMilliseconds / 1000:0.0}s\n" +
                $"Distance: [c][ff00ff]{(int)route.Aggregate((double)0, (D, T) => D + T.Distance / Const.SectorsPerLY)}[-][/c] LY with max [c][ff00ff]{maxTravelDistance}[-][/c] LY warp capacity\n{route.Aggregate("", (N, T) => $"{N}\n{T}")}{(alias == null ? "" : $"\n{alias.Alias}")}", "Yes", "No");
            if (answer.Id != P.entityId || answer.Value != 0)
            {
                MessagePlayer(playerId, $"Navigation canceled from '{P.playfield}' to '{target}{(alias == null ? "" : $" / {alias.Alias}")}'", MessagePriorityType.Alarm);
                return;
            }

            var playerTarget = new PlayerTarget() {
                Id              = P.entityId,
                Name            = P.playerName,
                CurrentLocation = P.playfield,
                NextLocation    = route.First().Name,
                Route           = route,
                LastMessage     = DateTime.Now,
                Target          = target,
                Alias           = alias?.Alias 
            };

            Configuration.Current.NavigationTargets.AddOrUpdate(P.steamId, playerTarget, (K, D) => playerTarget);
            Configuration.Save();

            GalaxyMap.DbAccess.InsertBookmarks(route.Take(1), P.factionId, P.entityId, GameAPI.Game_GetTickTime());

            MessagePlayer(playerId, $"Navigation started from '{P.playfield}' to '{target}{(alias == null ? "" : $" / {alias.Alias}")}' next target is {playerTarget.NextLocation}", MessagePriorityType.Alarm);
        }

        private async Task StopNavigation(int playerId)
        {
            var P = await Request_Player_Info(playerId.ToId());
            Configuration.Current.NavigationTargets.TryRemove(P.steamId, out _);
            Configuration.Save();

            GalaxyMap.DbAccess.ClearPathMarkers(playerId);
        }

        private async Task DisplayHelp(int playerId)
        {
            Log($"Configuration:{Configuration} Current:{Configuration?.Current} ConfigFilename:{Configuration?.ConfigFilename}", LogLevel.Debug);
            Log($"GalaxyMap:{GalaxyMap} SolarSystemNavMap:{GalaxyMap?.SolarSystemNavMap} Nodes:{GalaxyMap?.SolarSystemNavMap?.Nodes?.Count} PlayfieldInSolarSystem:{GalaxyMap?.PlayfieldInSolarSystem?.Count} GalaxyReadTime:{GalaxyMap?.GalaxyReadTime?.ElapsedMilliseconds}", LogLevel.Debug);

            var P = await Request_Player_Info(playerId.ToId());
            var playerInfo = Configuration.Current.Player?.SingleOrDefault(p => p.PlayerId == playerId);
            bool currentTargetFound = Configuration.Current.NavigationTargets.TryGetValue(P.steamId, out var currentTarget);
            Log($"Player:{P?.playerName} PlayerInfo:{playerInfo?.Name} MaxTravelDistance:{MaxTravelDistance(playerId)} TargetFound:{currentTargetFound}:{currentTarget?.CurrentLocation} Route:{currentTarget?.Route?.Count}", LogLevel.Debug);

            await DisplayHelp(playerId,
                $"{GalaxyMap.SolarSystemNavMap.Nodes.Count} known systems and {GalaxyMap.PlayfieldInSolarSystem.Count} planets reading took {GalaxyMap.GalaxyReadTime.ElapsedMilliseconds / 1000:0.0}s\n" +
                $"Player warp limit: {MaxTravelDistance(playerId)} LY display nav messages {(playerInfo?.HideMessages == true ? "off" : "on")}\n" +
                (currentTargetFound
                    ? $"Route: '{P.playfield}' -> '{currentTarget.Target}{(string.IsNullOrEmpty(currentTarget.Alias) ? "" : $" / {currentTarget.Alias}")}'" + currentTarget.Route?.Aggregate("", (r, t) => $"{r}\n{t}") 
                    : ""
                ));
        }

        private void LoadConfiguration()
        {
            ConfigurationManager<Configuration>.Log = Log;
            Configuration = new ConfigurationManager<Configuration>() { ConfigFilename = Path.Combine(EmpyrionConfiguration.SaveGameModPath, "Configuration.json") };
            Configuration.CreateDefaults = (C) => C.Aliases.Add(new AliasName() { PlayfieldName = "Playfieldname", Alias = "Alias" });

            Configuration.Load();
            Configuration.Save();
        }
    }
}
