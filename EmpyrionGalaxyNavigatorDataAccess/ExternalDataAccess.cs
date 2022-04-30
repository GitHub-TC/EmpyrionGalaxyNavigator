using Eleon.Modding;
using EmpyrionGalaxyNavigator;
using EmpyrionNetAPITools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EmpyrionGalaxyNavigatorDataAccess
{
    public class ExternalDataAccess : IMod
    {
        public ConfigurationManager<Configuration> Configuration { get; set; }

        public IModApi ModAPI { get; set; }

        public IDictionary<string, Func<IEntity, object[], object>> ScriptExternalDataAccess { get; }
        public ExternalDataAccess()
        {
            ScriptExternalDataAccess = new Dictionary<string, Func<IEntity, object[], object>>()
            {
                ["Navigation"] = (entity, args) => entity?.Structure?.Pilot?.Id > 0 ?         Navigation(entity) : null,
                ["MaxWarp"   ] = (entity, args) => entity?.Structure?.Pilot?.Id > 0 ? (object)MaxWarp   (entity) : null,
            };
        }

        public void Init(IModApi modAPI)
        {
            ModAPI = modAPI;
            ModAPI.Log($"Init: {GetType().FullName}");

            ConfigurationManager<Configuration>.Log = ModAPI.Log;
            LoadConfiguration();
        }

        public void Shutdown()
        {
            ModAPI.Log($"Shutdown: {GetType().FullName}");
        }

        private void LoadConfiguration()
        {
            Configuration = new ConfigurationManager<Configuration>() { ConfigFilename = Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location), "Configuration.json") };
            Configuration.Load();
        }

        private int MaxWarp(IEntity entity)
            => Configuration?.Current?.Player?.FirstOrDefault(P => P.PlayerId == entity.Structure?.Pilot?.Id)?.Distance ?? 30;

        private PlayerTarget Navigation(IEntity entity)
            => Configuration?.Current?.NavigationTargets?.FirstOrDefault(P => P.Value.Id == entity.Structure?.Pilot?.Id).Value;

    }
}
