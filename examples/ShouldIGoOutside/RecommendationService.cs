using BahmanM.Flow;

namespace ShouldIGoOutside;

public sealed class RecommendationService(IGeolocationClient geolocationClient, IWeatherClient weatherClient)
{
    // Public recipe: end-to-end recommendation
    public IFlow<string> GetRecommendationFlow() =>
        DetermineLocationFlow()
            .Chain(FetchConditionsFlow)
            .DoOnSuccess(pair =>
            {
                Console.WriteLine($"--> Fetched Weather: {pair.weather.Temperature}°C");
                Console.WriteLine($"--> Fetched Air Quality (AQI): {pair.aqi.Aqi}");
            })
            .Select(pair => MakeRecommendation(pair.weather, pair.aqi));

    // 1) Determine location (composable operation)
    private IFlow<Geolocation> DetermineLocationFlow() =>
        geolocationClient
            .GetGeolocationFlow()
            .DoOnSuccess(loc => Console.WriteLine($"--> Determined location: {loc.City}"))
            .DoOnFailure(ex => Console.WriteLine($"--> Failed to determine location: {ex.Message}"));

    // 2) Fetch current conditions (simple, sequential and readable)
    private IFlow<(Weather weather, AirQuality aqi)> FetchConditionsFlow(Geolocation loc) =>
        weatherClient
            .GetWeatherFlow(loc)
            .Chain(w => weatherClient
                .GetAirQualityFlow(loc)
                .DoOnFailure(ex => Console.WriteLine($"--> Air Quality API failed: {ex.Message}. Recovering..."))
                .Recover(_ => Flow.Succeed(new AirQuality(0)))
                .Select(a => (w, a)));

    private static string MakeRecommendation(in Weather weather, in AirQuality airQuality) =>
        weather switch
        {
            _ when IsRaining(weather) => "No, it's raining!",
            _ when weather.Temperature < 10 => "No, it's too cold!",
            _ when airQuality.Aqi > 100 => "No, the air quality is poor.",
            _ => "Yes, it's a great day to go outside!"
        };

    private static bool IsRaining(in Weather w) =>
        // Open‑Meteo rain codes
        w.WeatherCode is >= 51 and <= 67 or >= 80 and <= 82;
}
