using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace eartq
{
    public class EarthquakeResponse
    {
        [JsonPropertyName("status")]
        public bool Status { get; set; }

        [JsonPropertyName("result")]
        public EarthquakeData[] Result { get; set; }
    }

    public class EarthquakeData
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("mag")]
        public double Mag { get; set; }

        [JsonPropertyName("date_time")]
        public string DateTime { get; set; }
    }

    public static class EarthquakeService
    {
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        private const string ApiUrl = "https://api.orhanaydogdu.com.tr/deprem/kandilli/live";

        public static async Task<string> GetLatestEarthquakeAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync(ApiUrl);
                var data = JsonSerializer.Deserialize<EarthquakeResponse>(response);

                if (data != null && data.Status && data.Result != null && data.Result.Length > 0)
                {
                    // Gather up to 5 recent significant earthquakes (or just latest 5)
                    var recentQuakes = data.Result.Where(e => e.Mag >= 2.0).Take(5).ToList();
                    
                    if (recentQuakes.Count == 0)
                    {
                        recentQuakes = data.Result.Take(5).ToList();
                    }

                    var formattedList = recentQuakes.Select(e => {
                        string timeStr = e.DateTime;
                        if (DateTime.TryParse(timeStr, out DateTime dt)) timeStr = dt.ToString("HH:mm");
                        return $"📍 {e.Title} - {e.Mag} Büyüklüğünde ({timeStr})";
                    });

                    return string.Join("   •   ", formattedList);
                }
            }
            catch (Exception)
            {
                // Silent catch, don't crash the widget if API fails
            }

            return "📍 Deprem verisi alınamadı...";
        }
    }
}
