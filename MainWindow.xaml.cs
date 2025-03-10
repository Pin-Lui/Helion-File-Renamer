using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Collections.Generic; 
using System.Linq;
using System.Threading.Tasks;

namespace Helion
{
  public partial class MainWindow : Window
  {
    #region Fields

    public static string ApplicationDirectory => AppDomain.CurrentDomain.BaseDirectory;
    public static string ListTxtFilePath => Path.Combine(ApplicationDirectory, "list.txt");
    public static string ListCsvFilePath => Path.Combine(ApplicationDirectory, "list.csv");
    public static string AllShowCsvFilePath => Path.Combine(ApplicationDirectory, "allshows.csv");
    private static MainWindow MainGUI;

    private const string TutMsg = "Input Show Titel Here";
    private const string ListGenErrorMsg = "Generating List.txt failed";
    private const string RenameSTitelErrorMsg = "Input Show Titel and Select a Season";
    private const string NoListFoundErrorMsg = "No List.txt Found, Generate it first.";
    private const string DefaultFileNamePattern = "{Titel} {SeasonNumber} {EpisodeNumber} - {EpisodeName}";
    private const string RenameSuccessMsg = "Files Renamed!";
    private const string ListGeneratedSuccessMsg = "EP List.txt generated!";
    private const string NoFilesFoundMsg = "No Files Found!";
    private const string AllShowTextFilePath = "https://epguides.com/common/allshows.txt";
    private const string FileExtension = "mkv";
    private const string ExplorerErrorMessage = "Could not start explorer.exe. The operating system is not Windows or there is no file manager available.";
    private const string GenericExplorerErrorMessage = "Could not start explorer.exe.";

    #endregion Fields

    #region ()

    public MainWindow()
    {
      InitializeComponent();
      MainGUI = this;
      TXB_SeriesSearch.Text = TutMsg;
      TXB_FileExtension.Text = FileExtension;
      TXB_NewFileNamePattern.Text = DefaultFileNamePattern;
    }

    #endregion ()

    #region Public()

    public static void DisplayMessage(string text)
    {
      MainGUI.LBL_Cout.Dispatcher.BeginInvoke(new Action(() =>
      {
        MainGUI.LBL_Cout.Content = text;
      }));
    }

    public string RetrieveFileNamePattern()
    {
      ValidateString(TXB_NewFileNamePattern.Text, nameof(TXB_NewFileNamePattern.Text));
      string pattern = TXB_NewFileNamePattern.Text.Trim();
      return string.IsNullOrWhiteSpace(pattern) ? DefaultFileNamePattern : pattern;
    }

    public static async void DownloadShowListCsv()
    {
      await DownloadFileFromWeb(AllShowTextFilePath, AllShowCsvFilePath);
    }

    #endregion Public()

    #region Buttons

    private async void SearchButtonClick(object sender, RoutedEventArgs e)
    {
      ButtonsOn(false);
      ClearShowComboBox();
      if (IsShowSearchInvalid())
      {
        DisplayMessage(RenameSTitelErrorMsg);
        ButtonsOn(true);
        return;
      }
      CMB_SelectSeason.Items.Clear();
      try
      {
        if (!ShowsBuffered() && !await DownloadShowDB())
        {
          ButtonsOn(true);
          return;
        }
        var results = LoadSearchResults();
        AddBoxItem(results);
        DisplayMessage(CreateSearchResultsMessage(results.Count));
        if (CMB_SelectShow.Items.Count == 0)
        {
          ButtonsOn(true);
          return;
        }
        CMB_SelectShow.SelectedIndex = 0;
        CSVManager.CleanCsvFiles();
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.Message);
      }
      finally
      {
        ButtonsOn(true);
      }
    }

