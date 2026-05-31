using System;
using System.IO;
using System.Text.Json;
using EasyLog;
using EasySave.Models;

namespace EasySave.Services
{
    public static class SettingsManager
    {
        private static readonly JsonSerializerOptions Options =
            new JsonSerializerOptions { WriteIndented = true };

        private static string SettingsPath
        {
            get
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.ApplicationData),
                    "EasySave");

                Directory.CreateDirectory(dir);

                return Path.Combine(dir, "settings.json");
            }
        }

        public static AppSettings Load()
        {
            if (!File.Exists(SettingsPath))
                return new AppSettings();

            try
            {
                string json = File.ReadAllText(SettingsPath);

                return JsonSerializer.Deserialize<AppSettings>(json)
                       ?? new AppSettings();
            }
            catch (JsonException)
            {
                Console.WriteLine(
                    "[WARNING] settings.json corrupted — using defaults.");

                return new AppSettings();
            }
        }

        public static void Save(AppSettings settings)
        {
            File.WriteAllText(SettingsPath,
                JsonSerializer.Serialize(settings, Options));
        }

        public static Ilogger CreateLogger(AppSettings settings)
        {
            if (settings.LogFormat == "xml")
                return new EasyLog.XmlLogger();

            return new EasyLog.JsonLogger();
        }
    }
}