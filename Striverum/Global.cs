using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Striverum
{
    public static class Global
    {
        public static Config config;
        public static Logger logger;
        public static char s = Path.DirectorySeparatorChar;
        public static string assemblyLocation = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        public static List<string> games;
        public static ObservableCollection<String> LoadoutItems;
        public static ObservableCollection<Mod> ModList;
        public static string GetCurrentModDirectory()
        {
            if (config?.Configs != null && config.Configs.ContainsKey(config.CurrentGame))
            {
                var custom = config.Configs[config.CurrentGame].CustomModsFolder;
                if (!string.IsNullOrEmpty(custom) && Directory.Exists(custom))
                {
                    return custom;
                }
            }
            return $@"{assemblyLocation}{s}Mods{s}{config?.CurrentGame ?? "Guilty Gear -Strive-"}";
        }
        public static void UpdateConfig()
        {
            if (config == null) return;
            config.CurrentGame = "Guilty Gear -Strive-";

            if (config.Configs == null)
            {
                config.Configs = new()
                {
                    { config.CurrentGame, new() }
                };
            }

            if (!config.Configs.ContainsKey(config.CurrentGame))
            {
                config.Configs[config.CurrentGame] = new();
            }

            var gameConfig = config.Configs[config.CurrentGame];
            if (gameConfig.Loadouts == null)
            {
                gameConfig.Loadouts = new();
            }

            if (string.IsNullOrEmpty(gameConfig.CurrentLoadout))
            {
                gameConfig.CurrentLoadout = "Default";
            }

            if (ModList != null)
            {
                gameConfig.Loadouts[gameConfig.CurrentLoadout] = new ObservableCollection<Mod>(ModList.Where(m => !m.isGroupHeader));
            }

            try
            {
                string configString = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText($@"{assemblyLocation}{s}Config.json", configString);
            }
            catch (Exception e)
            {
                logger?.WriteLine($"Couldn't write Config.json ({e.Message})", LoggerType.Error);
            }
        }
    }
}
