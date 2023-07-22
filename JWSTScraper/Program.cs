using HtmlAgilityPack;
using JWST_ClassLib;
using JWSTScraper.Models;

using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Diagnostics;
using System.Net.Mail;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

internal class Program
{
    private static async Task Main(string[] args)
    {
        IConfigurationBuilder builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json");

        IConfigurationRoot configuration = builder.Build();

        var connectionString = configuration.GetConnectionString("MySqlConnection");

        DatabaseManager dbManager = new DatabaseManager(connectionString);

        dbManager.Initialize();
        bool scrapeSuccessful;
        try
        {
            await ScrapeUrl(dbManager);
            scrapeSuccessful = true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"An error occurred during the scrape operation: {ex.Message}");
            scrapeSuccessful = false;
        }

        await SendCompletionSMS(scrapeSuccessful);

        static async Task ScrapeUrl(DatabaseManager dbManager)
        {
            HtmlWeb web = new HtmlWeb();
            HtmlDocument doc = await web.LoadFromWebAsync("https://www.stsci.edu/jwst/science-execution/observing-schedules");

            HtmlNodeCollection linkNodes = doc.DocumentNode.SelectNodes("//a[@href]");
            foreach (HtmlNode? linkNode in linkNodes)
            {
                var hrefValue = linkNode.GetAttributeValue("href", null);
                if (hrefValue != null && hrefValue.Contains("_report_"))
                {
                    // Check if URL is already in database
                    if (!dbManager.DoesUrlExist(hrefValue))
                    {
                        Console.WriteLine(hrefValue);
                        dbManager.InsertObservationUrl(hrefValue);
                        await InsertDataObservationsAsync(hrefValue, dbManager);
                        Console.WriteLine("Url's successfully checked.");
                    }
                    else
                    {
                        Console.WriteLine($"Skipping, the URL is already in Database");
                    }
                }
            }
        }

         static async Task SendCompletionSMS(bool scrapeSuccessful)
        {
            var accountSid = "ACef406048d021b4956ec93da7ab4ba976";
            var authToken = "568de279e1880e770390b0d1ae95c2d8";
            TwilioClient.Init(accountSid, authToken);
            var messageOptions = new CreateMessageOptions(
  new PhoneNumber("+19893265078"));
            messageOptions.From = new PhoneNumber("+18339032566");
            messageOptions.Body = scrapeSuccessful
               ? "The scrape operation completed successfully."
               : "The scrape operation encountered an error.";
            var message = MessageResource.Create(messageOptions);
            Console.WriteLine(message.Body);
        }

        static async Task InsertDataObservationsAsync(string reportUrl, DatabaseManager dbManager)
        {
            using (HttpClient client = new HttpClient())
            {
                var baseUrl = "https://www.stsci.edu/";
                var url = baseUrl + reportUrl;
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                HttpResponseMessage response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var scheduleData = await response.Content.ReadAsStringAsync();
                    var scheduleLines = scheduleData.Split('\n');
                    List<Observation> observations = ParseScheduleData(scheduleLines);
                    foreach (Observation observation in observations)
                    {
                        dbManager.InsertObservationData(observation);
                        Console.WriteLine($"Observation Vist Id: {observation.VisitId} added successfully to database.");
                    }
                    Console.WriteLine("Observation insertion operations have completed successfully");
                }
            }
        }
        static List<Observation> ParseScheduleData(string[] scheduleLines)
        {
            List<Observation> observations = new List<Observation>();
            List<string> instrumentModeKeywords = new List<string> { "Series", "Imaging", "Lamp", "Spectroscopy", "Phasing", "Dark", "Anneal" };

            foreach (var line in scheduleLines.Skip(4)) // Skipping headers
            {
                var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 12)
                {
                    continue; // Not a valid observation
                }

                try
                {
                    var visitIdParts = parts[0].Split(':');
                    int scheduledStartTimeIndex;
                    string visitType;
                    var scheduledStartTime = "No time scheduled"; // Initialized with default string
                    var duration = "No duration"; // Initialized with default value

                    if (parts[5] == "^ATTACHED")
                    {
                        scheduledStartTimeIndex = 8;
                        visitType = string.Join(" ", parts.Skip(2).Take(6));

                        if (parts[scheduledStartTimeIndex] != "TO")
                        {
                            scheduledStartTime = parts[scheduledStartTimeIndex];
                        }
                        scheduledStartTimeIndex += 2; // incrementing by 2 to bypass "TO PRIME^"

                        // Check if the duration is present
                        if (parts.Length > scheduledStartTimeIndex + 1)
                        {
                            duration = parts[scheduledStartTimeIndex + 1];
                        }
                    }
                    else
                    {
                        scheduledStartTimeIndex = 5;
                        visitType = string.Join(" ", parts.Skip(2).Take(3));
                        scheduledStartTime = parts[scheduledStartTimeIndex];
                        duration = parts[scheduledStartTimeIndex + 1];
                    }

                    // Handle the ScienceInstrumentAndMode field
                    List<string> scienceInstrumentAndModeParts = new List<string>();
                    while (scheduledStartTimeIndex + 2 < parts.Length && !instrumentModeKeywords.Contains(parts[scheduledStartTimeIndex + 2]))
                    {
                        scienceInstrumentAndModeParts.Add(parts[scheduledStartTimeIndex + 2]);
                        scheduledStartTimeIndex++;
                    }

                    if (scheduledStartTimeIndex + 2 < parts.Length && instrumentModeKeywords.Contains(parts[scheduledStartTimeIndex + 2]))
                    {
                        // Add the stopping word to the ScienceInstrumentAndMode field
                        scienceInstrumentAndModeParts.Add(parts[scheduledStartTimeIndex + 2]);
                        scheduledStartTimeIndex++;
                    }

                    var scienceInstrumentAndMode = string.Join(" ", scienceInstrumentAndModeParts);

                    // After processing the ScienceInstrumentAndMode field, the next parts are TargetName and Category
                    var targetName = parts.Length > scheduledStartTimeIndex + 2 ? parts[scheduledStartTimeIndex + 2] : "Unknown";
                    var category = parts.Length > scheduledStartTimeIndex + 3 ? parts[scheduledStartTimeIndex + 3] : "Unknown";

                    // The keywords start after the Category field
                    List<string> keywordList = parts.Skip(scheduledStartTimeIndex + 4).ToList();

                    Observation observation = new Observation
                    {
                        VisitId = parts[0],
                        PCSMode = parts[1],
                        VisitType = visitType,
                        ScheduledStartTime = scheduledStartTime,
                        Duration = duration,
                        ScienceInstrumentAndMode = scienceInstrumentAndMode,
                        TargetName = targetName,
                        Category = category,
                        KeywordList = keywordList,  // This is the list of keywords
                        Keywords = string.Join(",", keywordList) // This is the string of keywords
                    };

                    observations.Add(observation);
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Error parsing line '{line}'. Message: {e.Message}");
                }
            }

            return observations;
        }

    }
}
