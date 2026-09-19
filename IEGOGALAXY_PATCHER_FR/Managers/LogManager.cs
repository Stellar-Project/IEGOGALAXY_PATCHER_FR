using System;
using System.IO;

namespace IEGOGALAXY_PATCHER_FR.Managers
{
    public static class LogManager
    {
        private static readonly string LogFilePath;

        static LogManager()
        {
            string appDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "IEGOGALAXY_PATCHER_FR"
            );

            if (!Directory.Exists(appDataFolder)) Directory.CreateDirectory(appDataFolder);

            LogFilePath = Path.Combine(appDataFolder, "log.txt");
        }

        public static void Log(string message)
        {
            try
            {
                string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                File.AppendAllText(LogFilePath, line + Environment.NewLine);
            }
            catch
            {
                // Le logging ne doit jamais faire planter l'application.
            }
        }

        public static string GetLogFilePath() => LogFilePath;
    }
}