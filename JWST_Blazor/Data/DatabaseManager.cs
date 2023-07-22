using Dapper;  // Include Dapper
using JWST_ClassLib;
using MySqlConnector;

namespace JWST_Blazor.Data
{
    public class DatabaseManager
    {
        private string _connectionString;


        public DatabaseManager(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<Observation> GetAllObservations()
        {
            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                List<Observation> observations = connection.Query<Observation>("SELECT * FROM ObservationData").ToList();

                foreach (Observation? observation in observations)
                {
                    observation.KeywordList = observation.Keywords.Split(',').ToList();
                }

                return observations;
            }
        }



        // Add more methods as needed for other queries
    }
}
