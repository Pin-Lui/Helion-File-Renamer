using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Helion
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    
    public partial class MainWindow : Window
    {
        #region Felder

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
        private static MainWindow GUI_MainWindow;
        private static string ApplicationDirectory => AppDomain.CurrentDomain.BaseDirectory;
        private static string ListTxtFilePath => Path.Combine(ApplicationDirectory, "list.txt");
        private static string ListCsvFilePath => Path.Combine(ApplicationDirectory, "list.csv");
        private static string AllShowCsvFilePath => Path.Combine(ApplicationDirectory, "allshows.csv");

        #endregion Felder

        #region Konstruktor

        public MainWindow()
        {
            InitializeComponent();
            GUI_MainWindow = this;
            TxB_SeriesSearch.Text = TutMsg;
            TxB_FileExtension.Text = FileExtension;
            TxB_NewFileNamePattern.Text = DefaultFileNamePattern;
        }

        #endregion Konstruktor

        #region Public()

        public static void DisplayMessage(string text)
        {
            GUI_MainWindow.Lbl_Cout.Dispatcher.BeginInvoke(new Action(() =>
            {
                GUI_MainWindow.Lbl_Cout.Content = text;
            }));
        }

        public string RetrieveFileNamePattern()
        {
            ValidateString(TxB_NewFileNamePattern.Text, nameof(TxB_NewFileNamePattern.Text));
            string pattern = TxB_NewFileNamePattern.Text.Trim();
            return string.IsNullOrWhiteSpace(pattern) ? DefaultFileNamePattern : pattern;
        }

        public static async void DownloadShowListCsv()
        {
            await DownloadFileFromWeb(AllShowTextFilePath, AllShowCsvFilePath);
        }

        #endregion Public()

        #region Buttons

        private async void SelectShowButtonClick(object sender, RoutedEventArgs e)
        {
            /// < summary >
            /// Handles the click event for the 'Select Show' button. Downloads show lists, 
            /// checks file existence, retrieves episode CSV URLs, and updates the GUI accordingly.
            /// </summary>

            UpdateGUIStatus(false);
            ClearSeasonComboBoxIfShowSelected();

            if (IsShowNotSelected())
            {
                DisplayMessage(RenameSTitelErrorMsg);
                UpdateGUIStatus(true);
                return;
            }

            try
            {
                if (!await ProcessShowFileDownload())
                {
                    UpdateGUIStatus(true);
                    return;
                }

                int seasonSize = CsvFileManager.CalculateSeasonSize(CmB_SelectShow.Text);
                if (seasonSize == 0)
                {
                    UpdateGUIStatus(true);
                    return;
                }

                FillSeasonDropdown(seasonSize);
                DisplayMessage(CreateSeasonsFoundMessage(seasonSize));

                if (CmB_SelectSeason.Items.Count == 0)
                {
                    UpdateGUIStatus(true);
                    return;
                }

                CmB_SelectSeason.SelectedIndex = 0;
                CsvFileManager.CleanCsvFiles();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                UpdateGUIStatus(true);
            }
        }

        private async void SearchButtonClick(object sender, RoutedEventArgs e)
        {
            /// <summary>
            /// Handles the click event for the 'Search' button. Clears and updates the show selection
            /// based on the search criteria, downloads required files, and updates the GUI.
            /// </summary>

            UpdateGUIStatus(false);
            ClearShowComboBox();

            if (IsShowSearchInvalid())
            {
                DisplayMessage(RenameSTitelErrorMsg);
                UpdateGUIStatus(true);
                return;
            }

            CmB_SelectSeason.Items.Clear();

            try
            {
                if (!await ProcessShowSearchDownload())
                {
                    UpdateGUIStatus(true);
                    return;
                }

                var results = RetrieveSearchResults();
                FillShowDropdown(results);
                DisplayMessage(CreateSearchResultsMessage(results.Count));

                if (CmB_SelectShow.Items.Count == 0)
                {
                    UpdateGUIStatus(true);
                    return;
                }

                CmB_SelectShow.SelectedIndex = 0;
                CsvFileManager.CleanCsvFiles();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                UpdateGUIStatus(true);
            }
        }

        private async void GenerateSeasonEpisodeListClick(object sender, RoutedEventArgs e)
        {
            /// <summary>
            /// Handles the click event for generating a season's episode name list. Validates the user's input,
            /// downloads necessary files, and creates a text file with episode names for the selected season.
            /// </summary>

            UpdateGUIStatus(false);

            if (!IsShowSelectionValid())
            {
                DisplayMessage(RenameSTitelErrorMsg);
                UpdateGUIStatus(true);
                return;
            }

            try
            {
                if (!await DownloadShowFiles())
                {
                    UpdateGUIStatus(true);
                    return;
                }

                string seasonNr = FormatSeasonNumber(CmB_SelectSeason.SelectedIndex + 1);

                if (!CsvFileManager.CreateSeasonTextFile(CmB_SelectShow.Text, seasonNr))
                {
                    DisplayMessage(ListGenErrorMsg);
                    UpdateGUIStatus(true);
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
                UpdateGUIStatus(true);
                OpenListFile();
            }
        }

        private void RenameFilesWithListClick(object sender, RoutedEventArgs e)
        {
            /// <summary>
            /// Handles the click event for the 'Rename Files With List' button. Validates input, checks for list file existence,
            /// generates a preview of new file names, and performs the file renaming operation.
            /// </summary>

            UpdateGUIStatus(false);

            if (!IsFileRenameInputValid())
            {
                DisplayMessage(RenameSTitelErrorMsg);
                UpdateGUIStatus(true);
                return;
            }

            if (!CsvFileManager.IsFilePresent(ListTxtFilePath))
            {
                DisplayMessage(NoListFoundErrorMsg);
                UpdateGUIStatus(true);
                return;
            }

            string sNumber = FormatSeasonNumber(CmB_SelectSeason.SelectedIndex + 1);

            var fileName = MediaFileHandler.CreateDataGridFilePreview(TxB_SeriesSearch.Text.TrimEnd(), sNumber, RetrieveFileExtension());
            if (fileName.Count == 0)
            {
                DisplayMessage(NoFilesFoundMsg);
                UpdateGUIStatus(true);
                return;
            }

            if (!DisplayGridViewWindow(fileName))
            {
                UpdateGUIStatus(true);
                return;
            }

            MediaFileHandler.RenameFilesFromList(TxB_SeriesSearch.Text.TrimEnd(), sNumber, RetrieveFileExtension());
            DisplayMessage(RenameSuccessMsg);
            UpdateGUIStatus(true);
        }

        #endregion Buttons

        #region Private()

        private static DataGridTextColumn CreateGridColumn(string header, string bindingPath)
        {
            //var style = new Style(typeof(TextBlock));
            //style.Setters.Add(new Setter(TextBlock.FontSizeProperty, 13));
            //style.Setters.Add(new Setter(TextBlock.FontFamilyProperty, new FontFamily("pack://application:,,,/Helion;component/Resources/#Comic Mono")));
            return new DataGridTextColumn
            {
                Header = header,
                Binding = new Binding(bindingPath),
                Width = header == "Separator" ? DataGridLength.Auto : new DataGridLength(1, DataGridLengthUnitType.Star),
                FontSize = 13,
                //ElementStyle = style
                //FontFamily = new FontFamily("pack://application:,,,/Helion;component/Resources/#Comic Mono")
            };
        }

        private List<ShowDetails> RetrieveSearchResults()
        {
            List<ShowDetails> buffer = CsvFileManager.RetrieveShowsData();
            return buffer.Where(x => x.Title.Contains(TxB_SeriesSearch.Text, StringComparison.OrdinalIgnoreCase)).ToList();
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
            GUI_MainWindow.PgB_Main.Value = progressPercentage;

            if (GUI_MainWindow.PgB_Main.Value == 100)
            {
                GUI_MainWindow.PgB_Main.Value = 0;
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

        private static void UpdateGUIStatus(bool state)
        {
            /// <summary>
            /// Toggles the enabled status of GUI elements. Used to prevent user interaction during processing.
            /// </summary>
            /// <param name="status">Boolean indicating whether to enable or disable the GUI elements.</param>

            List<Button> buttons = new()
            {
                GUI_MainWindow.Btn_Search,
                GUI_MainWindow.Btn_SelectShow,
                GUI_MainWindow.Btn_GenSeasonEPNameList,
                GUI_MainWindow.Btn_RenameFilesWithList
            };

            foreach (var button in buttons)
            {
                button.IsEnabled = state;
            }
        }

        private static void SanitizeTextBoxInput(TextBox textBox, string invalidChars)
        {
            /// <summary>
            /// Removes invalid characters from a given textbox's text. Used in various TextChanged event handlers.
            /// </summary>
            /// <param name="textBox">The textbox from which to remove invalid characters.</param>
            /// <param name="invalidChars">A string containing characters that are considered invalid.</param>

            if (textBox.Text.IndexOfAny(invalidChars.ToCharArray()) >= 0)
            {
                textBox.Text = new string(textBox.Text.Where(c => !invalidChars.Contains(c)).ToArray());
            }
        }

        private static void ValidateString(string value, string paramName)
        {
            /// <summary>
            /// Validates that a given string is not null or whitespace.
            /// </summary>
            /// <param name="value">The string to validate.</param>
            /// <param name="paramName">The name of the parameter being validated.</param>

            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(paramName);

        }

        private void ClearShowComboBox()
        {
            if (!string.IsNullOrWhiteSpace(CmB_SelectShow.Text))
            {
                CmB_SelectShow.Items.Clear();
                CmB_SelectShow.Text = "";
            }
        }

        private void ClearSeasonComboBoxIfShowSelected()
        {
            if (!string.IsNullOrWhiteSpace(CmB_SelectSeason.Text))
            {
                CmB_SelectSeason.Items.Clear();
                CmB_SelectSeason.Text = "";
            }
        }

        private void FillSeasonDropdown(int seasonSize)
        {
            for (int i = 0; i < seasonSize; i++)
            {
                CmB_SelectSeason.Items.Add(i <= 8 ? $"Season 0{i + 1}" : $"Season {i + 1}");
            }
        }

        private void FillShowDropdown(List<ShowDetails> results)
        {
            foreach (var item in results)
            {
                CmB_SelectShow.Items.Add(item.Title);
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

        private static async Task<bool> ProcessShowSearchDownload()
        {
            await DownloadFileFromWeb(AllShowTextFilePath, AllShowCsvFilePath);
            return CsvFileManager.IsFilePresent(AllShowCsvFilePath);
        }

        private async Task<bool> DownloadShowFiles()
        {
            await DownloadFileFromWeb(AllShowTextFilePath, AllShowCsvFilePath);
            if (!CsvFileManager.IsFilePresent(AllShowCsvFilePath)) return false;

            string urlToEpisodeCSV = CsvFileManager.RetrieveEpisodeCsvUrl(CmB_SelectShow.Text);
            await DownloadFileFromWeb(urlToEpisodeCSV, ListCsvFilePath);
            return CsvFileManager.IsFilePresent(ListCsvFilePath);
        }
        
        private async Task<bool> ProcessShowFileDownload()
        {
            await DownloadFileFromWeb(AllShowTextFilePath, AllShowCsvFilePath);
            if (!CsvFileManager.IsFilePresent(AllShowCsvFilePath)) return false;

            string urlToEpisodeCSV = CsvFileManager.RetrieveEpisodeCsvUrl(CmB_SelectShow.Text);
            await DownloadFileFromWeb(urlToEpisodeCSV, ListCsvFilePath);
            return CsvFileManager.IsFilePresent(ListCsvFilePath);
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
            /// <summary>
            /// Formats a season number as a string with a leading zero if less than 10.
            /// Example: 1 becomes '01', 10 remains '10'.
            /// </summary>
            /// <param name="seasonNumber">The season number to format.</param>
            /// <returns>A formatted string representing the season number.</returns>

            return seasonNumber <= 9 ? $"0{seasonNumber}" : seasonNumber.ToString();
        }

        private string RetrieveFileExtension()
        {
            string pattern = @"^[a-zA-Z0-9]+$";

            if (TxB_FileExtension.Text.Length == 3 && Regex.IsMatch(TxB_FileExtension.Text, pattern))
            {
                return "." + TxB_FileExtension.Text;
            }
            else
            {
                TxB_FileExtension.Text = FileExtension;
                return "." + FileExtension;
            }
        }

        private static bool DisplayGridViewWindow(List<string[]> fileName)
        {
            /// <summary>
            /// Creates and displays a GridView window with the specified file names.
            /// </summary>
            /// <param name="fileName">A list of file name arrays to display in the GridView window.</param>
            /// <returns>Boolean indicating whether the user confirmed the action in the GridView window.</returns>

            var gridViewWindow = new GridViewWindow();
            gridViewWindow.DataGrid.ItemsSource = fileName;
            ConfigureDataGridColumns(gridViewWindow.DataGrid);
            gridViewWindow.DataGrid.HeadersVisibility = DataGridHeadersVisibility.None;
            //gridViewWindow.DataGrid.FontFamily = new FontFamily("pack://application:,,,/Helion;component/Resources/#Comic Mono");
            //gridViewWindow.DataGrid.FontSize = 13;
            return gridViewWindow.ShowDialog() == true;
        }

        private bool IsShowSelectionValid()
        {
            return !string.IsNullOrWhiteSpace(CmB_SelectShow.Text) && !string.IsNullOrWhiteSpace(CmB_SelectSeason.Text);
        }

        private bool IsFileRenameInputValid()
        {
            return !string.IsNullOrWhiteSpace(TxB_SeriesSearch.Text) && !string.IsNullOrWhiteSpace(CmB_SelectSeason.Text);
        }

        private bool IsShowSearchInvalid()
        {
            return string.IsNullOrWhiteSpace(TxB_SeriesSearch.Text) || TxB_SeriesSearch.Text.Equals(TutMsg);
        }

        private bool IsShowNotSelected()
        {
            return string.IsNullOrWhiteSpace(CmB_SelectShow.Text);
        }

        #endregion Private()

        #region Events()

        private void TxB_SeriesSearch_GotMouseCapture(object sender, MouseEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(TxB_SeriesSearch.Text) && TxB_SeriesSearch.Text.Equals(TutMsg))
            {
                TxB_SeriesSearch.Text = "";
            }
        }

        private void CmB_SelectShow_DropDownClosed(object sender, EventArgs e)
        {
            TxB_SeriesSearch.Text = CmB_SelectShow.Text;
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

        private void TxB_SeriesSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string invalidChars = new string(Path.GetInvalidFileNameChars()) + ";\'\"<>\\&";
            SanitizeTextBoxInput(TxB_SeriesSearch, invalidChars);
        }

        private void TxB_FileExtension_TextChanged(object sender, TextChangedEventArgs e)
        {
            string invalidChars = new string(Path.GetInvalidPathChars()) + ";\'\"<>\\&";
            SanitizeTextBoxInput(TxB_FileExtension, invalidChars);
        }

        private void TxB_NewFileNamePattern_TextChanged(object sender, TextChangedEventArgs e)
        {
            string invalidChars = new string(Path.GetInvalidFileNameChars()) + ";\'\"<>\\&";
            SanitizeTextBoxInput(TxB_NewFileNamePattern, invalidChars);
        }

        private void TxB_NewFileNamePattern_LostKeyFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxB_NewFileNamePattern.Text))
            {
                TxB_NewFileNamePattern.Text = DefaultFileNamePattern;
            }
        }

        private void CmB_SelectSeason_DropDownOpened(object sender, EventArgs e)
        {
            // Check if the ComboBox is empty
            if (sender is ComboBox comboBox && comboBox.Items.Count == 0)
            {
                // Close the drop-down if there are no items
                comboBox.IsDropDownOpen = false;
            }
        }

        private void CmB_SelectShow_DropDownOpened(object sender, EventArgs e)
        {
            // Check if the ComboBox is empty
            if (sender is ComboBox comboBox && comboBox.Items.Count == 0)
            {
                // Close the drop-down if there are no items
                comboBox.IsDropDownOpen = false;
            }
        }

        #endregion Events()
    }
}
