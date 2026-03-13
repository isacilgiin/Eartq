using System;
using System.IO;
using System.Text.Json;

namespace eartq
{
    public class AppSettings
    {
        public string SelectedCamera { get; set; } = string.Empty;
        public string SelectedMicrophone { get; set; } = string.Empty;
        public string OutputDirectory { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
    }

    public static class SettingsManager
    {
        private static readonly string SettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                        return settings;
                }
            }
            catch (Exception) { /* Ignored, fallback to default */ }
            return new AppSettings();
        }

        public static void Save(AppSettings settings)
        {
            try
            {
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ayarlar kaydedilemedi: {ex.Message}");
            }
        }
    }
}
