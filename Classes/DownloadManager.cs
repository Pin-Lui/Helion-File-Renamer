using System.IO;
using System.Net.Http;

namespace Helion
{
    public delegate void ProgressChangedHandler(long? totalFileSize, long totalBytesDownloaded, double? progressPercentage);

    public class DownloadManager : HttpClient
    {
        #region Felder

        private readonly string _DownloadUrl;
        private readonly string _DestinationFilePath;

        #endregion Felder

        #region Events

        public event ProgressChangedHandler ProgressChanged;

        #endregion Events

        #region Konstruktor

        public DownloadManager(string downloadUrl, string destinationFilePath)
        {
            _DownloadUrl = downloadUrl;
            _DestinationFilePath = destinationFilePath;
        }

        #endregion Konstruktor

        #region Public()

        public async Task StartDownload()
        {
            // Send a GET request to the download URL
            using var response = await GetAsync(_DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
            // Download the file from the response
            await DownloadFileFromHttpResponseMessage(response);
        }

        #endregion Public()

        #region Private()

        private async Task DownloadFileFromHttpResponseMessage(HttpResponseMessage response)
        {
            // Ensure that the request was successful
            response.EnsureSuccessStatusCode();
            // Get the total bytes of the file from the response headers
            long? totalBytes = response.Content.Headers.ContentLength;
            // Get the content stream from the response
            using (var contentStream = await response.Content.ReadAsStreamAsync())
            {
                // Process the content stream to download the file
                await ProcessContentStream(totalBytes, contentStream);
            }
        }

        private async Task ProcessContentStream(long? totalDownloadSize, Stream contentStream)
        {
            long totalBytesRead = 0L; // variable to keep track of the total bytes read
            long readCount = 0L; // variable to keep track of the number of reads
            byte[] buffer = new byte[8192]; // buffer to store the data read
            bool isMoreToRead = true; // flag to check if there is more data to read

            // Open a filestream to write the data
            using (FileStream fileStream = new(_DestinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
            {
                // Read data from the stream and write it to the file
                do
                {
                    int bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length);
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
                        TriggerProgressChanged(totalDownloadSize, totalBytesRead);
                }
                while (isMoreToRead);
            }
            // Call the progress changed event handler one final time
            TriggerProgressChanged(totalDownloadSize, totalBytesRead);
        }

        private void TriggerProgressChanged(long? totalDownloadSize, long totalBytesRead)
        {
            // Check if the ProgressChanged event has any subscribers
            if (ProgressChanged == null)
                return;

            // Calculate the progress percentage
            double? progressPercentage = null;
            if (totalDownloadSize.HasValue)
                progressPercentage = Math.Round((double)totalBytesRead / totalDownloadSize.Value * 100, 2);

            // Trigger the ProgressChanged event with the total download size, total bytes read, and progress percentage
            ProgressChanged(totalDownloadSize, totalBytesRead, progressPercentage);
        }

        #endregion Private()
    }
}
