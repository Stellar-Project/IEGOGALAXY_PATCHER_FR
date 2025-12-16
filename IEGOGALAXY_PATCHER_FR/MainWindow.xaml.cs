using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ControlzEx.Theming;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Ookii.Dialogs.Wpf;

namespace IEGOGALAXY_PATCHER_FR
{
    public partial class MainWindow : MetroWindow
    {
        private const string TITLE_ID_BIGBANG = "000400000010BB00";
        private const string TITLE_ID_SUPERNOVA = "000400000010BC00";
        private const string URL_PATCH_BIGBANG = "";
        private const string URL_PATCH_SUPERNOVA = "";

        public MainWindow()
        {
            InitializeComponent();

            ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
            ThemeManager.Current.SyncTheme();

            UpdatePath_Event(null, null);
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            FlyoutSettings.IsOpen = !FlyoutSettings.IsOpen;
        }

        private async void BtnAbout_Click(object sender, RoutedEventArgs e)
        {
            await this.ShowMessageAsync("À propos",
                "IEGO GALAXY PATCHER FR\n\n" +
                "Développé pour la communauté Inazuma Eleven.\n" +
                "Permet l'installation automatique du patch FR sur Citra, Azahar et 3DS.\n\n" +
                "Version 1.3.0");
        }

        private void ComboTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var app = Application.Current;

            switch (ComboTheme.SelectedIndex)
            {
                case 0:
                    ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
                    ThemeManager.Current.SyncTheme();
                    break;

                case 1:
                    ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.DoNotSync;
                    ThemeManager.Current.ChangeTheme(app, "Dark.Blue");
                    break;

                case 2:
                    ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.DoNotSync;
                    ThemeManager.Current.ChangeTheme(app, "Light.Blue");
                    break;
            }
        }

        private void UpdatePath_Event(object? sender, RoutedEventArgs? e)
        {
            if (TxtPath == null || RbCitra == null || RbAzahar == null || RbBigbang == null) return;

            string currentTitleId = RbBigbang.IsChecked == true ? TITLE_ID_BIGBANG : TITLE_ID_SUPERNOVA;
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            if (RbCitra.IsChecked == true)
            {
                TxtPath.Text = Path.Combine(appData, "Citra", "load", "mods", currentTitleId);
            }
            else if (RbAzahar.IsChecked == true)
            {
                TxtPath.Text = Path.Combine(appData, "Azahar", "load", "mods", currentTitleId);
            }
            else
            {
                string sdCardRoot = Detect3DSSDCards();
                if (!string.IsNullOrEmpty(sdCardRoot))
                {
                    TxtPath.Text = Path.Combine(sdCardRoot, "luma", "titles", currentTitleId);
                }
                else
                {
                    TxtPath.Text = "";
                }
            }
        }

        private string Detect3DSSDCards()
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

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog();
            if (dialog.ShowDialog() == true)
            {
                TxtPath.Text = dialog.SelectedPath;
            }
        }

        private async void BtnPatch_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtPath.Text))
            {
                await this.ShowMessageAsync("Erreur", "Aucun chemin défini ou carte SD non détectée.");
                return;
            }

            if (!Directory.Exists(TxtPath.Text))
            {
                try { Directory.CreateDirectory(TxtPath.Text); }
                catch { await this.ShowMessageAsync("Erreur", "Impossible de créer le dossier de destination."); return; }
            }

            string targetUrl = RbBigbang.IsChecked == true ? URL_PATCH_BIGBANG : URL_PATCH_SUPERNOVA;
            string tempZipPath = Path.Combine(Path.GetTempPath(), "iego_patch.zip");
            string extractPath = Path.Combine(Path.GetTempPath(), "iego_extracted");

            BtnPatch.IsEnabled = false;

            try
            {
                if (Directory.Exists(extractPath)) Directory.Delete(extractPath, true);
                Directory.CreateDirectory(extractPath);

                await DownloadFileAsync(targetUrl, tempZipPath);

                LblStatus.Text = "Extraction...";
                ProgressBar.IsIndeterminate = true;

                await Task.Run(() => ZipFile.ExtractToDirectory(tempZipPath, extractPath));

                LblStatus.Text = "Installation...";

                CopyDirectory(extractPath, TxtPath.Text);

                File.Delete(tempZipPath);
                Directory.Delete(extractPath, true);

                LblStatus.Text = "Terminé !";
                ProgressBar.Value = 100;
                ProgressBar.IsIndeterminate = false;
                LblPercentage.Text = "100%";

                await this.ShowMessageAsync("Succès", "Patch installé ! Bon jeu !");
            }
            catch (Exception ex)
            {
                await this.ShowMessageAsync("Erreur", $"Erreur : {ex.Message}");
                LblStatus.Text = "Erreur";
            }
            finally
            {
                BtnPatch.IsEnabled = true;
                ProgressBar.IsIndeterminate = false;
            }
        }

        private async Task DownloadFileAsync(string url, string outputPath)
        {
            using (HttpClient client = new HttpClient())
            {
                using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    var canReportProgress = totalBytes != -1;

                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var totalRead = 0L;
                        var buffer = new byte[8192];
                        var isMoreToRead = true;

                        do
                        {
                            var read = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                            if (read == 0)
                            {
                                isMoreToRead = false;
                            }
                            else
                            {
                                await fileStream.WriteAsync(buffer, 0, read);
                                totalRead += read;
                                if (canReportProgress)
                                {
                                    var progress = (double)totalRead / totalBytes * 100;
                                    Dispatcher.Invoke(() =>
                                    {
                                        ProgressBar.Value = progress;
                                        LblPercentage.Text = $"{progress:F0}%";
                                        LblStatus.Text = "Téléchargement...";
                                    });
                                }
                            }
                        } while (isMoreToRead);
                    }
                }
            }
        }

        private void CopyDirectory(string sourceDir, string destinationDir)
        {
            var dir = new DirectoryInfo(sourceDir);
            if (!dir.Exists) throw new DirectoryNotFoundException($"Source introuvable: {dir.FullName}");

            DirectoryInfo[] dirs = dir.GetDirectories();
            Directory.CreateDirectory(destinationDir);

            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath, true);
            }

            foreach (DirectoryInfo subDir in dirs)
            {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir);
            }
        }
    }
}