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

    // 2) Fetch current conditions (composable operation)
    private IFlow<(Weather weather, AirQuality aqi)> FetchConditionsFlow(Geolocation loc) =>
        FlowEx
            .Zip(weatherClient.GetWeatherFlow(loc),
                 weatherClient
                     .GetAirQualityFlow(loc)
                     .DoOnFailure(ex => Console.WriteLine($"--> Air Quality API failed: {ex.Message}. Recovering..."))
                     .Recover(_ => Flow.Succeed(new AirQuality(0))))
            .WithTimeout(TimeSpan.FromSeconds(4)); // shared budget across both calls

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

// Minimal helper for strongly‑typed parallel composition over two different types
public static class FlowEx
{
    public static IFlow<(T1 weather, T2 aqi)> Zip<T1, T2>(IFlow<T1> a, IFlow<T2> b)
        where T1 : notnull
        where T2 : notnull
        => Flow.All(
                a.Select(x => (object)x),
                b.Select(x => (object)x))
            .Select(items => ((T1)items[0], (T2)items[1]));
}
