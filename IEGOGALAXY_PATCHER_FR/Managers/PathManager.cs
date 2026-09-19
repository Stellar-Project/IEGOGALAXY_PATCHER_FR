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

        public static string? DetectGameVersion()
        {
            string bigbangId = "000400000010BB00";
            string supernovaId = "000400000010BC00";

            // 1. Citra/Azahar
            if (Directory.Exists(GetCitraPath(supernovaId)) || Directory.Exists(GetAzaharPath(supernovaId))) return "supernova";
            if (Directory.Exists(GetCitraPath(bigbangId)) || Directory.Exists(GetAzaharPath(bigbangId))) return "bigbang";

            // 2. SD Cards
            foreach (var card in DetectAll3DSSDCards())
            {
                // Check luma
                string lumaTitles = Path.Combine(card, "luma", "titles");
                if (Directory.Exists(lumaTitles))
                {
                    if (Directory.Exists(Path.Combine(lumaTitles, supernovaId))) return "supernova";
                    if (Directory.Exists(Path.Combine(lumaTitles, bigbangId))) return "bigbang";
                }

                // Check Nintendo 3DS root for the actual game
                string nintendo3ds = Path.Combine(card, "Nintendo 3DS");
                if (Directory.Exists(nintendo3ds))
                {
                    try
                    {
                        if (Directory.GetDirectories(nintendo3ds, "0010BC00", SearchOption.AllDirectories).Length > 0) return "supernova";
                        if (Directory.GetDirectories(nintendo3ds, "0010BB00", SearchOption.AllDirectories).Length > 0) return "bigbang";
                    }
                    catch { }
                }
            }

            return null;
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