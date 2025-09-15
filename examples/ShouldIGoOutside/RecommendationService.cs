using BahmanM.Flow;

using Microsoft.Extensions.Logging;

namespace ShouldIGoOutside;

/// <summary>
/// Orchestrates the entire process of fetching data and creating a recommendation
/// by composing multiple smaller, reusable Flows.
/// </summary>
public sealed class RecommendationService(
    IGeolocationClient geolocationClient,
    IWeatherClient weatherClient,
    ILogger<RecommendationService> logger)
{
    private readonly ILogger<RecommendationService> _logger = logger;
    /// <summary>
    /// The main recipe for the entire recommendation process.
    /// It composes the other private Flows to create the end-to-end sequence.
    /// </summary>
    public IFlow<string> GetRecommendationFlow() =>
        DetermineLocationFlow()
            .Chain(FetchConditionsFlow)
            .DoOnSuccess(pair =>
                _logger.LogInformation("Fetched Weather: {Temp}°C; AQI: {Aqi}", pair.weather.Temperature, pair.aqi.Aqi))
            .Select(pair =>
                MakeRecommendation(pair.weather, pair.aqi));

    /// <summary>
    /// A sub-recipe for determining the user's location.
    /// This Flow can be reused, tested, or composed independently.
    /// </summary>
    private IFlow<Geolocation> DetermineLocationFlow() =>
        geolocationClient
            .GetGeolocationFlow()
            .DoOnSuccess(loc =>
                _logger.LogInformation("Determined location: {City}", loc.City))
            .DoOnFailure(ex =>
                _logger.LogError(ex, "Failed to determine location"));

    /// <summary>
    /// A sub-recipe for fetching weather and air quality based on a given location.
    /// </summary>
    private IFlow<(Weather weather, AirQuality aqi)> FetchConditionsFlow(Geolocation location) =>
        weatherClient
            .GetWeatherFlow(location)
            .Chain(weather => weatherClient
                .GetAirQualityFlow(location)
                // Safety Net: If the Air Quality API fails, we recover with a default value instead of failing the entire process.
                .Recover(_ =>
                {
                    _logger.LogWarning("Air Quality API failed. Recovering with default AQI.");
                    return Flow.Succeed(new AirQuality(0));
                })
                .Select(airQuality => (weather, airQuality)));

    /// <summary>
    /// A pure function that applies business logic to make a final recommendation.
    /// </summary>
    private static string MakeRecommendation(in Weather weather, in AirQuality airQuality) =>
        weather switch
        {
            _ when IsRaining(weather) =>
                "No, it's raining!",
            _ when weather.Temperature < 10 =>
                "No, it's too cold!",
            _ when airQuality.Aqi > 100 =>
                "No, the air quality is poor.",
            _ =>
                "Yes, it's a great day to go outside!"
        };

    // A simple helper to make the business logic more readable.
    private static bool IsRaining(in Weather weather) =>
        // Weather codes from Open-Meteo documentation for rain
        weather.WeatherCode is >= 51 and <= 67 or >= 80 and <= 82;
}