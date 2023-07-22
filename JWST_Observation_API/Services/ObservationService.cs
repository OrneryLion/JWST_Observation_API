using Dapper;
using JWSTScraper.Models;

using MySqlConnector;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace JWST_Observation_API.Models
{
    public class ObservationService
    {
        private string _connectionString ;
        public List<ObservationUrl> ObservationUrls;

        public ObservationService(string connectionString)
        {
            _connectionString = connectionString;
            ObservationUrls = GetObservationUrls(); 
        }
        public void Initialize()
        {

            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    Console.WriteLine("Connection Successful");
                    connection.Close();
                }

            }
            catch
            {
                throw new Exception("Connection Unsuccessful");
            }
        }

        public List<ObservationUrl> GetObservationUrls()
        {

            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    return connection.Query<ObservationUrl>("SELECT * FROM ObservationUrls").ToList();
                    
                }

            }
            catch
            {
                throw new Exception("Connection Unsuccessful");
            }
        }
    }
}
