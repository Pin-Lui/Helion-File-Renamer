using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Helion
{
  public delegate void ProgressChangedHandler(long? totalFileSize, long totalBytesDownloaded, double? progressPercentage);

  public class DownloadManager(string downloadUrl, string destinationFilePath) : HttpClient
  {
    #region Fields

    private readonly string DownloadUrl = downloadUrl;
    private readonly string DestinationFilePath = destinationFilePath;

    #endregion Fields

    #region Events

    public event ProgressChangedHandler ProgressChanged;

    #endregion Events

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
        do // Read data from the stream and write it to the file
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
          if (readCount % 10 == 0) // Call the progress changed event handler every 10 reads
            UpdateDownloadProgress(totalDownloadSize, totalBytesRead);
        }
        while (isMoreToRead);
      }
      UpdateDownloadProgress(totalDownloadSize, totalBytesRead); // Call the progress changed event handler one final time
    }

    private void UpdateDownloadProgress(long? totalDownloadSize, long totalBytesRead)
    {
      if (ProgressChanged == null) return;
      double? progressPercentage = null;
      if (totalDownloadSize.HasValue)
        progressPercentage = Math.Round((double)totalBytesRead / totalDownloadSize.Value * 100, 2);
      ProgressChanged(totalDownloadSize, totalBytesRead, progressPercentage);
    }

    #endregion Private()
  }
}
