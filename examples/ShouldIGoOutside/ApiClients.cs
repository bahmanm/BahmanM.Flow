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
    /// <inheritdoc/>
    /// <remarks>
    /// This implementation applies a default resiliency policy of 2 retry attempts and a 3-second timeout.
    /// </remarks>
    public IFlow<Geolocation> GetGeolocationFlow() =>
        Flow
            .Create(cancellationToken =>
                httpClient.GetAsync("http://ip-api.com/json", cancellationToken))
            .Validate(
                resp => resp.IsSuccessStatusCode,
                resp => new HttpRequestException($"HTTP {(int)resp.StatusCode} from geolocation API"))
            .Chain(response => Flow
                .WithResource(
                    acquire: () => response,
                    use: httpResponse => Flow
                        .Create(cancellationToken =>
                            httpResponse.Content.ReadFromJsonAsync<IpApiGeolocation>(cancellationToken))
                        .Validate(
                            geoDto => geoDto is not null,
                            _ => new InvalidDataException("Failed to deserialize geolocation response."))
                        .Select(geoDto =>
                            new Geolocation(geoDto!.City, geoDto.Lat, geoDto.Lon))
                ))
            .Validate(
                g => !string.IsNullOrWhiteSpace(g.City),
                _ => new InvalidDataException("City is missing."))
            .WithRetry(2, typeof(InvalidDataException))
            .WithTimeout(TimeSpan.FromSeconds(3));

    private sealed record IpApiGeolocation(string City, double Lat, double Lon);
}

/// <summary>
/// An implementation of <see cref="IWeatherClient"/> that uses the Open-Meteo service.
/// </summary>
public sealed class WeatherClient(HttpClient httpClient) : IWeatherClient
{
    public IFlow<Weather> GetWeatherFlow(Geolocation location)
    {
        var requestUrl =
            $"https://api.open-meteo.com/v1/forecast?latitude={location.Lat}&longitude={location.Lon}&current_weather=true";

        return Flow
            .Create(cancellationToken =>
                httpClient.GetAsync(requestUrl, cancellationToken))
            .Validate(
                response => response.IsSuccessStatusCode,
                response => new HttpRequestException($"HTTP {(int)response.StatusCode} from weather API"))
            .Chain(response => Flow
                .WithResource(
                    acquire: () => response,
                    use: httpResponse => Flow
                        .Create(cancellationToken =>
                            httpResponse.Content.ReadFromJsonAsync<OpenMeteoWeatherResponse>(cancellationToken))
                        .Validate(
                            weatherDto => weatherDto is not null,
                            _ => new InvalidDataException("Failed to deserialize weather response."))
                        .Select(weatherDto =>
                            new Weather(weatherDto!.CurrentWeather.Temperature, weatherDto!.CurrentWeather.WeatherCode))
                ))
            .Validate(
                w => w.Temperature is > -90 and < 60, // A simple sanity check on the data.
                w => new InvalidDataException($"Unrealistic temperature: {w.Temperature}"))
            .WithRetry(2, typeof(InvalidDataException))
            .WithTimeout(TimeSpan.FromSeconds(3));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// This implementation uses a more aggressive retry (3 attempts) and a shorter timeout
    /// compared to the other client methods, reflecting a different reliability assumption for this specific endpoint.
    /// </remarks>
    public IFlow<AirQuality> GetAirQualityFlow(Geolocation location)
    {
        var requestUrl =
            $"https://air-quality-api.open-meteo.com/v1/air-quality?latitude={location.Lat}&longitude={location.Lon}&current=european_aqi";

        return Flow
            .Create(cancellationToken => httpClient.GetAsync(requestUrl, cancellationToken))
            .Validate(
                response => response.IsSuccessStatusCode,
                response => new HttpRequestException($"HTTP {(int)response.StatusCode} from AQI API"))
            .Chain(response => Flow
                .WithResource(
                    acquire: () => response,
                    use: httpResponse => Flow
                        .Create(cancellationToken =>
                            httpResponse.Content.ReadFromJsonAsync<OpenMeteoAqiResponse>(cancellationToken))
                        .Validate(
                            aqiDto => aqiDto is not null,
                            _ => new InvalidDataException("Failed to deserialize AQI response."))
                        .Select(aqiDto =>
                            new AirQuality(aqiDto!.Current.EuropeanAqi))
                ))
            .Validate(
                airQuality => airQuality.Aqi is >= 0 and <= 500,
                airQuality => new InvalidDataException($"AQI out of range: {airQuality.Aqi}"))
            .WithRetry(3, typeof(InvalidDataException), typeof(TaskCanceledException))
            .WithTimeout(TimeSpan.FromSeconds(2));
    }

    private sealed record OpenMeteoWeatherResponse(
        [property: JsonPropertyName("current_weather")]
        CurrentWeatherResponse CurrentWeather);

    private sealed record CurrentWeatherResponse(
        double Temperature,
        [property: JsonPropertyName("weathercode")]
        int WeatherCode);

    private sealed record OpenMeteoAqiResponse(
        [property: JsonPropertyName("current")]
        AqiResponse Current);

    private sealed record AqiResponse(
        [property: JsonPropertyName("european_aqi")]
        double EuropeanAqi);
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