using Dapper;
using JWST_ClassLib;

using MySqlConnector;

namespace JWSTScraper.Models
{
    public class DatabaseManager
    {
        private string _connectionString;
        private IHttpClientFactory _clientFactory;
        private List<string> urls = new List<string>();
        public DatabaseManager(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void Initialize()
        {
            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                // Create the ObservationUrls table if it doesn't exist
                connection.Execute(@"
            CREATE TABLE IF NOT EXISTS ObservationUrls (
                Id INT AUTO_INCREMENT PRIMARY KEY, 
                Url TEXT, 
                ScrapeDate DATETIME
            ) ENGINE = InnoDB");

                // Create the ObservationData table if it doesn't exist
                connection.Execute(@"
            CREATE TABLE IF NOT EXISTS ObservationData (
                Id INT AUTO_INCREMENT PRIMARY KEY, 
                VisitId TEXT,
                PCSMode TEXT,
                VisitType TEXT,
                ScheduledStartTime TEXT,
                Duration TEXT,
                ScienceInstrumentAndMode TEXT,
                TargetName TEXT,
                Category TEXT,
                Keywords TEXT
            ) ENGINE = InnoDB");
            }
        }

        public void InsertObservationUrl(string url)
        {
            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                ObservationUrl observationUrl = new ObservationUrl { Url = url, ScrapeDate = DateTime.Now };
                connection.Execute("INSERT INTO ObservationUrls (Url, ScrapeDate) VALUES (@Url, @ScrapeDate)", observationUrl);
            }
        }
        public bool DoesUrlExist(string url)
        {
            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                var existingUrl = connection.QueryFirstOrDefault<string>("SELECT Url FROM ObservationUrls WHERE Url = @Url", new { Url = url });
                return existingUrl != null;
            }
        }

        public List<Observation> GetObservationsByDate(DateTime date)
        {
            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                IEnumerable<Observation> observationData = connection.Query<Observation>("SELECT * FROM ObservationData WHERE DATE(ScheduledStartTime) = @Date", new { Date = date.Date });
                return observationData.ToList();
            }
        }

        public void InsertObservationData(Observation observation)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    var parameters = new
                    {
                        observation.VisitId,
                        observation.PCSMode,
                        observation.VisitType,
                        observation.ScheduledStartTime,
                        observation.Duration,
                        observation.ScienceInstrumentAndMode,
                        observation.TargetName,
                        observation.Category,
                        Keywords = string.Join(",", observation.Keywords)  // Join the list into a single string with comma separators
                    };
                    connection.Execute("INSERT INTO ObservationData (VisitId, PCSMode, VisitType, ScheduledStartTime, Duration, ScienceInstrumentAndMode, TargetName, Category, Keywords) VALUES (@VisitId, @PCSMode, @VisitType, @ScheduledStartTime, @Duration, @ScienceInstrumentAndMode, @TargetName, @Category, @Keywords)", parameters);
                }
            }
            catch (Exception ex)
            {
                // Add logging or error handling here
                throw;
            }
        }



    }
}

