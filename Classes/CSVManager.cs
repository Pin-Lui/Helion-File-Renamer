using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using CsvHelper;

namespace Helion
{
  internal sealed class CSVManager
  {
    #region Fields

    private readonly string ShowTitle;
    private readonly string SeasonNumber;

    #endregion Fields

    #region ()

    public CSVManager() { }

    public CSVManager(string showTitle) : this()
    {
      ValidateString(showTitle, nameof(showTitle));
      ShowTitle = showTitle;
    }

    public CSVManager(string showTitle, string seasonNr) : this(showTitle)
    {
      ValidateString(seasonNr, nameof(seasonNr));
      SeasonNumber = seasonNr;
    }

    #endregion ()

    #region Public()

    public static void ShowOutputMessage(string text)
    {
      /// <summary>
      /// Displays the specified text in the main window's output area.
      /// </summary>
      /// <param name="text">The text to be displayed.</param>

      ValidateString(text, nameof(text));
      MainWindow.DisplayMessage(text);
    }

    public static bool IsFilePresent(string filePath)
    {
      /// <summary>
      /// Checks if a file exists at the given file path.
      /// </summary>
      /// <param name = "filePath"> The path of the file to check.</param>
      /// <returns> True if the file exists, otherwise false.</returns>

      ValidateString(filePath, nameof(filePath));
      return File.Exists(filePath);
    }

    public static int CalculateSeasonSize(string showTitel)
    {
      /// <summary>
      /// Calculates the number of seasons available for a given show.
      /// </summary>
      /// <param name="showTitle">The title of the show.</param>
      /// <returns>The total number of seasons for the show.</returns>

      return new CSVManager(showTitel).FindSeasonEpisodeCount();
    }

    public static bool CreateSeasonTextFile(string showTitel, string seasonNr)
    {
      /// <summary>
      /// Creates a text file containing episode details for a specific season of a show.
      /// </summary>
      /// <param name="showTitle">The title of the show.</param>
      /// <param name="seasonNr">The season number.</param>
      /// <returns>True if the file was created successfully, otherwise false.</returns>

      return new CSVManager(showTitel, seasonNr).ExportSeasonToTextFile();
    }

    public static List<EpisodeDetails> RetrieveAllEpisodesData(string showTitel)
    {
      /// <summary>
      /// Retrieves a list of all episodes for a given show.
      /// </summary>
      /// <param name="showTitle">The title of the show.</param>
      /// <returns>A list of episode details.</returns>

      return new CSVManager(showTitel).RetrieveAllEpisodesData();
    }

    public static List<ShowDetails> RetrieveShowsData()
    {
      /// <summary>
      /// Retrieves a list of all shows available in the CSV file.
      /// </summary>
      /// <returns>A list of show details.</returns>

      return new CSVManager().RetrieveAllShowsData();
    }

    public static void CleanCsvFiles()
    {
      /// <summary>
      /// Cleans up CSV files related to the application by deleting them.
      /// </summary>

      new CSVManager().DeleteCsvFiles();
    }

    public static string RetrieveEpisodeCsvUrl(string showTitel)
    {
      /// <summary>
      /// Retrieves the URL of the CSV file containing episode information for a specific show.
      /// </summary>
      /// <param name="showTitle">The title of the show.</param>
      /// <returns>The URL of the episode CSV file.</returns>

      return new CSVManager(showTitel).CreateEpisodeCsvUrl();
    }

    #endregion Public()

    #region Private()

    private static void ValidateString(string value, string paramName)
    {
      if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(paramName);
    }

    private static void ValidateString(string[] values, string paramName)
    {
      if (values == null)
        throw new ArgumentNullException(paramName);
      foreach (var value in values)
      {
        if (string.IsNullOrWhiteSpace(value))
          throw new ArgumentException($"An element in '{paramName}' is null or whitespace.");
      }
    }

    private void DeleteCsvFiles()
    {
      string[] filesToDelete = [MainWindow.AllShowCsvFilePath, MainWindow.ListCsvFilePath];
      foreach (string file in filesToDelete)
      {
        if (!File.Exists(file))
        {
          continue;
        }
        try
        {
          File.Delete(file);
        }
        catch (IOException ioExp)
        {
          MessageBox.Show(ioExp.Message);
        }
      }
    }

    private static void ClearListTxtFile()
    {
      if (!File.Exists(MainWindow.ListTxtFilePath))
      {
        return;
      }
      try
      {
        File.WriteAllText(MainWindow.ListTxtFilePath, string.Empty);
      }
      catch (IOException ioExp)
      {
        MessageBox.Show(ioExp.Message);
        return;
      }
    }

