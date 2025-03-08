using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;

namespace Helion
{
  internal sealed class MediaFileHandler
  {
    #region Fields

    private static string ApplicationDirectory => AppDomain.CurrentDomain.BaseDirectory;
    private static string ListTxtFilePath => Path.Combine(ApplicationDirectory, "list.txt");

    private readonly string ShowTitle;
    private readonly string SeasonNumber;
    private readonly string FileExtension;

    #endregion Fields

    #region ()

    public MediaFileHandler(string showTitel, string seasonNr, string fileExtension)
    {
      ValidateString(showTitel, nameof(showTitel));
      ValidateString(seasonNr, nameof(seasonNr));
      ValidateString(fileExtension, nameof(fileExtension));
      ShowTitle = showTitel;
      SeasonNumber = seasonNr;
      FileExtension = fileExtension;
    }

    #endregion ()

    #region Public()

    public static List<string[]> CreateDataGridFilePreview(string showTitel, string seasonNr, string fileExtension)
    {
      /// <summary>
      /// Creates a preview list of new file names for a given show, season, and file extension.
      /// </summary>
      /// <param name="showTitle">The title of the show.</param>
      /// <param name="seasonNr">The season number.</param>
      /// <param name="fileExtension">The file extension to filter the files.</param>
      /// <returns>A list of string arrays, each containing the old name, a separator, and the new name.</returns>

      return new MediaFileHandler(showTitel, seasonNr, fileExtension).CreateDataGridPreviewList();
    }

    public static void RenameFilesFromList(string showTitel, string seasonNr, string fileExtension)
    {
      /// <summary>
      /// Renames files in the application directory based on a provided list for a specific show and season.
      /// </summary>
      /// <param name="showTitle">The title of the show.</param>
      /// <param name="seasonNr">The season number.</param>
      /// <param name="fileExtension">The file extension to filter the files.</param>

      new MediaFileHandler(showTitel, seasonNr, fileExtension).RenameFilesFromListData();
    }

    #endregion Public()

    #region Private()

    private static void ValidateString(string value, string paramName)
    {
      if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(paramName);
    }

    private string[] FilterFileNamesByExtension(FileInfo[] Info)
    {
      return [.. Info.Where(f => f.Extension == FileExtension).Select(f => f.Name)];
    }

    private List<string[]> CreateDataGridPreviewList()
    {
      var result = new List<string[]>();
      string[] episodeNames = File.ReadAllLines(ListTxtFilePath);
      var mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
      if (mainWindow == null) return result;
      FileInfo[] infos = new DirectoryInfo(ApplicationDirectory).GetFiles();
      string[] filteredStrings = FilterFileNamesByExtension(infos);
      int loopLimit = Math.Min(filteredStrings.Length, episodeNames.Length);
      for (int i = 0; i < loopLimit; i++)
      {
        string episodeNumber = (i + 1).ToString("D2");
        string seasonNumberPadded = SeasonNumber.PadLeft(2, '0');
        string newFileName = $"{mainWindow.RetrieveFileNamePattern()
          .Replace("{Titel}", ShowTitle)
          .Replace("{SeasonNumber}", "S" + seasonNumberPadded)
          .Replace("{EpisodeNumber}", "E" + episodeNumber)
          .Replace("{EpisodeName}", episodeNames[i])}{FileExtension}";
        result.Add([filteredStrings[i], "    ==>    ", newFileName]);
      }
      return result;
    }

    private void RenameFilesFromListData()
    {
      string[] episodeNames = File.ReadAllLines(ListTxtFilePath);
      var mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
      if (mainWindow == null) return;
      FileInfo[] infos = new DirectoryInfo(ApplicationDirectory).GetFiles();
      string[] filteredStrings = FilterFileNamesByExtension(infos);
      int loopLimit = Math.Min(filteredStrings.Length, episodeNames.Length);
      for (int i = 0; i < loopLimit; i++)
      {
        string episodeNumber = (i + 1).ToString("D2");
        string seasonNumberPadded = SeasonNumber.PadLeft(2, '0');
        string newName = $"{mainWindow.RetrieveFileNamePattern()
          .Replace("{Titel}", ShowTitle)
          .Replace("{SeasonNumber}", "S" + seasonNumberPadded)
          .Replace("{EpisodeNumber}", "E" + episodeNumber)
          .Replace("{EpisodeName}", episodeNames[i])}{FileExtension}";
        string oldName = Path.Combine(ApplicationDirectory, filteredStrings[i]);
        string newFileName = Path.Combine(ApplicationDirectory, newName);
        try
        {
          File.Move(oldName, newFileName);
        }
        catch (Exception e)
        {
          MessageBox.Show(e.Message);
        }
      }
    }

    #endregion Private()
  }
}