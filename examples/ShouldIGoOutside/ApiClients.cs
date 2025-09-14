using System.Net.Http.Json;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using BahmanM.Flow;

namespace ShouldIGoOutside;

// --- 1. DTOs (value semantics) ---

public readonly record struct Geolocation(string City, double Lat, double Lon);

public readonly record struct Weather(double Temperature, int WeatherCode);

public readonly record struct AirQuality(double Aqi);

// --- 2. Client Interfaces ---

public interface IGeolocationClient
{
    IFlow<Geolocation> GetGeolocationFlow();
}

public interface IWeatherClient
{
    IFlow<Weather> GetWeatherFlow(Geolocation location);
    IFlow<AirQuality> GetAirQualityFlow(Geolocation location);
}

// --- 3. Client Implementations ---

public sealed class GeolocationClient(HttpClient httpClient) : IGeolocationClient
{
    public IFlow<Geolocation> GetGeolocationFlow() =>
        Flow.Create(ct => httpClient.GetAsync("http://ip-api.com/json", ct))
            .Validate(resp => resp.IsSuccessStatusCode,
                      resp => new HttpRequestException($"HTTP {(int)resp.StatusCode} from geolocation API"))
            .Chain(resp => Flow.WithResource(
                acquire: () => resp,
                use: r => Flow.Create(ct => r.Content.ReadFromJsonAsync<IpApiGeolocation>(ct))
                               .Validate(dto => dto is not null, _ => new InvalidDataException("Failed to deserialize geolocation response."))
                               .Select(dto => new Geolocation(dto!.City, dto.Lat, dto.Lon))
            ))
            .Validate(g => !string.IsNullOrWhiteSpace(g.City), _ => new InvalidDataException("City is missing."))
            .WithRetry(2, typeof(InvalidDataException))
            .WithTimeout(TimeSpan.FromSeconds(3));

    private sealed record IpApiGeolocation(string City, double Lat, double Lon);
}

public sealed class WeatherClient(HttpClient httpClient) : IWeatherClient
{
    public IFlow<Weather> GetWeatherFlow(Geolocation location)
    {
        var url = $"https://api.open-meteo.com/v1/forecast?latitude={location.Lat}&longitude={location.Lon}&current_weather=true";

        return Flow.Create(ct => httpClient.GetAsync(url, ct))
            .Validate(resp => resp.IsSuccessStatusCode,
                      resp => new HttpRequestException($"HTTP {(int)resp.StatusCode} from weather API"))
            .Chain(resp => Flow.WithResource(
                acquire: () => resp,
                use: r => Flow.Create(ct => r.Content.ReadFromJsonAsync<OpenMeteoWeatherResponse>(ct))
                               .Validate(dto => dto is not null, _ => new InvalidDataException("Failed to deserialize weather response."))
                               .Select(dto => new Weather(dto!.CurrentWeather.Temperature, dto!.CurrentWeather.WeatherCode))
            ))
            .Validate(w => w.Temperature is > -90 and < 60,
                      w => new InvalidDataException($"Unrealistic temperature: {w.Temperature}"))
            .WithRetry(2, typeof(InvalidDataException))
            .WithTimeout(TimeSpan.FromSeconds(3));
    }

    public IFlow<AirQuality> GetAirQualityFlow(Geolocation location)
    {
        var url = $"https://air-quality-api.open-meteo.com/v1/air-quality?latitude={location.Lat}&longitude={location.Lon}&current=european_aqi";

        return Flow
            // 1) Model flakiness as its own failable operation
            .Create(() =>
            {
                if (Random.Shared.Next(0, 3) == 0)
                    throw new HttpRequestException("Air Quality API is temporarily unavailable.");
                return 0; // dummy value
            })
            // 2) Perform the HTTP call
            .Chain(_ => Flow.Create(ct => httpClient.GetAsync(url, ct)))
            .Validate(resp => resp.IsSuccessStatusCode,
                      resp => new HttpRequestException($"HTTP {(int)resp.StatusCode} from AQI API"))
            // 3) Read and map the payload while ensuring disposal
            .Chain(resp => Flow.WithResource(
                acquire: () => resp,
                use: r => Flow.Create(ct => r.Content.ReadFromJsonAsync<OpenMeteoAqiResponse>(ct))
                               .Validate(dto => dto is not null, _ => new InvalidDataException("Failed to deserialize AQI response."))
                               .Select(dto => new AirQuality(dto!.Current.EuropeanAqi))
            ))
            .Validate(a => a.Aqi is >= 0 and <= 500, a => new InvalidDataException($"AQI out of range: {a.Aqi}"))
            .WithRetry(3, typeof(InvalidDataException), typeof(TaskCanceledException))
            .WithTimeout(TimeSpan.FromSeconds(2));
    }

    // Private records for deserialization to match the specific APIs
    private sealed record OpenMeteoWeatherResponse([property: JsonPropertyName("current_weather")] CurrentWeatherResponse CurrentWeather);
    private sealed record CurrentWeatherResponse(double Temperature, [property: JsonPropertyName("weathercode")] int WeatherCode);
    private sealed record OpenMeteoAqiResponse([property: JsonPropertyName("current")] AqiResponse Current);
    private sealed record AqiResponse([property: JsonPropertyName("european_aqi")] double EuropeanAqi);
}
