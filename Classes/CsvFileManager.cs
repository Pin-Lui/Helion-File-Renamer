using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;

namespace Helion
{
    internal sealed class CsvFileManager
    {
        #region Felder

        private readonly string ShowTitle;
        private readonly string SeasonNumber;
        private static string ApplicationDirectory => AppDomain.CurrentDomain.BaseDirectory;
        private static string ListTxtFilePath => Path.Combine(ApplicationDirectory, "list.txt");
        private static string ListCsvFilePath => Path.Combine(ApplicationDirectory, "list.csv");
        private static string AllShowCsvFilePath => Path.Combine(ApplicationDirectory, "allshows.csv");

        #endregion Felder

        #region Konstruktor
        public CsvFileManager() { }

        public CsvFileManager(string showTitle) : this()
        {
            ValidateString(showTitle, nameof(showTitle));
            ShowTitle = showTitle;
        }

        public CsvFileManager(string showTitle, string seasonNr) : this(showTitle)
        {
            ValidateString(seasonNr, nameof(seasonNr));
            SeasonNumber = seasonNr;
        }

        #endregion Konstruktor

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
            /// <param name="filePath">The path of the file to check.</param>
            /// <returns>True if the file exists, otherwise false.</returns>

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

            return new CsvFileManager(showTitel).FindSeasonEpisodeCount();
        }

        public static bool CreateSeasonTextFile(string showTitel, string seasonNr)
        {
            /// <summary>
            /// Creates a text file containing episode details for a specific season of a show.
            /// </summary>
            /// <param name="showTitle">The title of the show.</param>
            /// <param name="seasonNr">The season number.</param>
            /// <returns>True if the file was created successfully, otherwise false.</returns>

            return new CsvFileManager(showTitel, seasonNr).ExportSeasonToTextFile();
        }

        public static List<EpisodeDetails> RetrieveAllEpisodesData(string showTitel)
        {
            /// <summary>
            /// Retrieves a list of all episodes for a given show.
            /// </summary>
            /// <param name="showTitle">The title of the show.</param>
            /// <returns>A list of episode details.</returns>

            return new CsvFileManager(showTitel).RetrieveAllEpisodesData();
        }

        public static List<ShowDetails> RetrieveShowsData()
        {
            /// <summary>
            /// Retrieves a list of all shows available in the CSV file.
            /// </summary>
            /// <returns>A list of show details.</returns>

            return new CsvFileManager().RetrieveAllShowsData();
        }

        public static void CleanCsvFiles()
        {
            /// <summary>
            /// Cleans up CSV files related to the application by deleting them.
            /// </summary>

            new CsvFileManager().DeleteCsvFiles();
        }

        public static string RetrieveEpisodeCsvUrl(string showTitel)
        {
            /// <summary>
            /// Retrieves the URL of the CSV file containing episode information for a specific show.
            /// </summary>
            /// <param name="showTitle">The title of the show.</param>
            /// <returns>The URL of the episode CSV file.</returns>

            return new CsvFileManager(showTitel).CreateEpisodeCsvUrl();
        }

        #endregion Public()

        #region Private()

        private static void ValidateString(string value, string paramName)
        {
            /// <summary>
            /// Validates that a given string is not null or whitespace.
            /// </summary>
            /// <param name="value">The string to validate.</param>
            /// <param name="paramName">The name of the parameter being validated.</param>

            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(paramName);
            
        }

        private static void ValidateString(string[] values, string paramName)
        {
            /// <summary>
            /// Validates that a given string[] is not null or whitespace.
            /// </summary>
            /// <param name="values">The string[] to validate.</param>
            /// <param name="paramName">The name of the parameter being validated.</param>
            
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
            /// <summary>
            /// Deletes specific CSV files used by the application.
            /// </summary>

            string[] filesToDelete = { AllShowCsvFilePath, ListCsvFilePath };

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
            /// <summary>
            /// Clears the contents of the list.txt file.
            /// </summary>

            if (!File.Exists(ListTxtFilePath))
            {
                return;
            }
     
            try
            {
                File.WriteAllText(ListTxtFilePath, string.Empty);
            }
            catch (IOException ioExp)
            {
                MessageBox.Show(ioExp.Message);
                return;
            }
        }

