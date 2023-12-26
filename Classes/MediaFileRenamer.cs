using System.Windows;

namespace Helion
{
    internal sealed class MediaFileRenamer
    {
        #region Felder

        private readonly string ShowTitle;
        private readonly string SeasonNumber;
        private readonly string FileExtension;
        private static string ApplicationDirectory => AppDomain.CurrentDomain.BaseDirectory;
        private static string ListTxtFilePath => Path.Combine(ApplicationDirectory, "list.txt");

        #endregion Felder

        #region Kunstruktor

        public MediaFileRenamer(string showTitel, string seasonNr, string fileExtension)
        {
            ValidateString(showTitel, nameof(showTitel));
            ValidateString(seasonNr, nameof(seasonNr));
            ValidateString(fileExtension, nameof(fileExtension));
            ShowTitle = showTitel;
            SeasonNumber = seasonNr;
            FileExtension = fileExtension;
        }

        #endregion Kunstruktor

        #region Public()

        private static void ValidateString(string value, string paramName)
        {
            /// <summary>
            /// Validates that a given string is not null or whitespace.
            /// </summary>
            /// <param name="value">The string to validate.</param>
            /// <param name="paramName">The name of the parameter being validated.</param>

            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(paramName);

        }

        public static List<string[]> CreateDataGridFilePreview(string showTitel, string seasonNr, string fileExtension)
        {
            /// <summary>
            /// Creates a preview list of new file names for a given show, season, and file extension.
            /// </summary>
            /// <param name="showTitle">The title of the show.</param>
            /// <param name="seasonNr">The season number.</param>
            /// <param name="fileExtension">The file extension to filter the files.</param>
            /// <returns>A list of string arrays, each containing the old name, a separator, and the new name.</returns>

            return new MediaFileRenamer(showTitel, seasonNr, fileExtension).CreateDataGridPreviewList();
        }

        public static void RenameFilesFromList(string showTitel, string seasonNr, string fileExtension)
        {
            /// <summary>
            /// Renames files in the application directory based on a provided list for a specific show and season.
            /// </summary>
            /// <param name="showTitle">The title of the show.</param>
            /// <param name="seasonNr">The season number.</param>
            /// <param name="fileExtension">The file extension to filter the files.</param>

            new MediaFileRenamer(showTitel, seasonNr, fileExtension).RenameFilesFromListData();
        }

        #endregion Public()

        #region Private()

        private string[] FilterFileNamesByExtension(FileInfo[] Info)
        {
            /// <summary>
            /// Filters the file names in an array of FileInfo objects by a specific file extension.
            /// </summary>
            /// <param name="info">Array of FileInfo objects to filter.</param>
            /// <returns>An array of file names with the specified extension.</returns>

            return Info.Where(f => f.Extension == FileExtension)
                       .Select(f => f.Name)
                       .ToArray();
        }

        private List<string[]> CreateDataGridPreviewList()
        {
            /// <summary>
            /// Generates a list for previewing file name changes in a data grid format.
            /// This list includes the original file name and the proposed new file name.
            /// </summary>
            /// <returns>A list of string arrays for data grid display.</returns>

            var result = new List<string[]>();
            string[] episodeNames = File.ReadAllLines(ListTxtFilePath);
            var mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();

            if (mainWindow == null) return result;

            FileInfo[] infos = new DirectoryInfo(ApplicationDirectory).GetFiles();
            string[] filteredStrings = FilterFileNamesByExtension(infos);
            int loopLimit = Math.Min(filteredStrings.Length, episodeNames.Length);

            for (int i = 0; i < loopLimit; i++)
            {
                string episodeNumber = (i + 1).ToString("D2"); // Correct episode numbering
                string seasonNumberPadded = SeasonNumber.PadLeft(2, '0'); // Pad the season number with a leading zero if necessary

                string newFileName = $"{mainWindow.RetrieveFileNamePattern().Replace("{Titel}", ShowTitle).Replace("{SNr}", "S" + seasonNumberPadded).Replace("{ENr}", "E" + episodeNumber).Replace("{EPName}", episodeNames[i])}{FileExtension}";

                result.Add(new string[] { filteredStrings[i], "    ==>    ", newFileName });
            }

            return result;
        }
       
        private void RenameFilesFromListData()
        {
            /// <summary>
            /// Performs the actual file renaming operation using the names from the list file.
            /// It reads the episode names from a text file and renames files in the directory accordingly.
            /// </summary>

            string[] episodeNames = File.ReadAllLines(ListTxtFilePath);
            var mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();

            if (mainWindow == null) return;

            FileInfo[] infos = new DirectoryInfo(ApplicationDirectory).GetFiles();
            string[] filteredStrings = FilterFileNamesByExtension(infos);
            int loopLimit = Math.Min(filteredStrings.Length, episodeNames.Length);

            for (int i = 0; i < loopLimit; i++)
            {
                string episodeNumber = (i + 1).ToString("D2"); // Correct episode numbering
                string seasonNumberPadded = SeasonNumber.PadLeft(2, '0'); // Pad the season number with a leading zero if necessary

                string newName = $"{mainWindow.RetrieveFileNamePattern().Replace("{Titel}", ShowTitle).Replace("{SNr}", "S" + seasonNumberPadded).Replace("{ENr}", "E" + episodeNumber).Replace("{EPName}", episodeNames[i])}{FileExtension}";
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