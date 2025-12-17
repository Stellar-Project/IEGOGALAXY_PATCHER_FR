using System;
using System.IO;

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
                            return rootPath;
                        }
                    }
                }
            }
            catch { return ""; }
            return "";
        }
    }
}