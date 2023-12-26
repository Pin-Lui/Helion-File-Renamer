using System.Net.Http;

namespace Helion
{
    public delegate void ProgressChangedHandler(long? totalFileSize, long totalBytesDownloaded, double? progressPercentage);

    public class DownloadManager : HttpClient
    {
        #region Felder

        private readonly string DownloadUrl;
        private readonly string DestinationFilePath;

        #endregion Felder

        #region Events

        public event ProgressChangedHandler ProgressChanged;

        #endregion Events

        #region Konstruktor

        public DownloadManager(string downloadUrl, string destinationFilePath)
        {
            /// <summary>
            /// Initializes a new instance of the DownloadManager class.
            /// </summary>
            /// <param name="downloadUrl">The URL from which to download the file.</param>
            /// <param name="destinationFilePath">The local file path where the downloaded file will be saved.</param>

            DownloadUrl = downloadUrl;
            DestinationFilePath = destinationFilePath;
        }

        #endregion Konstruktor

        #region Public()

        public async Task InitiateDownload()
        {
            /// <summary>
            /// Starts the download process for the file at the specified URL.
            /// </summary>

            using var response = await GetAsync(DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
                await DownloadFileFromResponse(response);
        }

        #endregion Public()

        #region Private()

        private async Task DownloadFileFromResponse(HttpResponseMessage response)
        {
            /// <summary>
            /// Downloads a file from the server response and writes it to the local file system.
            /// </summary>
            /// <param name="response">The HTTP response message containing the file content.</param>

            response.EnsureSuccessStatusCode();
            long? totalBytes = response.Content.Headers.ContentLength;
            using var contentStream = await response.Content.ReadAsStreamAsync();
                await HandleContentStreamDownload(totalBytes, contentStream);
        }

        private async Task HandleContentStreamDownload(long? totalDownloadSize, Stream contentStream)
        {
            long totalBytesRead = 0L; // variable to keep track of the total bytes read
            long readCount = 0L; // variable to keep track of the number of reads
            byte[] buffer = new byte[8192]; // buffer to store the data read
            bool isMoreToRead = true; // flag to check if there is more data to read

            using (FileStream fileStream = new(DestinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
            {
                // Read data from the stream and write it to the file
                do
                {
                    int bytesRead = await contentStream.ReadAsync(buffer);
                    if (bytesRead == 0)
                    {
                        isMoreToRead = false;
                        continue;
                    }

                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));

                    totalBytesRead += bytesRead;
                    readCount += 1;

                    // Call the progress changed event handler every 10 reads
                    if (readCount % 10 == 0)
                        UpdateDownloadProgress(totalDownloadSize, totalBytesRead);
                }
                while (isMoreToRead);
            }
                // Call the progress changed event handler one final time
                UpdateDownloadProgress(totalDownloadSize, totalBytesRead);
        }

        private void UpdateDownloadProgress(long? totalDownloadSize, long totalBytesRead)
        {
            /// <summary>
            /// Triggers the ProgressChanged event to notify about the download progress.
            /// </summary>
            /// <param name="totalDownloadSize">The total size of the download, if known.</param>
            /// <param name="totalBytesRead">The total number of bytes read so far.</param>

            if (ProgressChanged == null) return;

            double? progressPercentage = null;
            if (totalDownloadSize.HasValue)
                progressPercentage = Math.Round((double)totalBytesRead / totalDownloadSize.Value * 100, 2);

            ProgressChanged(totalDownloadSize, totalBytesRead, progressPercentage);
        }

        #endregion Private()
    }
}
