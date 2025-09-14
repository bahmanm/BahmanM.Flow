
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using BahmanM.Flow;

namespace ShouldIGoOutside;

// --- 1. DTOs for API responses ---

public record Geolocation(string City, double Lat, double Lon);

public record Weather(double Temperature, int WeatherCode);

public record AirQuality(double Aqi);

// --- 2. Client Interfaces ---

public interface IGeolocationClient
{
    IFlow<Geolocation> GetGeolocation();
}

public interface IWeatherClient
{
    IFlow<Weather> GetWeather(Geolocation location);
    IFlow<AirQuality> GetAirQuality(Geolocation location);
}

// --- 3. Client Implementations ---

public class GeolocationClient : IGeolocationClient
{
    private readonly HttpClient _httpClient;

    public GeolocationClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public IFlow<Geolocation> GetGeolocation() => Flow.Create(async ct =>
    {
        var response = await _httpClient.GetAsync("http://ip-api.com/json", ct);
        response.EnsureSuccessStatusCode();

        var geo = await response.Content.ReadFromJsonAsync<IpApiGeolocation>(ct);

        return geo is null
            ? throw new InvalidDataException("Failed to deserialize geolocation response.")
            : new Geolocation(geo.City, geo.Lat, geo.Lon);
    });

    // Private record for deserialization to match the specific API
    private record IpApiGeolocation(string City, double Lat, double Lon);
}

public class WeatherClient : IWeatherClient
{
    private readonly HttpClient _httpClient;

    public WeatherClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public IFlow<Weather> GetWeather(Geolocation location) => Flow.Create(async ct =>
    {
        var url = $"https://api.open-meteo.com/v1/forecast?latitude={location.Lat}&longitude={location.Lon}&current_weather=true";
        var response = await _httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var weatherData = await response.Content.ReadFromJsonAsync<OpenMeteoWeatherResponse>(ct);

        return weatherData is null
            ? throw new InvalidDataException("Failed to deserialize weather response.")
            : new Weather(weatherData.CurrentWeather.Temperature, weatherData.CurrentWeather.WeatherCode);
    });

    public IFlow<AirQuality> GetAirQuality(Geolocation location) => Flow.Create(async ct =>
    {
        // Simulate a potentially flaky API call
        if (new Random().Next(0, 3) == 0) // 33% chance of failure
        {
            throw new HttpRequestException("Air Quality API is temporarily unavailable.");
        }

        var url = $"https://air-quality-api.open-meteo.com/v1/air-quality?latitude={location.Lat}&longitude={location.Lon}&current=european_aqi";
        var response = await _httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var aqiData = await response.Content.ReadFromJsonAsync<OpenMeteoAqiResponse>(ct);

        return aqiData is null
            ? throw new InvalidDataException("Failed to deserialize AQI response.")
            : new AirQuality(aqiData.Current.EuropeanAqi);
    });

    // Private records for deserialization to match the specific APIs
    private record OpenMeteoWeatherResponse([property: JsonPropertyName("current_weather")] CurrentWeatherResponse CurrentWeather);
    private record CurrentWeatherResponse(double Temperature, [property: JsonPropertyName("weathercode")] int WeatherCode);

    private record OpenMeteoAqiResponse([property: JsonPropertyName("current")] AqiResponse Current);
    private record AqiResponse([property: JsonPropertyName("european_aqi")] double EuropeanAqi);
}
