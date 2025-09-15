using System.Net.Http.Json;
using System.Text.Json.Serialization;
using BahmanM.Flow;

namespace ShouldIGoOutside;

/// <summary>
/// Defines the contract for a client that can determine geolocation.
/// </summary>
public interface IGeolocationClient
{
    /// <summary>
    /// A composable Flow that determines the user's geolocation from their IP address.
    /// </summary>
    IFlow<Geolocation> GetGeolocationFlow();
}

/// <summary>
/// Defines the contract for a client that provides weather and air quality data.
/// </summary>
public interface IWeatherClient
{
    /// <summary>
    /// A composable Flow that gets the current weather for a given location.
    /// </summary>
    IFlow<Weather> GetWeatherFlow(Geolocation location);

    /// <summary>
    /// A composable Flow that gets the current air quality for a given location.
    /// </summary>
    IFlow<AirQuality> GetAirQualityFlow(Geolocation location);
}

/// <summary>
/// An implementation of <see cref="IGeolocationClient"/> that uses the ip-api.com service.
/// </summary>
public sealed class GeolocationClient(HttpClient httpClient) : IGeolocationClient
{
    public IFlow<Geolocation> GetGeolocationFlow() =>
        // Start by creating a Flow from an async, failable operation.
        Flow.Create(ct => httpClient.GetAsync("http://ip-api.com/json", ct))
            // Gatekeeper: Ensure the HTTP call was successful before trying to read the body.
            .Validate(resp => resp.IsSuccessStatusCode,
                      resp => new HttpRequestException($"HTTP {(int)resp.StatusCode} from geolocation API"))
            // Sequencer: Safely read the response body. Using WithResource guarantees disposal.
            .Chain(resp => Flow.WithResource(
                acquire: () => resp,
                use: r => Flow.Create(ct => r.Content.ReadFromJsonAsync<IpApiGeolocation>(ct))
                               // Gatekeeper: Validate the deserialized object.
                               .Validate(dto => dto is not null, _ => new InvalidDataException("Failed to deserialize geolocation response."))
                               // Transformer: Map from the internal DTO to our domain record.
                               .Select(dto => new Geolocation(dto!.City, dto.Lat, dto.Lon))
            ))
            // Gatekeeper: Final validation on the result.
            .Validate(g => !string.IsNullOrWhiteSpace(g.City), _ => new InvalidDataException("City is missing."))
            // Resiliency: Add a retry policy for specific, transient failures.
            .WithRetry(2, typeof(InvalidDataException))
            .WithTimeout(TimeSpan.FromSeconds(3));

    // Private record for deserialization, specific to the ip-api.com response shape.
    private sealed record IpApiGeolocation(string City, double Lat, double Lon);
}

/// <summary>
/// An implementation of <see cref="IWeatherClient"/> that uses the Open-Meteo service.
/// </summary>
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
            .Validate(w => w.Temperature is > -90 and < 60, // A simple sanity check on the data.
                      w => new InvalidDataException($"Unrealistic temperature: {w.Temperature}"))
            .WithRetry(2, typeof(InvalidDataException))
            .WithTimeout(TimeSpan.FromSeconds(3));
    }

    public IFlow<AirQuality> GetAirQualityFlow(Geolocation location)
    {
        var url = $"https://air-quality-api.open-meteo.com/v1/air-quality?latitude={location.Lat}&longitude={location.Lon}&current=european_aqi";

        return Flow
            // 1. Model potential flakiness as its own failable operation. This makes the cause of
            //    failure clearer than just a random exception from deep within the HTTP call.
            .Create(() =>
            {
                if (Random.Shared.Next(0, 3) == 0) // 33% chance of failure
                    throw new HttpRequestException("Air Quality API is temporarily unavailable.");
                return 0; // dummy value
            })
            // 2. Perform the HTTP call.
            .Chain(_ => Flow.Create(ct => httpClient.GetAsync(url, ct)))
            .Validate(resp => resp.IsSuccessStatusCode,
                      resp => new HttpRequestException($"HTTP {(int)resp.StatusCode} from AQI API"))
            // 3. Read and map the payload while ensuring disposal of the response.
            .Chain(resp => Flow.WithResource(
                acquire: () => resp,
                use: r => Flow.Create(ct => r.Content.ReadFromJsonAsync<OpenMeteoAqiResponse>(ct))
                               .Validate(dto => dto is not null, _ => new InvalidDataException("Failed to deserialize AQI response."))
                               .Select(dto => new AirQuality(dto!.Current.EuropeanAqi))
            ))
            .Validate(a => a.Aqi is >= 0 and <= 500, a => new InvalidDataException($"AQI out of range: {a.Aqi}"))
            // This API is flaky, so we'll be more aggressive with retries.
            .WithRetry(3, typeof(InvalidDataException), typeof(TaskCanceledException))
            .WithTimeout(TimeSpan.FromSeconds(2));
    }

    // Private records for deserialization to match the specific API shapes.
    private sealed record OpenMeteoWeatherResponse([property: JsonPropertyName("current_weather")] CurrentWeatherResponse CurrentWeather);
    private sealed record CurrentWeatherResponse(double Temperature, [property: JsonPropertyName("weathercode")] int WeatherCode);
    private sealed record OpenMeteoAqiResponse([property: JsonPropertyName("current")] AqiResponse Current);
    private sealed record AqiResponse([property: JsonPropertyName("european_aqi")] double EuropeanAqi);
}

/// <summary>
/// Represents the core geolocation data.
/// </summary>
public readonly record struct Geolocation(string City, double Lat, double Lon);

/// <summary>
/// Represents the core weather data.
/// </summary>
public readonly record struct Weather(double Temperature, int WeatherCode);

/// <summary>
/// Represents the core air quality data.
/// </summary>
public readonly record struct AirQuality(double Aqi);
