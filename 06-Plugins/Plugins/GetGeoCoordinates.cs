using System.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;
using System.Net.Http;

namespace MyPlugins
{
    public class GetGeoCoordinates
    {
        private IHttpClientFactory _httpClientFactory;
        private string _apiKey = "replace";  //Get this API_KEY from your Geocode.maps.co account

        public GetGeoCoordinates(System.Net.Http.IHttpClientFactory httpClientFactory, Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
        }


        [KernelFunction("geocode_address")]
        [Description("get the latitude and longitude for an address")]
        [return: Description("JSON object containing lat and lon values for the supplied address that matches or null if not found.")]
        public async Task<string?> GeocodeAddressAsync(string address)
        {
            Console.WriteLine("Geo Tool has been called");
            using HttpClient httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetStringAsync($"https://geocode.maps.co/search?q={address}&api_key={_apiKey}");

            using var doc = System.Text.Json.JsonDocument.Parse(response);
            var root = doc.RootElement;
            if (root.ValueKind != System.Text.Json.JsonValueKind.Array || root.GetArrayLength() == 0)
                return null;

            var first = root[0];
            if (first.TryGetProperty("lat", out var latProp) && first.TryGetProperty("lon", out var lonProp))
            {
                var result = new
                {
                    lat = latProp.GetString(),
                    lon = lonProp.GetString()
                };
                return System.Text.Json.JsonSerializer.Serialize(result);
            }
            return null;
        }
    }

}