    private async void SelectShowButtonClick(object sender, RoutedEventArgs e)
    {
      ButtonsOn(false);
      ClearSeasonComboBoxIfShowSelected();
      if (IsShowNotSelected())
      {
        DisplayMessage(RenameSTitelErrorMsg);
        ButtonsOn(true);
        return;
      }
      try
      {
        if (!await DownloadEpisodeDB())
        {
          ButtonsOn(true);
          return;
        }
        int seasonSize = CSVManager.CalculateSeasonSize(CMB_SelectShow.Text);
        if (seasonSize == 0)
        {
          ButtonsOn(true);
          return;
        }
        FillSeasonDropdown(seasonSize);
        DisplayMessage(CreateSeasonsFoundMessage(seasonSize));
        if (CMB_SelectSeason.Items.Count == 0)
        {
          ButtonsOn(true);
          return;
        }
        CMB_SelectSeason.SelectedIndex = 0;
        CSVManager.CleanCsvFiles();
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.Message);
      }
      finally
      {
        ButtonsOn(true);
      }
    }

    private async void GenerateSeasonEpisodeListClick(object sender, RoutedEventArgs e)
    {
      ButtonsOn(false);
      if (!IsShowSelectionValid())
      {
        DisplayMessage(RenameSTitelErrorMsg);
        ButtonsOn(true);
        return;
      }
      try
      {
        if (!await DownloadEpisodeDB())
        {
          ButtonsOn(true);
          return;
        }
        string seasonNr = FormatSeasonNumber(CMB_SelectSeason.SelectedIndex + 1);
        if (!CSVManager.CreateSeasonTextFile(CMB_SelectShow.Text, seasonNr))
        {
          DisplayMessage(ListGenErrorMsg);
          ButtonsOn(true);
          return;
        }
        DisplayMessage(ListGeneratedSuccessMsg);
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.Message);
      }
      finally
      {
        ButtonsOn(true);
        OpenListFile();
      }
    }

    private void RenameFilesWithListClick(object sender, RoutedEventArgs e)
    {
      ButtonsOn(false);
      if (!IsFileRenameInputValid())
      {
        DisplayMessage(RenameSTitelErrorMsg);
        ButtonsOn(true);
        return;
      }
      if (!CSVManager.IsFilePresent(ListTxtFilePath))
      {
        DisplayMessage(NoListFoundErrorMsg);
        ButtonsOn(true);
        return;
      }
      string sNumber = FormatSeasonNumber(CMB_SelectSeason.SelectedIndex + 1);
      var fileName = FileHandler.CreateDataGridFilePreview(TXB_SeriesSearch.Text.TrimEnd(), sNumber, RetrieveFileExtension());
      if (fileName.Count == 0)
      {
        DisplayMessage(NoFilesFoundMsg);
        ButtonsOn(true);
        return;
      }
      if (!DisplayGridViewWindow(fileName))
      {
        ButtonsOn(true);
        return;
      }
      FileHandler.RenameFilesFromList(TXB_SeriesSearch.Text.TrimEnd(), sNumber, RetrieveFileExtension());
      DisplayMessage(RenameSuccessMsg);
      ButtonsOn(true);
    }

    #endregion Buttons

    #region Private()

    private static DataGridTextColumn CreateGridColumn(string header, string bindingPath)
    {
      return new DataGridTextColumn
      {
        Header = header,
        Binding = new Binding(bindingPath),
        Width = header == "Separator" ? DataGridLength.Auto : new DataGridLength(1, DataGridLengthUnitType.Star),
        FontSize = 13,
      };
    }

    private List<ShowDetails> LoadSearchResults()
    {
      App app = Application.Current as App;
      if (!ShowsBuffered())
      {
        List<ShowDetails> shows = CSVManager.RetrieveShowsData();
        app.ShowBuffer = shows;
      }
      return [.. app.ShowBuffer.Where(x => x.Title.Contains(TXB_SeriesSearch.Text, StringComparison.OrdinalIgnoreCase))];
    }

    private static void ConfigureDataGridColumns(DataGrid dataGrid)
    {
      dataGrid.Columns.Add(CreateGridColumn("Old Name", "[0]"));
      dataGrid.Columns.Add(CreateGridColumn("Separator", "[1]"));
      dataGrid.Columns.Add(CreateGridColumn("New Name", "[2]"));
    }

    private static void UpdateProgressBar(int progressPercentage, long _1, long _2)
    {
      DisplayMessage(Convert.ToString(progressPercentage) + "%");
      MainGUI.PGB_Main.Value = progressPercentage;
      if (MainGUI.PGB_Main.Value == 100)
      {
        MainGUI.PGB_Main.Value = 0;
      }
    }

    private static void OpenListFile()
    {
      try
      {
        new Process
        {
          StartInfo = new ProcessStartInfo(ListTxtFilePath)
          {
            UseShellExecute = true
          }
        }.Start();
      }
      catch (ArgumentNullException)
      {
        MessageBox.Show("The file path is null.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
      }
      catch (System.ComponentModel.Win32Exception)
      {
        try
        {
          Process.Start("notepad.exe", ListTxtFilePath);
        }
        catch (Exception)
        {
          MessageBox.Show("Could not open the file with the default text editor or Notepad.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
      }
      catch (Exception)
      {
        MessageBox.Show("Could not open the file with the default text editor.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private static void ButtonsOn(bool state)
    {
      List<Button> buttons =
      [
        MainGUI.BTN_Search,
        MainGUI.BTN_SelectShow,
        MainGUI.BTN_SeasonList,
        MainGUI.BTN_RenameFilesWithList
      ];
      foreach (var button in buttons)
      {
        button.IsEnabled = state;
      }
    }

    private static void SanitizeTextBoxInput(TextBox textBox, string invalidChars)
    {
      if (textBox.Text.IndexOfAny(invalidChars.ToCharArray()) >= 0)
      {
        textBox.Text = new string([.. textBox.Text.Where(c => !invalidChars.Contains(c))]);
      }
    }

    private static void ValidateString(string value, string paramName)
    {
      if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(paramName);
    }

    private void ClearShowComboBox()
    {
      if (!string.IsNullOrWhiteSpace(CMB_SelectShow.Text))
      {
        CMB_SelectShow.Items.Clear();
        CMB_SelectShow.Text = "";
      }
    }

    private void ClearSeasonComboBoxIfShowSelected()
    {
      if (!string.IsNullOrWhiteSpace(CMB_SelectSeason.Text))
      {
        CMB_SelectSeason.Items.Clear();
        CMB_SelectSeason.Text = "";
      }
    }

    private void FillSeasonDropdown(int seasonSize)
    {
      for (int i = 0; i < seasonSize; i++)
      {
        CMB_SelectSeason.Items.Add(i <= 8 ? $"Season 0{i + 1}" : $"Season {i + 1}");
      }
    }

    private void AddBoxItem(List<ShowDetails> results)
    {
      foreach (var item in results)
      {
        if (!CMB_SelectShow.Items.Contains(item.Title))
        {
          CMB_SelectShow.Items.Add(item.Title);
        }
      }
    }

    private static async Task DownloadFileFromWeb(string url, string fullPath)
    {
      using var client = new DownloadManager(url, fullPath);
      client.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) =>
      {
        UpdateProgressBar((int)progressPercentage, (long)totalFileSize, totalBytesDownloaded);
      };
      await client.InitiateDownload();
    }

    private static async Task<bool> DownloadShowDB()
    {
      await DownloadFileFromWeb(AllShowTextFilePath, AllShowCsvFilePath);
      return CSVManager.IsFilePresent(AllShowCsvFilePath);
    }

    private async Task<bool> DownloadEpisodeDB()
    {
      if (!ShowsBuffered())
      {
        await DownloadFileFromWeb(AllShowTextFilePath, AllShowCsvFilePath);
        if (!CSVManager.IsFilePresent(AllShowCsvFilePath)) return false;
      }
      string urlToEpisodeCSV = CSVManager.RetrieveEpisodeCsvUrl(CMB_SelectShow.Text);
      await DownloadFileFromWeb(urlToEpisodeCSV, ListCsvFilePath);
      return CSVManager.IsFilePresent(ListCsvFilePath);
    }

    private static string CreateSearchResultsMessage(int resultCount)
    {
      return $"Found {resultCount} match{(resultCount == 1 ? "!" : "es!")}";
    }

    private static string CreateSeasonsFoundMessage(int seasonSize)
    {
      return seasonSize == 1 ? $"Found {seasonSize} Season!" : $"Found {seasonSize} Seasons!";
    }

    private static string FormatSeasonNumber(int seasonNumber)
    {
      return seasonNumber <= 9 ? $"0{seasonNumber}" : seasonNumber.ToString();
    }

    private string RetrieveFileExtension()
    {
      string pattern = @"^[a-zA-Z0-9]+$";

      if (TXB_FileExtension.Text.Length == 3 && Regex.IsMatch(TXB_FileExtension.Text, pattern))
      {
        return "." + TXB_FileExtension.Text;
      }
      else
      {
        TXB_FileExtension.Text = FileExtension;
        return "." + FileExtension;
      }
    }

    private static bool DisplayGridViewWindow(List<string[]> fileName)
    {
      var gridViewWindow = new GridViewWindow();
      gridViewWindow.DataGrid.ItemsSource = fileName;
      ConfigureDataGridColumns(gridViewWindow.DataGrid);
      gridViewWindow.DataGrid.HeadersVisibility = DataGridHeadersVisibility.None;
      return gridViewWindow.ShowDialog() == true;
    }

    public static bool ShowsBuffered()
    {
      return Application.Current is App app && app.ShowBuffer != null && app.ShowBuffer.Count > 0;
    }

    private bool IsShowSelectionValid()
    {
      return !string.IsNullOrWhiteSpace(CMB_SelectShow.Text) && !string.IsNullOrWhiteSpace(CMB_SelectSeason.Text);
    }

    private bool IsFileRenameInputValid()
    {
      return !string.IsNullOrWhiteSpace(TXB_SeriesSearch.Text) && !string.IsNullOrWhiteSpace(CMB_SelectSeason.Text);
    }

    private bool IsShowSearchInvalid()
    {
      return string.IsNullOrWhiteSpace(TXB_SeriesSearch.Text) || TXB_SeriesSearch.Text.Equals(TutMsg);
    }

    private bool IsShowNotSelected()
    {
      return string.IsNullOrWhiteSpace(CMB_SelectShow.Text);
    }

    #endregion Private()

    #region Events()

    private void TXB_SeriesSearch_GotMouseCapture(object sender, MouseEventArgs e)
    {
      if (!string.IsNullOrWhiteSpace(TXB_SeriesSearch.Text) && TXB_SeriesSearch.Text.Equals(TutMsg))
      {
        TXB_SeriesSearch.Text = "";
      }
    }

    private void CMB_SelectShow_DropDownClosed(object sender, EventArgs e)
    {
      TXB_SeriesSearch.Text = CMB_SelectShow.Text;
    }

    private void Lbl_Cout_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      try
      {
        Process.Start("explorer.exe", ApplicationDirectory);
      }
      catch (InvalidOperationException)
      {
        MessageBox.Show(ExplorerErrorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
      }
      catch (Exception)
      {
        MessageBox.Show(GenericExplorerErrorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
      }

    }

    private void TXB_SeriesSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
      string invalidChars = new string(Path.GetInvalidFileNameChars()) + ";\'\"<>\\&";
      SanitizeTextBoxInput(TXB_SeriesSearch, invalidChars);
    }

    private void TXB_FileExtension_TextChanged(object sender, TextChangedEventArgs e)
    {
      string invalidChars = new string(Path.GetInvalidPathChars()) + ";\'\"<>\\&";
      SanitizeTextBoxInput(TXB_FileExtension, invalidChars);
    }

    private void TXB_NewFileNamePattern_TextChanged(object sender, TextChangedEventArgs e)
    {
      string invalidChars = new string(Path.GetInvalidFileNameChars()) + ";\'\"<>\\&";
      SanitizeTextBoxInput(TXB_NewFileNamePattern, invalidChars);
    }

    private void TXB_NewFileNamePattern_LostKeyFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
      if (string.IsNullOrWhiteSpace(TXB_NewFileNamePattern.Text))
      {
        TXB_NewFileNamePattern.Text = DefaultFileNamePattern;
      }
    }

    private void CMB_SelectSeason_DropDownOpened(object sender, EventArgs e)
    {
      if (sender is ComboBox comboBox && comboBox.Items.Count == 0)
      {
        comboBox.IsDropDownOpen = false;
      }
    }

    private void CMB_SelectShow_DropDownOpened(object sender, EventArgs e)
    {
      if (sender is ComboBox comboBox && comboBox.Items.Count == 0)
      {
        comboBox.IsDropDownOpen = false;
      }
    }

    #endregion Events()
  }
}
