using System;
using System.IO;
using System.Text.Json;

namespace IEGOGALAXY_PATCHER_FR.Managers;

public class AppSettings
{
    public bool AutoCheckUpdate { get; set; } = true;
    public int DownloadTimeoutSeconds { get; set; } = 60;
    public int ThemeIndex { get; set; }
    public int BackupRetentionCount { get; set; } = 1;
    public string? CustomBackupPath { get; set; } = null;
}

public static class SettingsManager
{
    private static readonly string SettingsFilePath;

    public static AppSettings Current { get; private set; } = new();

    static SettingsManager()
    {
        string appDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "IEGOGALAXY_PATCHER_FR"
        );

        if (!Directory.Exists(appDataFolder))
        {
            Directory.CreateDirectory(appDataFolder);
        }

        SettingsFilePath = Path.Combine(appDataFolder, "settings.json");
    }

    public static void Load()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                string json = File.ReadAllText(SettingsFilePath);
                Current = JsonSerializer.Deserialize<AppSettings>(json) ?? new();
            }
        }
        catch (Exception ex)
        {
            LogManager.Log($"Impossible de charger les paramètres : {ex.Message}");
            Current = new();
        }
    }

    public static void Save()
    {
        try
        {
            string json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFilePath, json);
        }
        catch (Exception ex)
        {
            LogManager.Log($"Impossible d'enregistrer les paramètres : {ex.Message}");
        }
    }
}
