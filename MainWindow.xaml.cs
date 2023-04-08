using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EPRenamer
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
        private const string DialogBoxTitel = "Rename Files to This?";
        private const string RenameSuccessMsg = "Files Renamed!";
        private const string ListGeneratedSuccessMsg = "EP List.txt generated!";
        private const string NoFilesFoundMsg = "No Files Found!";
        private const string PathAllShowtxt = "https://epguides.com/common/allshows.txt";
        private const string FileExtension = "mkv";
        private readonly string _AppDir;
        private readonly string _PathToListTXT;
        private readonly string _PathToListCSV;
        private readonly string _PathToAllShowCSV;

        private static MainWindow _gui;

        #endregion Felder

        #region Konstruktor

        public MainWindow()
        {
            InitializeComponent();
            _gui = this;
            _AppDir = (AppDomain.CurrentDomain.BaseDirectory);
            _PathToListTXT = (_AppDir + "\\list.txt");
            _PathToListCSV = (_AppDir + "\\list.csv");
            _PathToAllShowCSV = (_AppDir + "\\allshows.csv");
            TxB_SeriesSearch.Text = TutMsg;
            TxB_FileExtension.Text = FileExtension;
        }

        #endregion Konstruktor

        #region Public()

        public static void Cout(string text)
        {
            _gui.Lbl_Cout.Dispatcher.BeginInvoke(new Action(() => _gui.Lbl_Cout.Content = text));
        }

        #endregion Public()

        #region Buttons

        private async void Btn_Search_Click(object sender, RoutedEventArgs e)
        {
            // Disable GUI elements
            GuiActivated(false);

            // Clear the ComboBox for selecting a show and its text
            if (!string.IsNullOrWhiteSpace(CmB_SelectShow.Text))
            {
                CmB_SelectShow.Items.Clear();
                CmB_SelectShow.Text = "";
            }

            // Check if the user has entered a valid show title
            if (string.IsNullOrWhiteSpace(TxB_SeriesSearch.Text) || TxB_SeriesSearch.Text.Equals(TutMsg))
            {
                Cout("Input a Show Titel");
                GuiActivated(true);
                return;
            }

            // Clear the ComboBox for selecting a season
            CmB_SelectSeason.Items.Clear();

            try
            {
                // Download the web file and save it as a CSV
                await DownloadWebFile(PathAllShowtxt, _PathToAllShowCSV);

                // Check if the file exists and is not empty
                if (!CSVFileHandler.CheckforFiles(_PathToAllShowCSV))
                {
                    GuiActivated(true);
                    return;
                }

                // Get list of shows from EPGuideDB
                List<CSVAllShows> buffer = CSVFileHandler.GetEPGuideDB();

                // Get list of shows that match the search text (case-insensitive)
                List<CSVAllShows> results = buffer.Where(x => x.Titel.Contains(TxB_SeriesSearch.Text, StringComparison.OrdinalIgnoreCase)).ToList();

                // Add the title of each show to the CmB_SelectShow control
                foreach (var item in results)
                {
                    CmB_SelectShow.Items.Add(item.Titel);
                }

                // Display a message with the number of matches found
                int resultCount = results.Count;
                Cout("Found " + resultCount + " match" + (resultCount == 1 ? "!" : "es!"));

                // If no matches were found, activate the GUI and return
                if (CmB_SelectShow.Items.Count == 0)
                {
                    GuiActivated(true);
                    return;
                }

                // Select the first item in the CmB_SelectShow control
                CmB_SelectShow.SelectedIndex = 0;

                // Clean up any unnecessary resources
                CSVFileHandler.CleanCSVs();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            GuiActivated(true);
        }

        private async void Btn_SelectShow_Click(object sender, RoutedEventArgs e)
        {
            // Deactivate the GUI
            GuiActivated(false);

            // Clear the CmB_SelectSeason items and text if they are not null or whitespace
            if (!string.IsNullOrWhiteSpace(CmB_SelectSeason.Text))
            {
                CmB_SelectSeason.Items.Clear();
                CmB_SelectSeason.Text = "";
            }

            // If CmB_SelectShow text is null or whitespace, display error message and activate the GUI
            if (string.IsNullOrWhiteSpace(CmB_SelectShow.Text))
            {
                Cout(RenameSTitelErrorMsg);
                GuiActivated(true);
                return;
            }

            try
            {
                // Download the AllShow.txt file and save it as a CSV
                await DownloadWebFile(PathAllShowtxt, _PathToAllShowCSV);

                // If the CSV file doesn't exist, activate the GUI and return
                if (!CSVFileHandler.CheckforFiles(_PathToAllShowCSV))
                {
                    GuiActivated(true);
                    return;
                }

                // Get the URL of the episode CSV file for the selected show
                string UrlToEpisodeCSV = CSVFileHandler.GetEpsiodeCsvUrls(CmB_SelectShow.Text);

                // Download the episode CSV file and save it
                await DownloadWebFile(UrlToEpisodeCSV, _PathToListCSV);

                // If the episode CSV file doesn't exist, activate the GUI and return
                if (!CSVFileHandler.CheckforFiles(_PathToListCSV))
                {
                    GuiActivated(true);
                    return;
                }

                // Get the number of seasons for the selected show
                int seasonSize = CSVFileHandler.GetSeasonSize(CmB_SelectShow.Text);

                // If there are no seasons, activate the GUI and return
                if (seasonSize == 0)
                {
                    GuiActivated(true);
                    return;
                }

                // Use a for loop to add the seasons to the CmB_SelectSeason items
                // Use the ternary operator to format the season names correctly
                for (int i = 0; i < seasonSize; i++)
                {
                    CmB_SelectSeason.Items.Add(i <= 8 ? "Season 0" + (i + 1) : "Season " + (i + 1));
                }

                // Use the ternary operator to display the appropriate message
                Cout(seasonSize == 1 ? "Found " + seasonSize + " Season!" : "Found " + seasonSize + " Seasons!");

                if (CmB_SelectSeason.Items.Count == 0)
                {
                    GuiActivated(true);
                    return;
                }

                CmB_SelectSeason.SelectedIndex = 0;
                CSVFileHandler.CleanCSVs();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            GuiActivated(true);
        }

        private async void Btn_GenSeasonEPNameList_Click(object sender, RoutedEventArgs e)
        {
            // Disable buttons to prevent further actions
            GuiActivated(false);

            // Check if the show and season comboboxes have a selected value
            if (string.IsNullOrWhiteSpace(CmB_SelectShow.Text) || string.IsNullOrWhiteSpace(CmB_SelectSeason.Text))
            {
                GuiActivated(true);
                //output error message if series name or season number is missing
                Cout(RenameSTitelErrorMsg);
                return;
            }

            try
            {
                // Download the All Shows csv file
                await DownloadWebFile(PathAllShowtxt, _PathToAllShowCSV);

                // Check if the All Shows csv file was downloaded successfully
                if (!CSVFileHandler.CheckforFiles(_PathToAllShowCSV))
                {
                    GuiActivated(true);
                    return;
                }

                // Get the url to the selected show's episode csv file
                string UrlToEpisodeCSV = CSVFileHandler.GetEpsiodeCsvUrls(CmB_SelectShow.Text);

                // Download the episode csv file
                await DownloadWebFile(UrlToEpisodeCSV, _PathToListCSV);

                // Check if the episode csv file was downloaded successfully
                if (!CSVFileHandler.CheckforFiles(_PathToListCSV))
                {
                    GuiActivated(true);
                    return;
                }

                // Get the selected season number
                int sNr = (CmB_SelectSeason.SelectedIndex + 1);

                // Format the season number to have a leading zero if it is less than 9
                string seasonNr = (sNr <= 9) ? "0" + sNr.ToString() : sNr.ToString();

                // Generate the episode name list for the selected season
                if (!CSVFileHandler.GetSeasonTXTFile(CmB_SelectShow.Text, seasonNr))
                {
                    GuiActivated(true);
                    Cout(ListGenErrorMsg);
                    return;
                }

                // Print Success Msg.
                Cout(ListGeneratedSuccessMsg);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            // Activate buttons
            GuiActivated(true);
        }

        private void Btn_RenameFilesWithList_Click(object sender, RoutedEventArgs e)
        {
            //Dectivate GUI elements
            GuiActivated(false);

            //check if the series name and season number have been entered by the user
            if (string.IsNullOrWhiteSpace(TxB_SeriesSearch.Text) || string.IsNullOrWhiteSpace(CmB_SelectSeason.Text))
            {
                GuiActivated(true);
                //output error message if series name or season number is missing
                Cout(RenameSTitelErrorMsg);
                return;
            }

            //Check if list file exists
            if (!CSVFileHandler.CheckforFiles(_PathToListTXT))
            {
                GuiActivated(true);
                //output error message if list file is missing
                Cout(NoListFoundErrorMsg);
                return;
            }

            //calculate the season number in the format "S01" or "S02" etc.
            int sNr = CmB_SelectSeason.SelectedIndex + 1;
            string sNumber = sNr <= 9 ? "0" + Convert.ToString(sNr) : Convert.ToString(sNr);

            //create a preview of the new file names using the series name, season number, and file extension
            List<string> fileName = FileNameHandler.FileNamePreview(TxB_SeriesSearch.Text, sNumber, GetFileExtension());
            var message = string.Join(Environment.NewLine, fileName);

            //check if there are any files that match the search criteria
            if (string.IsNullOrEmpty(message) || string.IsNullOrWhiteSpace(message))
            {
                Cout(NoFilesFoundMsg);
                GuiActivated(true);
                return;
            }

            //show a dialog box displaying the new file names for confirmation

            MessageBoxResult dialogResult = MessageBox.Show(message, DialogBoxTitel, MessageBoxButton.YesNo, MessageBoxImage.Question);

            //if the user confirms, rename the files
            if (dialogResult == MessageBoxResult.Yes)
            {
                FileNameHandler.RenameFilesWithList(TxB_SeriesSearch.Text, sNumber, GetFileExtension());
                Cout(RenameSuccessMsg);
            }
            //if the user cancels, don't rename the files
            else if (dialogResult == MessageBoxResult.No)
            {
                GuiActivated(true);
                return;
            }

            //activate GUI elements
            GuiActivated(true);
        }

        #endregion Buttons

        #region Private()

        private static void SetProgressBarValue(int progressPercentage, long _1, long _2)
        {
            // Output the current progress percentage
            Cout(Convert.ToString(progressPercentage) + "%");

            // Update the progress bar value
            _gui.PgB_Main.Value = progressPercentage;

            // Check if the progress has reached 100%
            if (_gui.PgB_Main.Value == 100)
            {
                // Reset the progress bar value
                _gui.PgB_Main.Value = 0;
            }
        }

        private static async Task DownloadWebFile(string url, string fullPath)
        {
            // create a new instance of the DownloadManager class with the given url and full path
            using var client = new DownloadManager(url, fullPath);

            // subscribe to the ProgressChanged event of the DownloadManager
            client.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) =>
            {
                // call the SetProgressBarValue function and pass the progress percentage, total file size and total bytes downloaded as arguments
                SetProgressBarValue((int)progressPercentage, (long)totalFileSize, totalBytesDownloaded);
            };

            // start the download
            await client.StartDownload();
        }

        private static void GuiActivated(bool state)
        {
            // Create a list of buttons that need to have their state changed
            List<Button> buttons = new()
            {
                _gui.Btn_Search,
                _gui.Btn_SelectShow,
                _gui.Btn_GenSeasonEPNameList,
                _gui.Btn_RenameFilesWithList
            };

            // Iterate through the list of buttons
            foreach (var button in buttons)
            {
                // Update the button's state
                button.IsEnabled = state;
            }
        }

        private string GetFileExtension()
        {
            string pattern = @"^[a-zA-Z0-9]+$";
            string result;
            if (TxB_FileExtension.Text.Length == 3 && Regex.IsMatch(TxB_FileExtension.Text, pattern))
            {
                // TxB_FileExtension.Text is 3 characters long and contains only letters and numbers
                result = "." + TxB_FileExtension.Text;
            }
            else
            {
                // TxB_FileExtension.Text is not 3 characters long or contains characters that are not letters or numbers
                TxB_FileExtension.Text = FileExtension;
                result = "." + FileExtension;
            }

            return result;

        }

        private static string RemoveInvalidChars(string input)
        {
            return input.Replace(";", "")
                        .Replace("'", "")
                        .Replace("\"", "")
                        .Replace("\\", "")
                        .Replace("<", "")
                        .Replace(">", "")
                        .Replace("&", "");
        }

        #endregion Private()

        #region Events()

        private void TxB_SeriesSearch_GotMouseCapture(object sender, MouseEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(TxB_SeriesSearch.Text) && TxB_SeriesSearch.Text.Equals(TutMsg))
            {
                TxB_SeriesSearch.Text = "";
                //TxB_SeriesSearch.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        private void CmB_SelectShow_DropDownClosed(object sender, EventArgs e)
        {
            TxB_SeriesSearch.Text = CmB_SelectShow.Text;
        }

        private void Lbl_Cout_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Process.Start("explorer.exe", _AppDir);
        }

        private void TxB_SeriesSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TxB_SeriesSearch.Text.Contains(';')  ||
                TxB_SeriesSearch.Text.Contains('\'') ||
                TxB_SeriesSearch.Text.Contains('"')  ||
                TxB_SeriesSearch.Text.Contains('\\') ||
                TxB_SeriesSearch.Text.Contains('<')  ||
                TxB_SeriesSearch.Text.Contains('>')  ||
                TxB_SeriesSearch.Text.Contains('&'))
            {
                TxB_SeriesSearch.Text = RemoveInvalidChars(TxB_SeriesSearch.Text);
            }
        }

        private void TxB_FileExtension_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TxB_FileExtension.Text.Contains(';')  ||
                TxB_FileExtension.Text.Contains('\'') ||
                TxB_FileExtension.Text.Contains('"')  ||
                TxB_FileExtension.Text.Contains('\\') ||
                TxB_FileExtension.Text.Contains('<')  ||
                TxB_FileExtension.Text.Contains('>')  ||
                TxB_FileExtension.Text.Contains('&'))
            {
                TxB_FileExtension.Text = RemoveInvalidChars(TxB_FileExtension.Text);
            }

        }

        #endregion Events()

    }

    internal class CSVAllShows
    {
        #region Felder
        public string Titel { get; set; }
        public string Directory { get; set; }
        public string Tvrage { get; set; }
        public string TVmaze { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string NumberOfEpisodes { get; set; }
        public string RunTime { get; set; }
        public string Network { get; set; }
        public string Country { get; set; }
        public string Onhiatus { get; set; }
        public string Onhiatusdesc { get; set; }

        #endregion Felder
    }

    internal class CSVEpisodes
    {
        #region Felder
        public string EPNumber { get; set; }
        public string Season { get; set; }
        public string Episode { get; set; }
        public string Airdate { get; set; }
        public string Title { get; set; }
        public string TvmazeLink { get; set; }

        #endregion Felder
    }

}
