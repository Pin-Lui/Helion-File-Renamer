using System.Diagnostics;
using System.IO;
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
        private const string DefaultFileNamePattern = "{Titel} {SNr} {ENr} - {EPName}";
        private const string RenameSuccessMsg = "Files Renamed!";
        private const string ListGeneratedSuccessMsg = "EP List.txt generated!";
        private const string NoFilesFoundMsg = "No Files Found!";
        private const string PathAllShowtxt = "https://epguides.com/common/allshows.txt";
        private const string FileExtension = "mkv";
        private readonly string _AppDir;
        private readonly string _PathToListTXT;
        private readonly string _PathToListCSV;
        private readonly string _PathToAllShowCSV;

        private static MainWindow _GUI_MainWindow;

        #endregion Felder

        #region Konstruktor

        public MainWindow()
        {
            InitializeComponent();
            _GUI_MainWindow = this;
            _AppDir = (AppDomain.CurrentDomain.BaseDirectory);
            _PathToListTXT = (_AppDir + "\\list.txt");
            _PathToListCSV = (_AppDir + "\\list.csv");
            _PathToAllShowCSV = (_AppDir + "\\allshows.csv");
            TxB_SeriesSearch.Text = TutMsg;
            TxB_FileExtension.Text = FileExtension;
            TxB_NewFileNamePattern.Text = DefaultFileNamePattern;

        }

        #endregion Konstruktor

        #region Public()

        public static void Cout(string text)
        {
            // Access the Lbl_Cout label from the GUI_MainWindow on the main thread using Dispatcher.BeginInvoke
            _GUI_MainWindow.Lbl_Cout.Dispatcher.BeginInvoke(new Action(() =>
            {
                // Set the Content property of Lbl_Cout to the given text string
                _GUI_MainWindow.Lbl_Cout.Content = text;
            }));
        }

        public string GetNewFileNamePattern()
        {
            // Return the text from the textbox.
            return TxB_NewFileNamePattern.Text;
        }

        #endregion Public()

        #region Buttons

        private async void Btn_Search_Click(object sender, RoutedEventArgs e)
        {
            // Disable GUI elements
            GUIStatus(false);

            // Clear the ComboBox for selecting a show and its text
            if (!string.IsNullOrWhiteSpace(CmB_SelectShow.Text))
            {
                CmB_SelectShow.Items.Clear();
                CmB_SelectShow.Text = "";
            }

            // Check if the user has entered a valid show title
            if (string.IsNullOrWhiteSpace(TxB_SeriesSearch.Text) || TxB_SeriesSearch.Text.Equals(TutMsg))
            {
                // Display an error message
                Cout("Input a Show Titel");

                // Enable the GUI elements and return
                GUIStatus(true);
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
                    // Enable the GUI elements and return
                    GUIStatus(true);
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
                    // Enable the GUI elements and return
                    GUIStatus(true);
                    return;
                }

                // Select the first item in the CmB_SelectShow control
                CmB_SelectShow.SelectedIndex = 0;

                // Clean up any unnecessary resources
                CSVFileHandler.CleanCSVs();
            }
            catch (Exception ex)
            {
                // Display any exceptions thrown
                MessageBox.Show(ex.Message);
            }

            // Enable the GUI elements
            GUIStatus(true);
        }

        private async void Btn_SelectShow_Click(object sender, RoutedEventArgs e)
        {
            // Deactivate the GUI while processing
            GUIStatus(false);

            // Clear season dropdown list if a show has been selected
            if (!string.IsNullOrWhiteSpace(CmB_SelectSeason.Text))
            {
                CmB_SelectSeason.Items.Clear();
                CmB_SelectSeason.Text = "";
            }

            // If no show is selected, display an error message and reactivate GUI
            if (string.IsNullOrWhiteSpace(CmB_SelectShow.Text))
            {
                Cout(RenameSTitelErrorMsg);
                GUIStatus(true);
                return;
            }

            try
            {
                // Download the list of all shows and save as a CSV file
                await DownloadWebFile(PathAllShowtxt, _PathToAllShowCSV);

                // If the CSV file doesn't exist, reactivate GUI and return
                if (!CSVFileHandler.CheckforFiles(_PathToAllShowCSV))
                {
                    GUIStatus(true);
                    return;
                }

                // Get the URL of the episode CSV file for the selected show
                string UrlToEpisodeCSV = CSVFileHandler.GetEpsiodeCsvUrls(CmB_SelectShow.Text);

                // Download the episode CSV file and save it
                await DownloadWebFile(UrlToEpisodeCSV, _PathToListCSV);

                // If the episode CSV file doesn't exist, reactivate GUI and return
                if (!CSVFileHandler.CheckforFiles(_PathToListCSV))
                {
                    GUIStatus(true);
                    return;
                }

                // Get the number of seasons for the selected show
                int seasonSize = CSVFileHandler.GetSeasonSize(CmB_SelectShow.Text);

                // If there are no seasons, reactivate GUI and return
                if (seasonSize == 0)
                {
                    GUIStatus(true);
                    return;
                }

                // Use a loop to add the seasons to the CmB_SelectSeason dropdown list
                // Use the ternary operator to format the season names correctly
                for (int i = 0; i < seasonSize; i++)
                {
                    CmB_SelectSeason.Items.Add(i <= 8 ? "Season 0" + (i + 1) : "Season " + (i + 1));
                }

                // Use the ternary operator to display the appropriate message
                Cout(seasonSize == 1 ? "Found " + seasonSize + " Season!" : "Found " + seasonSize + " Seasons!");

                // If there are no seasons in the dropdown list, reactivate GUI and return
                if (CmB_SelectSeason.Items.Count == 0)
                {
                    GUIStatus(true);
                    return;
                }

                // Select the first season in the dropdown list and clean up CSV files
                CmB_SelectSeason.SelectedIndex = 0;
                CSVFileHandler.CleanCSVs();
            }
            catch (Exception ex)
            {
                // If an exception occurs, show an error message
                MessageBox.Show(ex.Message);
            }

            // Reactivate GUI
            GUIStatus(true);
        }

        private async void Btn_GenSeasonEPNameList_Click(object sender, RoutedEventArgs e)
        {
            // Disable buttons to prevent further actions
            GUIStatus(false);

            // Check if the show and season comboboxes have a selected value
            if (string.IsNullOrWhiteSpace(CmB_SelectShow.Text) || string.IsNullOrWhiteSpace(CmB_SelectSeason.Text))
            {
                GUIStatus(true);
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
                    GUIStatus(true);
                    return;
                }

                // Get the url to the selected show's episode csv file
                string UrlToEpisodeCSV = CSVFileHandler.GetEpsiodeCsvUrls(CmB_SelectShow.Text);

                // Download the episode csv file
                await DownloadWebFile(UrlToEpisodeCSV, _PathToListCSV);

                // Check if the episode csv file was downloaded successfully
                if (!CSVFileHandler.CheckforFiles(_PathToListCSV))
                {
                    GUIStatus(true);
                    return;
                }

                // Get the selected season number
                int sNr = (CmB_SelectSeason.SelectedIndex + 1);

                // Format the season number to have a leading zero if it is less than 9
                string seasonNr = (sNr <= 9) ? "0" + sNr.ToString() : sNr.ToString();

                // Generate the episode name list for the selected season
                if (!CSVFileHandler.GetSeasonTXTFile(CmB_SelectShow.Text, seasonNr))
                {
                    GUIStatus(true);
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
            GUIStatus(true);
        }

        private void Btn_RenameFilesWithList_Click(object sender, RoutedEventArgs e)
        {
            // Deactivate GUI elements
            GUIStatus(false);

            // Check if the series name and season number have been entered by the user
            if (string.IsNullOrWhiteSpace(TxB_SeriesSearch.Text) || string.IsNullOrWhiteSpace(CmB_SelectSeason.Text))
            {
                GUIStatus(true);
                // Output error message if series name or season number is missing
                Cout(RenameSTitelErrorMsg);
                return;
            }

            // Check if list file exists
            if (!CSVFileHandler.CheckforFiles(_PathToListTXT))
            {
                GUIStatus(true);
                // Output error message if list file is missing
                Cout(NoListFoundErrorMsg);
                return;
            }

            // Calculate the season number in the format "S01" or "S02" etc.
            int sNr = CmB_SelectSeason.SelectedIndex + 1;
            string sNumber = sNr <= 9 ? "0" + Convert.ToString(sNr) : Convert.ToString(sNr);

            // Create a preview of the new file names using the series name, season number, and file extension
            List<string[]> fileName = FileNameHandler.DataGridPreview(TxB_SeriesSearch.Text, sNumber, GetFileExtension());

            // Check if there are any files that match the search criteria
            if (fileName.Count == 0)
            {
                Cout(NoFilesFoundMsg);
                GUIStatus(true);
                return;
            }

            // Show the second window as a modal dialog
            var gridViewWindowResult = new GridViewWindow();

            // Get the instance of the second window
            GridViewWindow gridViewWindow = (GridViewWindow)Application.Current.Windows.OfType<Window>().FirstOrDefault(window => window is GridViewWindow);

            if (gridViewWindow == null)
            {
                GUIStatus(true);
                return;
            }

            // Set the item source for the DataGrid
            gridViewWindow.DataGrid.ItemsSource = fileName;

            // Create and define the columns for the DataGrid
            DataGridTextColumn oldNameColumn = new DataGridTextColumn
            {
                Header = "Old Name",
                Binding = new Binding("[0]"), // Bind to the first element of the array
                Width = new DataGridLength(1, DataGridLengthUnitType.Star), // Set width to fill available space
                FontSize = 13,
                FontFamily = new FontFamily("Poppins")
            };

            DataGridTextColumn middleColumn = new DataGridTextColumn();
            middleColumn.Header = "Separator";
            middleColumn.Binding = new Binding("[1]"); // Bind to the second element of the array
            middleColumn.FontSize = 13;
            middleColumn.FontFamily = new FontFamily("Poppins");

            DataGridTextColumn newNameColumn = new DataGridTextColumn();
            newNameColumn.Header = "New Name";
            newNameColumn.Binding = new Binding("[2]"); // Bind to the third element of the array
            newNameColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star); // Set width to fill available space
            newNameColumn.FontSize = 13;
            newNameColumn.FontFamily = new FontFamily("Poppins");

            // Add the columns to the DataGrid
            gridViewWindow.DataGrid.Columns.Add(oldNameColumn);
            gridViewWindow.DataGrid.Columns.Add(middleColumn);
            gridViewWindow.DataGrid.Columns.Add(newNameColumn);

            // Hide the column headers
            gridViewWindow.DataGrid.HeadersVisibility = DataGridHeadersVisibility.None;

            // Open the GridViewWindow as a modal dialog
            bool? result = gridViewWindowResult.ShowDialog();

            // Check the dialog result and proceed with file renaming if user clicked "OK"
            if (result == true)
            {
                FileNameHandler.RenameFilesWithList(TxB_SeriesSearch.Text, sNumber, GetFileExtension());
                // Output success message after renaming files
                Cout(RenameSuccessMsg);
            }
            // Otherwise, restore GUI elements and return
            else
            {
                GUIStatus(true);
                return;
            }

            // Restore GUI elements after renaming files
            GUIStatus(true);
        }

        #endregion Buttons

        #region Private()

        private static void SetProgressBarValue(int progressPercentage, long _1, long _2)
        {
            // Output the current progress percentage
            Cout(Convert.ToString(progressPercentage) + "%");

            // Update the progress bar value
            _GUI_MainWindow.PgB_Main.Value = progressPercentage;

            // Check if the progress has reached 100%
            if (_GUI_MainWindow.PgB_Main.Value == 100)
            {
                // Reset the progress bar value
                _GUI_MainWindow.PgB_Main.Value = 0;
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

        private static void GUIStatus(bool state)
        {
            // Create a list of buttons that need to have their state changed
            List<Button> buttons = new()
            {
                _GUI_MainWindow.Btn_Search,
                _GUI_MainWindow.Btn_SelectShow,
                _GUI_MainWindow.Btn_GenSeasonEPNameList,
                _GUI_MainWindow.Btn_RenameFilesWithList
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
            // Define a regular expression pattern to match strings containing only letters and numbers
            string pattern = @"^[a-zA-Z0-9]+$";

            string result;

            // Check if the length of the TxB_FileExtension.Text is 3 and matches the pattern
            if (TxB_FileExtension.Text.Length == 3 && Regex.IsMatch(TxB_FileExtension.Text, pattern))
            {
                // TxB_FileExtension.Text is 3 characters long and contains only letters and numbers
                result = "." + TxB_FileExtension.Text;
            }
            else
            {
                // TxB_FileExtension.Text is not 3 characters long or contains characters that are not letters or numbers
                // Set TxB_FileExtension.Text to the default FileExtension value
                TxB_FileExtension.Text = FileExtension;
                result = "." + FileExtension;
            }

            // Return the result
            return result;
        }

        #endregion Private()

        #region Events()

        private void TxB_SeriesSearch_GotMouseCapture(object sender, MouseEventArgs e)
        {
            // Check if the TxB_SeriesSearch textbox contains text and if it equals the tutorial message
            if (!string.IsNullOrWhiteSpace(TxB_SeriesSearch.Text) && TxB_SeriesSearch.Text.Equals(TutMsg))
            {
                // Clear the contents of the TxB_SeriesSearch textbox
                TxB_SeriesSearch.Text = "";
            }
        }

        private void CmB_SelectShow_DropDownClosed(object sender, EventArgs e)
        {
            // Set the text of the TxB_SeriesSearch textbox to the selected item in the CmB_SelectShow ComboBox
            TxB_SeriesSearch.Text = CmB_SelectShow.Text;
        }

        private void Lbl_Cout_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Open a new explorer window and navigate to the application directory
            Process.Start("explorer.exe", _AppDir);
        }
        
        private void TxB_SeriesSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Get the list of invalid filename characters and add additional characters that are not allowed
            string invalidChars = new string(Path.GetInvalidFileNameChars()) + ";\'\"<>\\&";

            // Check if the TxB_SeriesSearch textbox contains any invalid characters
            if (TxB_SeriesSearch.Text.IndexOfAny(invalidChars.ToCharArray()) >= 0)
            {
                // Remove any invalid characters from the TxB_SeriesSearch textbox
                TxB_SeriesSearch.Text = new string(TxB_SeriesSearch.Text
                    .Where(c => !invalidChars.Contains(c)).ToArray());
            }
        }

        private void TxB_FileExtension_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Get the list of invalid path characters and add additional characters that are not allowed
            string invalidChars = new string(Path.GetInvalidPathChars()) + ";\'\"<>\\&";

            // Check if the TxB_FileExtension textbox contains any invalid characters
            if (TxB_FileExtension.Text.IndexOfAny(invalidChars.ToCharArray()) >= 0)
            {
                // Remove any invalid characters from the TxB_FileExtension textbox
                TxB_FileExtension.Text = new string(TxB_FileExtension.Text
                    .Where(c => !invalidChars.Contains(c)).ToArray());
            }
        }

        private void TxB_NewFileNamePattern_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Get the list of invalid filename characters and add additional characters that are not allowed
            string invalidChars = new string(Path.GetInvalidFileNameChars()) + ";\'\"<>\\&";

            // Check if the TxB_NewFileNamePattern textbox contains any invalid characters
            if (TxB_NewFileNamePattern.Text.IndexOfAny(invalidChars.ToCharArray()) >= 0)
            {
                // Remove any invalid characters from the TxB_NewFileNamePattern textbox
                TxB_NewFileNamePattern.Text = new string(TxB_NewFileNamePattern.Text
                    .Where(c => !invalidChars.Contains(c)).ToArray());
            }
        }

        #endregion Events()

    }
}
