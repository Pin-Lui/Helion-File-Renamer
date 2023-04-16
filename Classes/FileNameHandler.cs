using System.IO;
using System.Windows;

namespace Helion
{
    internal class FileNameHandler
    {
        #region Felder

        private readonly string _ShowTitel;
        private readonly string _AppDir;
        private readonly string _SeasonNr;
        private readonly string _PathToListTXT;
        private readonly string _FileExtension;

        #endregion Felder

        #region Kunstruktor

        public FileNameHandler(string showTitel, string seasonNr, string fileExtension)
        {
            _AppDir = (AppDomain.CurrentDomain.BaseDirectory);
            _PathToListTXT = (_AppDir + "\\list.txt");

            if (string.IsNullOrEmpty(showTitel)) throw new ArgumentNullException(nameof(showTitel), "Show Titel");
            if (string.IsNullOrEmpty(seasonNr)) throw new ArgumentNullException(nameof(seasonNr), "Season Number");
            if (string.IsNullOrEmpty(fileExtension) || string.IsNullOrWhiteSpace(fileExtension)) throw new ArgumentNullException(nameof(fileExtension), "File Extension");

            _ShowTitel = showTitel;
            _SeasonNr = seasonNr;
            _FileExtension = fileExtension;
        }

        #endregion Kunstruktor

        #region Public()

        public static List<string[]> DataGridPreview(string showTitel, string seasonNr, string fileExtension)
        {
            return new FileNameHandler(showTitel, seasonNr, fileExtension).GetDataGridPreview();
        }

        public static void RenameFilesWithList(string showTitel, string seasonNr, string fileExtension)
        {
            new FileNameHandler(showTitel, seasonNr, fileExtension).FileRenameWithList();
        }

        #endregion Public()

        #region Private()

        private string[] FilterFileNames(FileInfo[] Info)
        {
            return Info.Where(f => f.Extension == _FileExtension)
                       .Select(f => f.Name)
                       .ToArray();
        }

        private List<string[]> GetDataGridPreview()
        {
            // Initialize the result list
            List<string[]> result = new();

            // Read the episode names from the text file
            string[] episodeNames = File.ReadAllLines(_PathToListTXT);

            MainWindow mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();

            // Get the files in the search directory
            DirectoryInfo d = new(_AppDir);
            FileInfo[] fileInfos = d.GetFiles();

            // Get the file names with the right extension
            string[] filteredStrings = FilterFileNames(fileInfos);

            // Loop through the filtered file names
            for (int i = 0; i < filteredStrings.Length; i++)
            {
                // Stop if there are no more episode names
                if (i >= episodeNames.Length) break;

                // Get the formatted episode number
                string episodeNumber = (i + 1 < 10) ? "0" + Convert.ToString(i + 1) : Convert.ToString(i + 1);

                try
                {
                    // Generate the new file name based on the tag pattern in the text box
                    string newFileName = mainWindow.GetNewFileNamePattern()
                        .Replace("{Titel}", _ShowTitel)
                        .Replace("{SNr}", " S" + _SeasonNr)
                        .Replace("{ENr}", "E" + episodeNumber)
                        .Replace("{EPName}", episodeNames[i])
                        + _FileExtension;

                    // Add the old and new file names to the result list
                    result.Add(new string[] { filteredStrings[i], "    ==>    ", newFileName });
                }
                catch (FormatException e)
                {
                    MessageBox.Show(e.Message);
                }
            }

            return result;
        }

        private void FileRenameWithList()
        {
            // Read the episode names from the text file
            string[] episodeNames = File.ReadAllLines(_PathToListTXT);

            MainWindow mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();

            // Get the files in the search directory
            DirectoryInfo d = new(_AppDir);
            FileInfo[] infos = d.GetFiles();

            // Filter the file names
            string[] filteredStrings = FilterFileNames(infos);

            // Loop through the filtered file names
            for (int i = 0; i < filteredStrings.Length; i++)
            {
                // Stop if there are no more episode names
                if (i >= episodeNames.Length) break;

                // Get the formatted episode number
                string episodesNumber = (i + 1 < 10) ? "0" + Convert.ToString(i + 1) : Convert.ToString(i + 1);

                try
                {
                    // Generate the new file name based on the tag pattern in the text box
                    string newName = mainWindow.GetNewFileNamePattern()
                        .Replace("{Titel}", _ShowTitel)
                        .Replace("{SNr}", " S" + _SeasonNr)
                        .Replace("{ENr}", "E" + episodesNumber)
                        .Replace("{EPName}", episodeNames[i])
                        + _FileExtension;

                    // Rename the file
                    string oldName = _AppDir + filteredStrings[i];
                    string newFilePath = Path.Combine(_AppDir, newName);

                    // Check if the new file name already exists, and rename to a new unique name if it does
                    if (File.Exists(newFilePath))
                    {
                        int j = 1;
                        while (File.Exists(newFilePath))
                        {
                            newName = mainWindow.GetNewFileNamePattern()
                                .Replace("{Titel}", _ShowTitel)
                                .Replace("{Nr}", _SeasonNr)
                                .Replace("{ENr}", "E" + episodesNumber)
                                .Replace("{EPName}", episodeNames[i])
                                + "_" + j.ToString() + _FileExtension;

                            newFilePath = Path.Combine(_AppDir, newName);
                            j++;
                        }
                    }

                    File.Move(oldName, newFilePath);
                }
                catch (FormatException e)
                {
                    MessageBox.Show(e.Message);
                }
            }
        }

        #endregion Private()
    }
}