    private int FindSeasonEpisodeCount()
    {
      List<EpisodeDetails> EpisodeBuffer = RetrieveAllEpisodesData(ShowTitle);
      if (EpisodeBuffer == null || EpisodeBuffer.Count == 0)
      {
        return 0;
      }
      int maxSeason = EpisodeBuffer.Max(item => Convert.ToInt32(item.Season));
      return maxSeason;
    }

    private string CreateEpisodeCsvUrl()
    {
      List<ShowDetails> allshowsBuffer = RetrieveAllShowsData();
      ShowDetails searchShow = allshowsBuffer.FirstOrDefault(show => show.Title == ShowTitle);
      if (searchShow == null)
      {
        ShowOutputMessage("Cound not find " + ShowTitle);
        return null;
      }
      string MazeNr = searchShow.TVmaze;
      string PathToEpisodeMaze = ("https://epguides.com/common/exportToCSVmaze.asp?maze=" + MazeNr);
      return PathToEpisodeMaze;
    }

    private List<ShowDetails> RetrieveAllShowsData()
    {
      if (MainWindow.ShowsBuffered())
      { 
        App app = Application.Current as App;
        return app.ShowBuffer;
      }
      if (!File.Exists(MainWindow.AllShowCsvFilePath))
      {
        return [];
      }
      using var reader = new StreamReader(MainWindow.AllShowCsvFilePath);
      using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
      csv.Context.RegisterClassMap<ShowDetailsMapping>();
      var allshowsBuffer = csv.GetRecords<ShowDetails>().ToList();
      return allshowsBuffer;
    }

    private List<EpisodeDetails> RetrieveAllEpisodesData()
    {
      if (!File.Exists(MainWindow.ListCsvFilePath) || !StripHtmlFromCsv())
      {
        return [];
      }
      using var reader = new StreamReader(MainWindow.ListCsvFilePath);
      using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
      csv.Context.RegisterClassMap<EpisodeDetailsMapping>();
      var allEpisodeBuffer = csv.GetRecords<EpisodeDetails>().ToList();
      return allEpisodeBuffer;
    }

    private bool ExportSeasonToTextFile()
    {
      string leadingDotsPattern = @"^\.*";
      string invalidCharsPattern = @"[\\/:*?""<>|]";

      ClearListTxtFile();
      if (!int.TryParse(SeasonNumber, out int sNr))
      {
        ShowOutputMessage("Could not Parse the Season Nr. Try Again!");
        return false;
      }
      string confSeasonNr = sNr.ToString();
      List<EpisodeDetails> episodeListBuffer = RetrieveAllEpisodesData();
      List<EpisodeDetails> selectedEpisodes = episodeListBuffer.FindAll(x => x.Season == confSeasonNr);
      if (selectedEpisodes == null || selectedEpisodes.Count == 0)
      {
        ShowOutputMessage("Could not find the Season Nr. Try Again!");
        return false;
      }
      List<string> episodeNames = [.. selectedEpisodes
        .Select(item => Regex.Replace(item.Title, leadingDotsPattern, string.Empty))
        .Select(cleanedTitle => Regex.Replace(cleanedTitle, invalidCharsPattern, string.Empty))];
      WriteLinesToFile(MainWindow.ListTxtFilePath, [.. episodeNames]);
      DeleteCsvFiles();
      return IsFilePresent(MainWindow.ListTxtFilePath);
    }

    private static bool StripHtmlFromCsv()
    {
      string htmlapostrophe = "&#039;";
      string apostrophe = "'";
      string combinedPattern = @"<(.|\n)*?>|(List Output)";

      if (!File.Exists(MainWindow.ListCsvFilePath))
      {
        return false;
      }
      var cleanedLines = File.ReadAllLines(MainWindow.ListCsvFilePath)
        .Select(line => Regex.Replace(line, combinedPattern, string.Empty))
        .Select(line => line.Replace(htmlapostrophe, apostrophe))
        .Where(line => !string.IsNullOrWhiteSpace(line))
        .ToArray();
      File.WriteAllLines(MainWindow.ListCsvFilePath, cleanedLines);
      return true;
    }

    private static void WriteLinesToFile(string path, params string[] lines)
    {
      ValidateString(value: path, nameof(path));
      ValidateString(lines, nameof(lines));
      using var stream = new FileStream(path, FileMode.Create);
      using var writer = new StreamWriter(stream);
      for (int i = 0; i < lines.Length; i++)
      {
        writer.Write(lines[i]);
        if (i < lines.Length - 1)
        {
          writer.WriteLine();
        }
      }
    }

    #endregion Private()
  }
}