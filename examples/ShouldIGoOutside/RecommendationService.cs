using BahmanM.Flow;

namespace ShouldIGoOutside;

/// <summary>
/// Orchestrates the entire process of fetching data and creating a recommendation
/// by composing multiple smaller, reusable Flows.
/// </summary>
public sealed class RecommendationService(IGeolocationClient geolocationClient, IWeatherClient weatherClient)
{
    /// <summary>
    /// The main recipe for the entire recommendation process.
    /// It composes the other private Flows to create the end-to-end sequence.
    /// </summary>
    public IFlow<string> GetRecommendationFlow() =>
        // Start with step 1...
        DetermineLocationFlow()
            // ...then, if successful, proceed to step 2.
            .Chain(FetchConditionsFlow)
            // Log the intermediate results for observability.
            .DoOnSuccess(pair =>
            {
                Console.WriteLine($"--> Fetched Weather: {pair.weather.Temperature}°C");
                Console.WriteLine($"--> Fetched Air Quality (AQI): {pair.aqi.Aqi}");
            })
            // Finally, transform the aggregated data into a human-readable recommendation.
            .Select(pair => MakeRecommendation(pair.weather, pair.aqi));

    /// <summary>
    /// A sub-recipe for determining the user's location.
    /// This Flow can be reused, tested, or composed independently.
    /// </summary>
    private IFlow<Geolocation> DetermineLocationFlow() =>
        geolocationClient
            .GetGeolocationFlow()
            .DoOnSuccess(loc => Console.WriteLine($"--> Determined location: {loc.City}"))
            .DoOnFailure(ex => Console.WriteLine($"--> Failed to determine location: {ex.Message}"));

    /// <summary>
    /// A sub-recipe for fetching weather and air quality based on a given location.
    /// </summary>
    private IFlow<(Weather weather, AirQuality aqi)> FetchConditionsFlow(Geolocation loc) =>
        // This Flow demonstrates a more complex composition.
        weatherClient
            .GetWeatherFlow(loc)
            // After getting the weather, we chain to the next operation: getting the air quality.
            .Chain(w => weatherClient
                .GetAirQualityFlow(loc)
                // Safety Net: If the Air Quality API fails, we don't fail the whole process.
                // Instead, we Recover to a default "Good" AQI value and continue.
                .DoOnFailure(ex => Console.WriteLine($"--> Air Quality API failed: {ex.Message}. Recovering..."))
                .Recover(_ => Flow.Succeed(new AirQuality(0)))
                // Finally, select both results into a tuple to pass to the next step.
                .Select(a => (w, a)));

    /// <summary>
    /// A pure function that applies business logic to make a final recommendation.
    /// </summary>
    private static string MakeRecommendation(in Weather weather, in AirQuality airQuality) =>
        weather switch
        {
            _ when IsRaining(weather) => "No, it's raining!",
            _ when weather.Temperature < 10 => "No, it's too cold!",
            _ when airQuality.Aqi > 100 => "No, the air quality is poor.",
            _ => "Yes, it's a great day to go outside!"
        };

    // A simple helper to make the business logic more readable.
    private static bool IsRaining(in Weather w) =>
        // Weather codes from Open-Meteo documentation for rain
        w.WeatherCode is >= 51 and <= 67 or >= 80 and <= 82;
}
