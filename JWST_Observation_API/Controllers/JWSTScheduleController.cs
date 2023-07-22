using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Globalization;
using JWST_ClassLib;
using System.Net.Http;
using MySqlConnector;
using System.Data;
using JWST_Observation_API.Models;
using JWSTScraper.Models;

namespace JWST_Observation_API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class JWSTScheduleController : ControllerBase
    {

        private readonly IHttpClientFactory _clientFactory;
        //private readonly ObservationService observationService;
        private readonly DatabaseManager _dbManager;

        public JWSTScheduleController(IHttpClientFactory clientFactory, DatabaseManager dbManager)
        {
            _clientFactory = clientFactory;
            _dbManager = dbManager;
        }
        [HttpGet("{year}/{month}/{day}")]
        public async Task<IActionResult> Get(int year, int month, int day)
        {
            var selectedDate = new DateTime(year, month, day);

            var observations = _dbManager.GetObservationsByDate(selectedDate);

            return Ok(observations);
        }
        public List<Observation> ParseScheduleData(string[] scheduleLines)
            {
                var observations = new List<Observation>();

                foreach (var line in scheduleLines.Skip(4)) // Skipping headers
                {
                    var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length < 12) continue; // Not a valid observation

                    try
                    {
                        var visitIdParts = parts[0].Split(':');
                        var keywords = parts.Skip(12).ToList(); // The 13th part and onwards are keywords

                        var observation = new Observation
                        {
                            VisitId = parts[0],
                            PCSMode = parts[1],
                            VisitType = $"{parts[2]} {parts[3]} {parts[4]}",
                            ScheduledStartTime = DateTime.ParseExact(parts[5], "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
                            Duration = TimeSpan.Parse(parts[6].Replace('/', ':')),
                            ScienceInstrumentAndMode = $"{parts[7]} {parts[8]}",
                            TargetName = parts[9],
                            Category = parts[10],
                            Keywords = keywords
                        };

                        observations.Add(observation);
                    }
                    catch (FormatException fe)
                    {
                        Debug.WriteLine($"Error parsing line '{line}'. Message: {fe.Message}");
                    }
                }

                return observations;
            }
        }
}
