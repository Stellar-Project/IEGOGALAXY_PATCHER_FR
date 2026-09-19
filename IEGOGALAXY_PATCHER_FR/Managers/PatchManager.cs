using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace IEGOGALAXY_PATCHER_FR.Managers
{
    public enum PatchErrorCode
    {
        None,
        NetworkError,
        Timeout,
        ChecksumMismatch,
        ExtractionFailed,
        InstallationFailed,
        Cancelled,
        Unknown
    }

    public class PatchException : Exception
    {
        public PatchErrorCode ErrorCode { get; }

        public PatchException(PatchErrorCode code, string message, Exception? inner = null)
            : base(message, inner)
        {
            ErrorCode = code;
        }
    }

    public class PatchManager
    {
        private const string VERSION_FILE_NAME = "iego_patch_version.txt";
        private const string BACKUP_FOLDER_NAME = "iego_backup";
        private const int HTTP_TIMEOUT_SECONDS = 60;
        private const int MAX_DOWNLOAD_RETRIES = 2;

        public async Task InstallPatchAsync(
            string url,
            string destinationPath,
            string patchVersion,
            string? expectedSha256,
            Action<string> statusCallback,
            Action<double> progressCallback,
            CancellationToken cancellationToken = default)
        {
            string tempZipPath = Path.Combine(Path.GetTempPath(), "iego_patch.zip");
            string extractPath = Path.Combine(Path.GetTempPath(), "iego_extracted");

            try
            {
                if (Directory.Exists(extractPath)) Directory.Delete(extractPath, true);
                Directory.CreateDirectory(extractPath);

                LogManager.Log($"Début de l'installation. Version cible : {patchVersion}. Destination : {destinationPath}");

                statusCallback("Connexion au serveur...");
                await DownloadFileWithRetryAsync(url, tempZipPath, statusCallback, progressCallback, cancellationToken);

                if (!string.IsNullOrWhiteSpace(expectedSha256))
                {
                    statusCallback("Vérification de l'intégrité du fichier...");
                    await VerifyChecksumAsync(tempZipPath, expectedSha256, cancellationToken);
                }

                statusCallback("Extraction des fichiers...");
                progressCallback(-1);

                try
                {
                    await Task.Run(() => ZipFile.ExtractToDirectory(tempZipPath, extractPath), cancellationToken);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    LogManager.Log($"Échec de l'extraction : {ex}");
                    throw new PatchException(PatchErrorCode.ExtractionFailed,
                        "Impossible d'extraire l'archive téléchargée. Le fichier est peut-être corrompu.", ex);
                }

                if (Directory.Exists(destinationPath) && Directory.GetFileSystemEntries(destinationPath).Length > 0)
                {
                    statusCallback("Sauvegarde de l'installation actuelle...");
                    BackupExistingInstall(destinationPath);
                }

                statusCallback("Copie des fichiers dans le dossier de destination...");
                try
                {
                    CopyDirectory(extractPath, destinationPath);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    LogManager.Log($"Échec de la copie vers la destination : {ex}");
                    throw new PatchException(PatchErrorCode.InstallationFailed,
                        "Impossible de copier les fichiers vers le dossier de destination. Vérifiez les permissions ou l'espace disque.", ex);
                }

                WriteInstalledVersion(destinationPath, patchVersion);

                File.Delete(tempZipPath);
                Directory.Delete(extractPath, true);

                statusCallback("Terminé");
                LogManager.Log("Installation terminée avec succès.");
            }
            catch (OperationCanceledException)
            {
                LogManager.Log("Installation annulée par l'utilisateur.");
                throw new PatchException(PatchErrorCode.Cancelled, "Installation annulée.");
            }
            catch (PatchException)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogManager.Log($"Erreur inattendue : {ex}");
                throw new PatchException(PatchErrorCode.Unknown, $"Erreur inattendue : {ex.Message}", ex);
            }
        }

        public string? GetInstalledVersion(string destinationPath)
        {
            string versionFile = Path.Combine(destinationPath, VERSION_FILE_NAME);
            if (!File.Exists(versionFile)) return null;

            try
            {
                return File.ReadAllText(versionFile).Trim();
            }
            catch
            {
                return null;
            }
        }

        public bool RestoreBackup(string destinationPath)
        {
            string backupPath = Path.Combine(Path.GetDirectoryName(destinationPath.TrimEnd(Path.DirectorySeparatorChar)) ?? "", BACKUP_FOLDER_NAME);

            if (!Directory.Exists(backupPath))
            {
                LogManager.Log("Restauration impossible : aucune sauvegarde trouvée.");
                return false;
            }

            LogManager.Log($"Restauration de la sauvegarde depuis {backupPath}");

            if (Directory.Exists(destinationPath)) Directory.Delete(destinationPath, true);
            Directory.Move(backupPath, destinationPath);

            LogManager.Log("Restauration terminée.");
            return true;
        }

        private void BackupExistingInstall(string destinationPath)
        {
            string? parentDir = Path.GetDirectoryName(destinationPath.TrimEnd(Path.DirectorySeparatorChar));
            if (parentDir == null) return;

            string backupPath = Path.Combine(parentDir, BACKUP_FOLDER_NAME);

            if (Directory.Exists(backupPath)) Directory.Delete(backupPath, true);

            LogManager.Log($"Sauvegarde de {destinationPath} vers {backupPath}");
            CopyDirectory(destinationPath, backupPath);
        }

        private void WriteInstalledVersion(string destinationPath, string version)
        {
            try
            {
                File.WriteAllText(Path.Combine(destinationPath, VERSION_FILE_NAME), version);
            }
            catch (Exception ex)
            {
                LogManager.Log($"Impossible d'écrire le fichier de version : {ex.Message}");
            }
        }

        private async Task VerifyChecksumAsync(string filePath, string expectedSha256, CancellationToken cancellationToken)
        {
            using (var sha256 = SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);
                string actualHash = Convert.ToHexString(hashBytes);

                if (!string.Equals(actualHash, expectedSha256, StringComparison.OrdinalIgnoreCase))
                {
                    LogManager.Log($"Échec de vérification. Attendu : {expectedSha256}, obtenu : {actualHash}");
                    throw new PatchException(PatchErrorCode.ChecksumMismatch,
                        "Le fichier téléchargé est corrompu ou ne correspond pas au checksum attendu.");
                }

                LogManager.Log("Checksum vérifié avec succès.");
            }
        }

        private async Task DownloadFileWithRetryAsync(
            string url,
            string outputPath,
            Action<string> statusCallback,
            Action<double> progressCallback,
            CancellationToken cancellationToken)
        {
            Exception? lastException = null;

            for (int attempt = 1; attempt <= MAX_DOWNLOAD_RETRIES + 1; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    if (attempt > 1)
                    {
                        statusCallback($"Nouvelle tentative de téléchargement ({attempt - 1}/{MAX_DOWNLOAD_RETRIES})...");
                        LogManager.Log($"Tentative de téléchargement {attempt}/{MAX_DOWNLOAD_RETRIES + 1}");
                    }

                    await DownloadFileAsync(url, outputPath, statusCallback, progressCallback, cancellationToken);
                    return;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (OperationCanceledException ex)
                {
                    // Timeout HttpClient, pas une annulation utilisateur
                    lastException = ex;
                    LogManager.Log($"Timeout lors du téléchargement (tentative {attempt}) : {ex.Message}");
                }
                catch (HttpRequestException ex)
                {
                    lastException = ex;
                    LogManager.Log($"Erreur réseau lors du téléchargement (tentative {attempt}) : {ex.Message}");
                }

                if (attempt <= MAX_DOWNLOAD_RETRIES)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                }
            }

            if (lastException is OperationCanceledException)
            {
                throw new PatchException(PatchErrorCode.Timeout,
                    "Le téléchargement a expiré (délai dépassé). Vérifiez votre connexion internet.", lastException);
            }

            throw new PatchException(PatchErrorCode.NetworkError,
                "Impossible de télécharger le patch. Vérifiez votre connexion internet.", lastException);
        }

        private async Task DownloadFileAsync(
            string url,
            string outputPath,
            Action<string> statusCallback,
            Action<double> progressCallback,
            CancellationToken cancellationToken)
        {
            using (var handler = new HttpClientHandler { AllowAutoRedirect = true })
            using (HttpClient client = new HttpClient(handler))
            {
                client.Timeout = Timeout.InfiniteTimeSpan; // on gère le timeout nous-mêmes via CancellationToken
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    linkedCts.CancelAfter(TimeSpan.FromSeconds(HTTP_TIMEOUT_SECONDS));

                    using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, linkedCts.Token))
                    {
                        response.EnsureSuccessStatusCode();

                        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                        var canReportProgress = totalBytes > 0;

                        if (canReportProgress)
                        {
                            statusCallback($"Téléchargement en cours... (0 / {FormatSize(totalBytes)})");
                        }
                        else
                        {
                            statusCallback("Téléchargement en cours... (taille inconnue)");
                            progressCallback(-1);
                        }

                        using (var contentStream = await response.Content.ReadAsStreamAsync(linkedCts.Token))
                        using (var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            var totalRead = 0L;
                            var buffer = new byte[81920];
                            var isMoreToRead = true;
                            var lastReportTime = DateTime.UtcNow;

                            do
                            {
                                // Réinitialise le compte à rebours du timeout à chaque donnée reçue :
                                // on ne veut pas couper un téléchargement lent mais actif.
                                linkedCts.CancelAfter(TimeSpan.FromSeconds(HTTP_TIMEOUT_SECONDS));

                                var read = await contentStream.ReadAsync(buffer, 0, buffer.Length, linkedCts.Token);
                                if (read == 0)
                                {
                                    isMoreToRead = false;
                                }
                                else
                                {
                                    await fileStream.WriteAsync(buffer, 0, read, linkedCts.Token);
                                    totalRead += read;

                                    if (canReportProgress)
                                    {
                                        var progress = (double)totalRead / totalBytes * 100;
                                        progressCallback(progress);

                                        // On ne spamme pas le statusCallback à chaque paquet lu.
                                        if ((DateTime.UtcNow - lastReportTime).TotalMilliseconds > 200)
                                        {
                                            statusCallback($"Téléchargement en cours... ({FormatSize(totalRead)} / {FormatSize(totalBytes)})");
                                            lastReportTime = DateTime.UtcNow;
                                        }
                                    }
                                    else if ((DateTime.UtcNow - lastReportTime).TotalMilliseconds > 200)
                                    {
                                        statusCallback($"Téléchargement en cours... ({FormatSize(totalRead)})");
                                        lastReportTime = DateTime.UtcNow;
                                    }
                                }
                            } while (isMoreToRead);
                        }
                    }
                }
            }
        }

        private static string FormatSize(long bytes)
        {
            string[] units = { "o", "Ko", "Mo", "Go" };
            double size = bytes;
            int unitIndex = 0;

            while (size >= 1024 && unitIndex < units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }

            return $"{size:0.#} {units[unitIndex]}";
        }

        private void CopyDirectory(string sourceDir, string destinationDir)
        {
            var dir = new DirectoryInfo(sourceDir);
            if (!dir.Exists) throw new DirectoryNotFoundException($"Source introuvable: {dir.FullName}");

            DirectoryInfo[] dirs = dir.GetDirectories();
            if (!Directory.Exists(destinationDir)) Directory.CreateDirectory(destinationDir);

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