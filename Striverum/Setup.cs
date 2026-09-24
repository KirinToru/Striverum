using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Striverum
{
    public static class Setup
    {
        public static string GetMD5Checksum(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "");
                }
            }
        }

        public static bool Generic(string exe, string projectName, string defaultPath, string otherExe = null, string steamId = null, bool epic = false)
        {
            // Get install path from registry
            if (steamId != null && !epic)
            {
                try
                {
                    var key = Registry.LocalMachine.OpenSubKey($@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App {steamId}");
                    if (!String.IsNullOrEmpty(key.GetValue("InstallLocation") as string))
                        defaultPath = $"{key.GetValue("InstallLocation") as string}{Global.s}{exe}";
                }
                catch (Exception)
                {
                }
            }
            if (!File.Exists(defaultPath))
            {
                if (!epic)
                    Global.logger.WriteLine($"Couldn't find install path in registry, select path to exe instead", LoggerType.Warning);
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.DefaultExt = ".exe";
                dialog.Filter = otherExe == null ? $"Executable Files ({exe})|{exe}" 
                    : $"Executable Files ({exe};{otherExe})|{exe};{otherExe}";
                dialog.Title = otherExe == null ? $"Select {exe} from your Steam Install folder"
                    : $"Select {exe} from your Steam Install folder or {otherExe} from your Epic Games install folder";
                dialog.Multiselect = false;
                dialog.InitialDirectory = Global.assemblyLocation;
                dialog.ShowDialog();
                if (!String.IsNullOrEmpty(dialog.FileName)
                    && (Path.GetFileName(dialog.FileName).Equals(exe, StringComparison.InvariantCultureIgnoreCase)
                    || (otherExe != null && Path.GetFileName(dialog.FileName).Equals(otherExe, StringComparison.InvariantCultureIgnoreCase))))
                    defaultPath = dialog.FileName;
                else if (!String.IsNullOrEmpty(dialog.FileName))
                {
                    Global.logger.WriteLine($"Invalid .exe chosen", LoggerType.Error);
                    return false;
                }
                else
                    return false;
            }
            var parent = Path.GetDirectoryName(defaultPath);
            var ModsFolder = $"{parent}{Global.s}{projectName}{Global.s}Content{Global.s}Paks{Global.s}~mods";
            Directory.CreateDirectory(ModsFolder);
            Global.config.Configs[Global.config.CurrentGame].ModsFolder = ModsFolder;
            Global.config.Configs[Global.config.CurrentGame].Launcher = defaultPath;



            Global.UpdateConfig();
            Global.logger.WriteLine($"Setup completed for {Global.config.CurrentGame}!", LoggerType.Info);
            return true;
        }

    }
}
