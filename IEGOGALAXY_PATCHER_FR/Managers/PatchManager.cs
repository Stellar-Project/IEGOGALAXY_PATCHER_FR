using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace IEGOGALAXY_PATCHER_FR.Managers
{
    public class PatchManager
    {
        public async Task InstallPatchAsync(string url, string destinationPath, Action<string> statusCallback, Action<double> progressCallback)
        {
            string tempZipPath = Path.Combine(Path.GetTempPath(), "iego_patch.zip");
            string extractPath = Path.Combine(Path.GetTempPath(), "iego_extracted");

            try
            {
                if (Directory.Exists(extractPath)) Directory.Delete(extractPath, true);
                Directory.CreateDirectory(extractPath);

                statusCallback("Téléchargement en cours...");
                await DownloadFileAsync(url, tempZipPath, progressCallback);

                statusCallback("Extraction des fichiers...");
                progressCallback(-1);

                await Task.Run(() => ZipFile.ExtractToDirectory(tempZipPath, extractPath));

                statusCallback("Installation...");
                CopyDirectory(extractPath, destinationPath);

                File.Delete(tempZipPath);
                Directory.Delete(extractPath, true);
            }
            catch
            {
                throw;
            }
        }

        private async Task DownloadFileAsync(string url, string outputPath, Action<double> progressCallback)
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
                                    progressCallback(progress);
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