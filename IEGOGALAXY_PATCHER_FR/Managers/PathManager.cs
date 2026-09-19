using System;
using System.IO;
using System.Collections.Generic;

namespace IEGOGALAXY_PATCHER_FR.Managers
{
    public static class PathManager
    {
        public static string GetCitraPath(string titleId)
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "Citra", "load", "mods", titleId);
        }

        public static string GetAzaharPath(string titleId)
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "Azahar", "load", "mods", titleId);
        }

        public static string Detect3DSSDCards()
        {
            var cards = DetectAll3DSSDCards();
            return cards.Count > 0 ? cards[0] : "";
        }

        public static List<string> DetectAll3DSSDCards()
        {
            var result = new List<string>();

            try
            {
                DriveInfo[] drives = DriveInfo.GetDrives();
                foreach (DriveInfo drive in drives)
                {
                    if (drive.DriveType == DriveType.Removable && drive.IsReady)
                    {
                        string rootPath = drive.RootDirectory.FullName;
                        if (Directory.Exists(Path.Combine(rootPath, "Nintendo 3DS")) ||
                            Directory.Exists(Path.Combine(rootPath, "luma")))
                        {
                            result.Add(rootPath);
                        }
                    }
                }
            }
            catch { }

            return result;
        }

        public static bool IsGameLikelyPresent(string installPath, string titleId)
        {
            try
            {
                if (!Directory.Exists(installPath)) return true; // pas encore de patch installé, rien à contredire

                // Cas carte SD : le dossier luma/titles/<titleId> ne prouve pas la présence du jeu lui-même,
                // seulement que le dossier de patch existe. On vérifie plutôt côté "Nintendo 3DS" du même volume.
                string? sdRoot = FindSDRootFromPath(installPath);
                if (sdRoot == null) return true; // Citra/Azahar : pas de vérification simple possible ici

                string titleFolder = Path.Combine(sdRoot, "Nintendo 3DS");
                return Directory.Exists(titleFolder);
            }
            catch
            {
                return true; // en cas de doute, ne pas bloquer l'utilisateur
            }
        }

        private static string? FindSDRootFromPath(string path)
        {
            foreach (var card in DetectAll3DSSDCards())
            {
                if (path.StartsWith(card, StringComparison.OrdinalIgnoreCase)) return card;
            }
            return null;
        }
    }
}