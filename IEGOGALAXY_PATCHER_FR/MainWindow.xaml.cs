using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using AutoUpdaterDotNET;
using ControlzEx.Theming;
using IEGOGALAXY_PATCHER_FR.Managers;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Ookii.Dialogs.Wpf;

namespace IEGOGALAXY_PATCHER_FR
{
    public partial class MainWindow : MetroWindow
    {
        private const string TITLE_ID_BIGBANG = "000400000010BB00";
        private const string TITLE_ID_SUPERNOVA = "000400000010BC00";
        private const string URL_PATCH_BIGBANG = "https://iegogalaxy.fr/downloads/patch/latest/patch_bigbang_fr.zip";
        private const string URL_PATCH_SUPERNOVA = "https://iegogalaxy.fr/downloads/patch/latest/patch_supernova_fr.zip";

        private const string URL_XML_UPDATE = "https://raw.githubusercontent.com/TON_PSEUDO/TON_REPO/main/update.xml";

        private readonly PatchManager _patchManager;

        public MainWindow()
        {
            InitializeComponent();
            _patchManager = new PatchManager();

            ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
            ThemeManager.Current.SyncTheme();

            InitializeAutoUpdater();
            DisplayVersion();
            UpdatePath_Event(null, null);
        }

        private void InitializeAutoUpdater()
        {
            AutoUpdater.Start(URL_XML_UPDATE);
            AutoUpdater.Synchronous = true;
        }

        private void DisplayVersion()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            this.Title = $"IEGO GALAXY - PATCH FR | v{version.Major}.{version.Minor}.{version.Build}";
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            FlyoutSettings.IsOpen = !FlyoutSettings.IsOpen;
        }

        private async void BtnAbout_Click(object sender, RoutedEventArgs e)
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            await this.ShowMessageAsync("À propos",
                $"IEGO GALAXY PATCHER FR\nVersion {version}\n\n" +
                "Développé pour la communauté Inazuma Eleven.");
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

            if (RbCitra.IsChecked == true)
            {
                TxtPath.Text = PathManager.GetCitraPath(currentTitleId);
            }
            else if (RbAzahar.IsChecked == true)
            {
                TxtPath.Text = PathManager.GetAzaharPath(currentTitleId);
            }
            else
            {
                string sdCardRoot = PathManager.Detect3DSSDCards();
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

            BtnPatch.IsEnabled = false;

            try
            {
                await _patchManager.InstallPatchAsync(
                    targetUrl,
                    TxtPath.Text,
                    (status) => Dispatcher.Invoke(() => LblStatus.Text = status),
                    (progress) => Dispatcher.Invoke(() =>
                    {
                        if (progress < 0)
                        {
                            ProgressBar.IsIndeterminate = true;
                            LblPercentage.Text = "";
                        }
                        else
                        {
                            ProgressBar.IsIndeterminate = false;
                            ProgressBar.Value = progress;
                            LblPercentage.Text = $"{progress:F0}%";
                        }
                    })
                );

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
    }
}