        private int FindSeasonEpisodeCount()
        {
            /// <summary>
            /// Finds the total number of episodes in the highest-numbered season for the given show.
            /// </summary>
            /// <returns>The maximum season number with available episodes.</returns>

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
            /// <summary>
            /// Constructs the URL to access the CSV file for episodes of the specified show.
            /// </summary>
            /// <returns>The URL to the episode CSV file.</returns>

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
            if (!File.Exists(AllShowCsvFilePath))
            {
                return new List<ShowDetails>();
            }

            using var reader = new StreamReader(AllShowCsvFilePath);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                    csv.Context.RegisterClassMap<ShowDetailsMapping>();
                    var allshowsBuffer = csv.GetRecords<ShowDetails>().ToList();

            return allshowsBuffer;
        }        

        private List<EpisodeDetails> RetrieveAllEpisodesData()
        {
            if (!File.Exists(ListCsvFilePath) || !StripHtmlFromCsv())
            {
                return new List<EpisodeDetails>();
            }

            using var reader = new StreamReader(ListCsvFilePath);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                    csv.Context.RegisterClassMap<EpisodeDetailsMapping>();
                    var allEpisodeBuffer = csv.GetRecords<EpisodeDetails>().ToList();

            return allEpisodeBuffer;
        }

        private bool ExportSeasonToTextFile()
        {
            /// <summary>
            /// Exports the episode details of a specified season to a text file.
            /// </summary>
            /// <returns>True if the export is successful, otherwise false.</returns>

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

            string leadingDotsPattern = @"^\.*";
            string invalidCharsPattern = @"[\\/:*?""<>|]";

            List<string> episodeNames = selectedEpisodes
                .Select(item => Regex.Replace(item.Title, leadingDotsPattern, string.Empty))
                .Select(cleanedTitle => Regex.Replace(cleanedTitle, invalidCharsPattern, string.Empty))
                .ToList();

            WriteLinesToFile(ListTxtFilePath, episodeNames.ToArray());
            DeleteCsvFiles();

            return IsFilePresent(ListTxtFilePath);
        }

        private static bool StripHtmlFromCsv()
        {
            /// <summary>
            /// Removes HTML tags from the CSV file content.
            /// </summary>
            /// <returns>True if the operation is successful, otherwise false.</returns>

            if (!File.Exists(ListCsvFilePath))
            {
                return false;
            }

            string combinedPattern = @"<(.|\n)*?>|(List Output)";

            var cleanedLines = File.ReadAllLines(ListCsvFilePath)
                .Select(line => Regex.Replace(line, combinedPattern, string.Empty))
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToArray();

            File.WriteAllLines(ListCsvFilePath, cleanedLines);

            return true;
        }

        private static void WriteLinesToFile(string path, params string[] lines)
        {
            /// <summary>
            /// Writes an array of strings to the specified file path, overwriting any existing content.
            /// Write all the lines to the file, manually controlling newlines to avoid an extra empty line
            /// </summary>
            /// <param name="path">The file path where the content is to be written.</param>
            /// <param name="lines">The array of strings to be written to the file.</param>

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

    internal sealed class ShowDetails
    {
        #region Felder
        public string Title { get; set; }
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

    internal sealed class EpisodeDetails
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

    internal sealed class ShowDetailsMapping : ClassMap<ShowDetails>
    {
        #region Public()

        public ShowDetailsMapping()
        {
            Map(m => m.Title).Name("title");
            Map(m => m.Directory).Name("directory");
            Map(m => m.Tvrage).Name("tvrage");
            Map(m => m.TVmaze).Name("TVmaze");
            Map(m => m.StartDate).Name("start date");
            Map(m => m.EndDate).Name("end date");
            Map(m => m.NumberOfEpisodes).Name("number of episodes");
            Map(m => m.RunTime).Name("run time");
            Map(m => m.Network).Name("network");
            Map(m => m.Country).Name("country");
            Map(m => m.Onhiatus).Name("onhiatus");
            Map(m => m.Onhiatusdesc).Name("onhiatusdesc");
        }

        #endregion Public()
    }

    internal sealed class EpisodeDetailsMapping : ClassMap<EpisodeDetails>
    {
        #region Public()

        public EpisodeDetailsMapping()
        {
            Map(m => m.EPNumber).Name("number");
            Map(m => m.Season).Name("season");
            Map(m => m.Episode).Name("episode");
            Map(m => m.Airdate).Name("airdate");
            Map(m => m.Title).Name("title");
            Map(m => m.TvmazeLink).Name("tvmaze link");
        }

        #endregion Public()
    }

}
