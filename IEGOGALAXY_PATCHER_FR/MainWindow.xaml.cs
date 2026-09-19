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
using System.Threading;

namespace IEGOGALAXY_PATCHER_FR
{
    public partial class MainWindow : MetroWindow
    {
        private const string TITLE_ID_BIGBANG = "000400000010BB00";
        private const string TITLE_ID_SUPERNOVA = "000400000010BC00";
        private const string URL_PATCH_BIGBANG = "https://iegogalaxy.fr/downloads/patch/latest/patch_bigbang_fr.zip";
        private const string URL_PATCH_SUPERNOVA = "https://iegogalaxy.fr/downloads/patch/latest/patch_supernova_fr.zip";

        private const string URL_XML_UPDATE = "https://raw.githubusercontent.com/Stellar-Project/IEGOGALAXY_PATCHER_FR/refs/heads/master/update.xml";

        private const string CURRENT_PATCH_VERSION = "1.0.0";
        private const string CHECKSUM_BIGBANG = ""; // à renseigner : SHA256 du patch_bigbang_fr.zip
        private const string CHECKSUM_SUPERNOVA = ""; // à renseigner : SHA256 du patch_supernova_fr.zip

        private readonly PatchManager _patchManager;
        private CancellationTokenSource? _cts;

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

            if (version != null)
            {
                this.Title = $"IEGO GALAXY - PATCH FR | v{version.Major}.{version.Minor}.{version.Build}";
            }
            else
            {
                this.Title = "IEGO GALAXY - PATCH FR";
            }
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
                var sdCards = PathManager.DetectAll3DSSDCards();

                if (sdCards.Count == 0)
                {
                    TxtPath.Text = "";
                }
                else if (sdCards.Count == 1)
                {
                    TxtPath.Text = Path.Combine(sdCards[0], "luma", "titles", currentTitleId);
                }
                else
                {
                    TxtPath.Text = "";
                    SetStatus("Plusieurs cartes SD détectées, sélectionnez le dossier manuellement.");
                }
            }

            UpdateVersionStatus();
        }

        private void UpdateVersionStatus()
        {
            if (string.IsNullOrWhiteSpace(TxtPath.Text)) return;

            string? installedVersion = _patchManager.GetInstalledVersion(TxtPath.Text);

            if (installedVersion == null)
            {
                SetStatus("Aucun patch détecté à cet emplacement.");
            }
            else if (installedVersion == CURRENT_PATCH_VERSION)
            {
                SetStatus($"Patch à jour (v{installedVersion}).");
            }
            else
            {
                SetStatus($"Mise à jour disponible : v{installedVersion} → v{CURRENT_PATCH_VERSION}.");
            }
        }

        private void SetStatus(string text)
        {
            LblStatus.Text = text;
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog();
            if (dialog.ShowDialog() == true)
            {
                TxtPath.Text = dialog.SelectedPath;
                UpdateVersionStatus();
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

            string currentTitleId = RbBigbang.IsChecked == true ? TITLE_ID_BIGBANG : TITLE_ID_SUPERNOVA;
            if (!PathManager.IsGameLikelyPresent(TxtPath.Text, currentTitleId))
            {
                var confirmResult = await this.ShowMessageAsync(
                    "Avertissement",
                    "Le jeu ne semble pas installé à cet emplacement. Voulez-vous continuer quand même ?",
                    MessageDialogStyle.AffirmativeAndNegative);

                if (confirmResult != MessageDialogResult.Affirmative) return;
            }

            string targetUrl = RbBigbang.IsChecked == true ? URL_PATCH_BIGBANG : URL_PATCH_SUPERNOVA;
            string expectedChecksum = RbBigbang.IsChecked == true ? CHECKSUM_BIGBANG : CHECKSUM_SUPERNOVA;

            BtnPatch.IsEnabled = false;
            BtnCancel.IsEnabled = true;
            BtnRestore.IsEnabled = false;
            ProgressBar.Value = 0;
            LblPercentage.Text = "0%";
            _cts = new CancellationTokenSource();

            try
            {
                await _patchManager.InstallPatchAsync(
                    targetUrl,
                    TxtPath.Text,
                    CURRENT_PATCH_VERSION,
                    expectedChecksum,
                    (status) => Dispatcher.Invoke(() => SetStatus(status)),
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
                    }),
                    _cts.Token
                );

                ProgressBar.Value = 100;
                ProgressBar.IsIndeterminate = false;
                LblPercentage.Text = "100%";
                SetStatus("Terminé !");

                await this.ShowMessageAsync("Succès", "Patch installé ! Bon jeu !");
                UpdateVersionStatus();
            }
            catch (PatchException pex)
            {
                LogManager.Log($"PatchException [{pex.ErrorCode}] : {pex.Message}");

                if (pex.ErrorCode == PatchErrorCode.Cancelled)
                {
                    SetStatus("Annulé");
                }
                else
                {
                    SetStatus($"Erreur ({pex.ErrorCode})");
                    await this.ShowMessageAsync("Erreur", $"[{pex.ErrorCode}] {pex.Message}\n\nConsultez le journal pour plus de détails :\n{LogManager.GetLogFilePath()}");
                }
            }
            catch (Exception ex)
            {
                LogManager.Log($"Erreur non gérée : {ex}");
                SetStatus("Erreur");
                await this.ShowMessageAsync("Erreur", $"Une erreur inattendue est survenue : {ex.Message}\n\nConsultez le journal pour plus de détails :\n{LogManager.GetLogFilePath()}");
            }
            finally
            {
                BtnPatch.IsEnabled = true;
                BtnCancel.IsEnabled = false;
                BtnRestore.IsEnabled = true;
                ProgressBar.IsIndeterminate = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
        }

        private async void BtnRestore_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtPath.Text)) return;

            var confirmResult = await this.ShowMessageAsync(
                "Confirmation",
                "Restaurer la sauvegarde précédente ? Le patch actuellement installé sera remplacé.",
                MessageDialogStyle.AffirmativeAndNegative);

            if (confirmResult != MessageDialogResult.Affirmative) return;

            bool success = _patchManager.RestoreBackup(TxtPath.Text);

            if (success)
            {
                UpdateVersionStatus();
                await this.ShowMessageAsync("Succès", "Sauvegarde restaurée.");
            }
            else
            {
                await this.ShowMessageAsync("Erreur", "Aucune sauvegarde disponible pour cet emplacement.");
            }
        }
    }
}